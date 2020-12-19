using System.Threading.Tasks;
using MicroBootstrap.Commands;
using Pacco.Services.Availability.Application.Exceptions;
using Pacco.Services.Availability.Application.Services;
using Pacco.Services.Availability.Core.Repositories;
using Pacco.Services.Availability.Core.ValueObjects;

namespace Pacco.Services.Availability.Application.Commands.Handlers
{
    internal sealed class ReserveResourceHandler : ICommandHandler<ReserveResource>
    {
        private readonly IResourcesRepository _repository;
        private readonly IEventProcessor _eventProcessor;

        public ReserveResourceHandler(IResourcesRepository repository, IEventProcessor eventProcessor)
        {
            _repository = repository;
            this._eventProcessor = eventProcessor;
        }

        // handler is nothing more orchestration of our domain and domain logic and external world (Application Service)
        public async Task HandleAsync(ReserveResource command)
        {
            // whenever we start to add additional logging, exception handling, retry policy, tracing, monitoring, ..., our application handlers will remain the same and all
            // additional cross cutting concerns around them will be kept in different layer (infra) and application logic doesn't change at all.

            var resource = await _repository.GetAsync(command.ResourceId); // get resource document from db and map it to tour domain model with its version
            if (resource is null)
            {
                // this is an application exception not domain exception because this is outside of boundary of particular aggregate and in aggregate perspective aggregate always exists

                // our exception will handle by our exception middleware and ExceptionToResponseMapper
                throw new ResourceNotFoundException(command.ResourceId);
            }
            var reservation = new Reservation(command.DateTime, command.Priority);
            resource.AddReservation(reservation);

            //http://www.kamilgrzybek.com/design/the-outbox-pattern/

            //we have 2 issue here:

            //1) may we commit our changes to mongo and before actually we publish the message microservices will crash what happend next? i changed internal state of microservice and I was obligated to publish this message inform the others but I crashed
            //but it may happened, it a outcomming message potential issue. whatever we have different infrastructure pieces such as mongo, redis, message broker we can never 100 percent sure, because we can't make a transaction on top of these infra parts
            //so there is a chance might be happen once per month or year. when we have different infrastructure pieces, we can never be sure that this will be process transactionally so will need to look for other patterns - outbox pattern

            //2) comminication on rabbitmq and microservices based on ACK, it happend when we not send ACK in rabbit timeout time and rabbitmq after this time that it doesn't get a ACK will send this message again and this lead us to situation that
            //we process same message twice or more. for example in our BusSubscriber we invoke message handler and it save data in database but the ACK never reach to rabbitmq, maybe our application die or maybe some networking issue and not reach to
            //rabbit and rabbit based on some setting will try send us this message again because it didn't get a ACK in 10 seconds

            //we need ensuring 2 things: 1) unique processing of single message only once even same message receive multiple time 2) ensure we never lose ant event 

            //delivery mode:
            //1)delivering at most once (fire and forget) - problem: message might be lost we don't care about it: it is like a fire and forget, we publish some message it might be eventually processed or not, but we don't care and ew don't track of this message and only thing we do is
            //  publishing this message once and we don't care about it processed or not. we will received a OrderCreated event on DeliveryService but somthing go wrong in that service and it doesn't send ACK but message
            //  broker doesn't care, it send us a message only once and doesn't care whethere we fail or proceed. but for most of scenario's we don't want to lose message and we care about processing theme and it is not what we want so we switch to at least one delivery
            
            //https://www.cloudcomputingpatterns.org/at_least_once_delivery/
            //2)at least one delivery - problem: message might be sent again: it is a better approach, idea is based on ACK. in happy path we will simply process it once if not then will try to process the same message again and again and here this is where we use idempotent. so when our message
            //are idempotant is pretty cool when it comes to distributed system. at least one delivery means having idempotant of a message or having idempotant of processing a message make it pretty easy with at least once delivery because, when we process message again
            //let say 10 time the result stay same. sometimes it is possible update and change my name or address more than once or delete because it is idempotant naturally but it's not always the case or any thing that can't be idempotant by nature like tansfering money or calling
            //some external system and we have no control out there and we would like to call them once and we can't do this idempotantly. 
            
            //3) so we need somehow provide this exactly one proceeding and handle the problem with previous approach and most important part of a message is messageId and we want unique identifier for each message, so we want exactly one processing and the way is
            //relying on database transaction to overcom thi issue.

            //https://www.cloudcomputingpatterns.org/exactly_once_delivery/
            // --- guarantee exactly one processing - Inbox Pattern --- : when we receive a message from the broker let say we receive OrderCreated event for OrderCreated command, we will store messag in the dedicated table that messageId is more important part
            // that already 'processed' so we check if we already processed a message with specific id we send back a ACk to rabbitmq because for any reason we receive same message again. so i will tell to message broker that this message process already
            //give me new one. but if it's first time I'm receiving this message then we invoke our handler as we usually do but once we save 'data' to database we also need to save this unique messageId into seperate table. so we will have a share database transaction 
            //so this transaction means we will either save both all things we change inside our handler together with messageId which will put in seperate table so we will guarantee that save this two thing at onc transaction and that is why we must have a transaction
            //and we have to use same database because otherwise if we decide to keep this process messages in redis and data in sql server we can't have a 'transaction'. so we need to have a seperate table for keep at least proccess messageId and save it as single transaction
            //in this transaction I save proccess messageId and some other data I needed and actual aggregate this part of transaction.
            // we have 2 scenario: 
            //1) we successfully save the data to Mongo and we also have this information that we processed the message with particular Id so assuming ACk not provided in the rabbitmq and it start same message again and first check this message
            //already proccessed and we will send this ACK for rabbitmq 
            //2)we not commit both messageId and data inside our resource

            //https://www.cloudcomputingpatterns.org/at_least_once_delivery/
            // --- guarantee we never lose message - guarantee delivery - OutBox Pattern --- : when we start processing message inside our handler for example OrderCreated we will insert some order in our mongodb collection but in same transaction we will also
            // have a outbox collection we will put payload of our message that we want to send to our services. so basically we will have like two insertions in one transaction, one is just order and the other is message integration event that we want to publish.
            //we also have a 'outbox processor' that is a background job and its responsibility of processor is with some interval looking into this outbox collection and cheking whether we have any thing to send and we mark it message published or not. let say every
            //3 seconds we wil check whether we have somthing new in outbox so we have new message OrderCreated so we take it from database and use message broker to publish message to microservices. when we publish message from outbox our microservices might be died so
            //eventually once the microservice restarted, processor take message from database again and publish it again. if we have inbox and exactly one processing this is not a issue for us. whenever we are republishing the same message we just have to guarantee
            //that the messageId is always the same.

            //we need do 2 things: 
            //1)unique processing of single message only once event receive a message multiple time  - inbox pattern
            //2)ensure that we will never lose any event such as save to database and publish event we always succeeded - outbox pattern - guaranty delivery mode


            //exactly-once processing: we need somehow provide this exactly one processing and because each message contain a message id and we need a unique identifier for each message and a way do achieve one processing is
            //relying on database transaction. at first we received message from broker  
            await _repository.UpdateAsync(resource); //database

            //1)we don't want publish directly to message broker we want save this event into OutBox collection and of course. we handle this in MessageBroker class with using IMessageOutbox class
            
            //2)we want to verification of unique messageId somewhere. before we invoke handler we like to verify whether we can invoke this handler or no
            //by checking this message is unique or not. we need some middleware before hit our handler. for example in rabbitmq BusSubscriber before do invocation handle(serviceProvider,message,messagecontext) we like to verify before call handle but if we can't
            //change internal of some library we could put a decorator on top of it. 

            // when message broker down and we rely on this messages being send in event driven architecture and things will go wrong and when broker is down whole system based on event will be down and we can not actually making more resilient and trying to high availability mode.
            // the beauty of this approach by using broker we have isolation and we have decoupling between services
            await _eventProcessor.ProcessAsync(resource.Events); //rabbit 
        }
    }
}