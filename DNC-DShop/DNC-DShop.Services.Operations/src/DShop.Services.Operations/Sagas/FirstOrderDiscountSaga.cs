using Chronicle;
using DShop.Common.RabbitMq;
using DShop.Services.Operations.Messages.Customers.Events;
using DShop.Services.Operations.Messages.Orders.Commands;
using DShop.Services.Operations.Messages.Orders.Events;
using System;
using System.Threading.Tasks;

namespace DShop.Services.Operations.Sagas
{
    //this is a long living transaction because immediately after start transaction don't fire another participate action
    //in transaction that is OrderCreated and maybe user create his order after some days. and we use ResolveId for detect
    //unique SagaId  because each participant in a long living transaction like this has its own corelationId and because
    //saga cordinator as default use corelation id its participant for collaborate in saga and particpants don't have same
    //corelationId Saga Action don't run for this participant because their correlationId not equal with star action 
    //correlationId.
    public class FirstOrderDiscountSagaState
    {
        public DateTime CustomerCreatedAt { get; set; }
    }

    public class FirstOrderDiscountSaga : Saga<FirstOrderDiscountSagaState>,
        ISagaStartAction<CustomerCreated>,
        ISagaAction<OrderCreated>
    {
        private const int CreationHoursLimit = 24;
        private readonly IBusPublisher _busPublisher;

        public FirstOrderDiscountSaga(IBusPublisher busPublisher)
            => _busPublisher = busPublisher;

        //by default we are resolving particular saga with using corelation Id that we create this corelation context
        //from start of whole flow in API Gateway so for scenario like short living transaction that we just send a request 
        //and some multiple steps happens in a chain that would work because we would use the same corelation context but 
        //imagine that we have long living transaction we can find our Saga with specifc field on each message hear is
        //CustomerId
        public override Guid ResolveId(object message, ISagaContext context)
        {
            switch (message)
            {
                case CustomerCreated cc: return cc.Id;
                case OrderCreated oc: return oc.CustomerId;
                default: return base.ResolveId(message, context);
            }
        }

        //1: Check whether customer creation hours diff fits the limit
        public async Task HandleAsync(CustomerCreated message, ISagaContext context)
        {
            Data.CustomerCreatedAt = DateTime.UtcNow;
            await Task.CompletedTask;
        }

        //2: Check whether customer creation hours diff fits the limit
        public async Task HandleAsync(OrderCreated message, ISagaContext context)
        {
            var diff = DateTime.UtcNow.Subtract(Data.CustomerCreatedAt);

            if (diff.TotalHours <= CreationHoursLimit)
            {
                await _busPublisher.SendAsync(new CreateOrderDiscount(
                    message.Id, message.CustomerId, 10), CorrelationContext.Empty);

                Complete();
            }
            else
            {
                Reject();
            }
        }

        #region Compensate
        public async Task CompensateAsync(CustomerCreated message, ISagaContext context)
        {
            //TOOD: Implement compensation
            await Task.CompletedTask;
        }

        public async Task CompensateAsync(OrderCreated message, ISagaContext context)
        {
            //TOOD: Implement compensation
            await Task.CompletedTask;
        }
        #endregion
    }
}
