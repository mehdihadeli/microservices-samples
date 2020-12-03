using System.Threading.Tasks;
using Chronicle;
using DShop.Common.Handlers;
using DShop.Common.Messages;
using DShop.Common.RabbitMq;
using DShop.Services.Operations.Sagas;
using DShop.Services.Operations.Services;
using SagaContext = DShop.Services.Operations.Sagas.SagaContext;

namespace DShop.Services.Operations.Handlers
{
    public class GenericEventHandler<T> : IEventHandler<T> where T : class, IEvent
    {
        private readonly ISagaCoordinator _sagaCoordinator;
        private readonly IOperationPublisher _operationPublisher;
        private readonly IOperationsStorage _operationsStorage;

        public GenericEventHandler(ISagaCoordinator sagaCoordinator,
            IOperationPublisher operationPublisher,
            IOperationsStorage operationsStorage)
        {
            _sagaCoordinator = sagaCoordinator;
            _operationPublisher = operationPublisher;
            _operationsStorage = operationsStorage;
        }

        public async Task HandleAsync(T @event, ICorrelationContext context)
        {
            //true if this event message is part of a Saga as a participant service
            if (@event.BelongsToSaga())
            {
                //create a SagaContext from CorelationContext
                //we use CorretionId for distinguish between two or more saga with same type
                //for short living transaction we use this CorelationId for distinguish between sagas
                //for long living transactions this will be tricky
                var sagaContext = SagaContext.FromCorrelationContext(context);
                //cordinator i responsible for getting the particular message and context and it looks for the all type of sagas that actually suport this
                //so one message could potentially kick off multiple Sagas because one message could be involved in different distributed transaction not just one
                //like in this case. it looks for sagas and simply resolve them using the Id
                await _sagaCoordinator.ProcessAsync(@event, sagaContext);
            }

            switch (@event)
            {
                case IRejectedEvent rejectedEvent:
                    await _operationsStorage.SetAsync(context.Id, context.UserId,
                        context.Name, OperationState.Rejected, context.Resource,
                        rejectedEvent.Code, rejectedEvent.Reason);
                    //with signalR we subscribe on operation events
                    await _operationPublisher.RejectAsync(context,
                        rejectedEvent.Code, rejectedEvent.Reason);
                    return;
                case IEvent _:
                    await _operationsStorage.SetAsync(context.Id, context.UserId,
                        context.Name, OperationState.Completed, context.Resource);
                    //with signalR we subscribe on operation events
                    await _operationPublisher.CompleteAsync(context);
                    return;
            }
        }
    }
}