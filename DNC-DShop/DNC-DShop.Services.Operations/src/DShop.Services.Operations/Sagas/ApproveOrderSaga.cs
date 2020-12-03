using System;
using System.Threading.Tasks;
using Chronicle;
using DShop.Common.RabbitMq;
using DShop.Services.Operations.Messages.Orders.Commands;
using DShop.Services.Operations.Messages.Orders.Events;
using DShop.Services.Operations.Messages.Products.Commands;
using DShop.Services.Operations.Messages.Products.Events;

namespace DShop.Services.Operations.Sagas
{
    //Another Example for Saga FirstOrderDiscountSaga is LonLiving transaction

    //user said i want to create an order so it start with order created and it looks for things like OrderRevoked and whatever
    // we reserved products and what the order was approved or not so whole idea behind this saga is that once you start 
    //a new order and you want to approve it there are multiple steps involved :

     //this is a short living transaction
    public class ApproveOrderSaga : Saga,
        ISagaStartAction<OrderCreated>,
        ISagaAction<OrderRevoked>,
        ISagaAction<RevokeOrderRejected>,
        ISagaAction<ProductsReserved>,
        ISagaAction<ReserveProductsRejected>,
        ISagaAction<OrderApproved>,
        ISagaAction<ApproveOrderRejected>
    {
        private readonly IBusPublisher _busPublisher;

        public ApproveOrderSaga(IBusPublisher busPublisher)
            => _busPublisher = busPublisher;

        //by default we are resolving particular saga with using corelation Id that we create this corelation context from start
        //of whole flow in API Gateway so for scenario like short living transaction that we just send a request and some 
        //multiple steps happens in a chain that would work because we would use the same corelation context but imagine that 
        //we have long living transaction we can find our Saga with specifc field on each message hear is OrderId
//        public override Guid ResolveId(object message, ISagaContext context)
//        {
//            switch (message)
//            {
//                case OrderCreated m: return m.Id;
//                case ProductsReserved m: return m.OrderId;
//                case ReserveProductsRejected m: return m.OrderId;
//                case OrderApproved m: return m.Id;
//                case ApproveOrderRejected m: return m.Id;
//                default: return base.ResolveId(message, context);
//            }
//        }
        //1. when i create a order and it was created successfully by order created event i want to reserve a product and this
        //command will be handle by the product service with ReserveProductHandler and it looks for product available if
        //product available it will just update them and reserve this product with minus its quantity and then published 
        //ProductReserved event meaning that was fine so if isn't then it might publish the ReservedProductRejected 
        public async Task HandleAsync(OrderCreated message, ISagaContext context)
        {
            //we can omit resolveId method and pas our correlationId to send async method because we lost our CorrelationId
            //with calling ReserveProduct command and after reply this command in ReserveProductHandler we can't get 
            //correlationId from that for finding  
            //await _busPublisher.SendAsync(new ReserveProducts(message.Id, message.Products),
            //CorrelationContext.FromId(context.CorrelationId));
            await _busPublisher.SendAsync(new ReserveProducts(message.Id, message.Products), CorrelationContext.Empty);
        }

        public async Task CompensateAsync(OrderCreated message, ISagaContext context)
        {
            await _busPublisher.SendAsync(new RevokeOrder(message.Id, message.CustomerId), CorrelationContext.Empty);
        }

        //2.1 if product reserved successfully then i could say approved order and order (we send a approve order event)
        // is approved and now the products are booked and the user can complete this order because product are available 
        //for this order but if product reserved failed go to 2.2
        public async Task HandleAsync(ProductsReserved message, ISagaContext context)
        {
            await _busPublisher.SendAsync(new ApproveOrder(message.OrderId), CorrelationContext.Empty);
        }

        public async Task CompensateAsync(ProductsReserved message, ISagaContext context)
        {
            await _busPublisher.SendAsync(new ReleaseProducts(message.OrderId, message.Products),
                CorrelationContext.Empty);
        }

        //2.2 if product reserv rejected i listen to ReserveProductsRejected failure and i'm starting the Reject of global
        // transaction with call Reject method of Saga and reject go to previous steps and call our compensations so for my
        //product reserved message it will fire above compensate and send a new command ReleaseProducts to product service
        //and handle by product service and it go to product that was previously reserved and it will update their quantity 
        //to the previous sate and fire off ProductReleased event and get back to Saga
        public async Task HandleAsync(ReserveProductsRejected message, ISagaContext context)
        {
            Reject();
            await Task.CompletedTask;
        }

        public async Task CompensateAsync(ReserveProductsRejected message, ISagaContext context)
        {
            await Task.CompletedTask;
        }

        public async Task HandleAsync(OrderApproved message, ISagaContext context)
        {
            Complete();
            await Task.CompletedTask;
        }

        public async Task CompensateAsync(OrderApproved message, ISagaContext context)
        {
            await Task.CompletedTask;
        }

        public async Task HandleAsync(ApproveOrderRejected message, ISagaContext context)
        {
            Reject();
            await Task.CompletedTask;
        }

        public async Task CompensateAsync(ApproveOrderRejected message, ISagaContext context)
        {
            await Task.CompletedTask;
        }

        public async Task HandleAsync(OrderRevoked message, ISagaContext context)
        {
            Complete();
            await Task.CompletedTask;
        }

        public async Task CompensateAsync(OrderRevoked message, ISagaContext context)
        {
            await Task.CompletedTask;
        }

        //Edge case
        public async Task HandleAsync(RevokeOrderRejected message, ISagaContext context)
        {
            Reject();
            await Task.CompletedTask;
        }

        public async Task CompensateAsync(RevokeOrderRejected message, ISagaContext context)
        {
            await Task.CompletedTask;
        }
    }
}