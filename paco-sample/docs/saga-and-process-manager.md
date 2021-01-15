we need to start appropriate library to handle the saga and one oth this is [Saga for MassTransit](https://masstransit-project.com/usage/sagas/) and we can use saga being part of `NService Bus` but we also use [OpenSleigh](https://github.com/mizrael/OpenSleigh) or [Chronicle](https://github.com/snatch-dev/Chronicle).

we can start all of the our services with using our [docker-compose](../Pacco/compose/services-local.yml) file for up and running all of them in place.

[https://docs.microsoft.com/en-us/previous-versions/msp-n-p/jj591569(v=pandp.10)](https://docs.microsoft.com/en-us/previous-versions/msp-n-p/jj591569(v=pandp.10))

[Effective Microservice Communication & Conversation Patterns with Jimmy Bogard, July 2020](https://www.youtube.com/watch?v=oNjJ5fTASJo)

order-maker is some sort of jobs for deal with sagas, idea about order maker is that we could imagine the situation which we have new business requirement so business would like to have this functionality for creating `whole order` for `transportation` of particular `parcel` fully automatically ony thing that we need to do as a user is defining parcel within our pacco service and click send with a `MakeOrder (C)` command:

``` csharp
public class MakeOrder : ICommand
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public Guid ParcelId { get; }

    public MakeOrder(Guid orderId, Guid customerId, Guid parcelId)
    {
        OrderId = orderId == Guid.Empty ? Guid.NewGuid() : orderId;
        CustomerId = customerId;
        ParcelId = parcelId;
    }
}
```
what should happen underneath, we have `ParcelId` for this particular customer `CustomerId` we need to check its type whether this is a `weapon` or maybe some `human organs` whatever and now we need to find proper vehicle based on some characteristics and this could be some advance AI and finally we need to check the availability of this particular vehicle with using availability service and eventually `reserve resource` for particular date.
our `MakeOrder` command will start whole saga and we just need provide customerId and parcelId so before trigger saga we need to add these required data to our services and system. so since all of our service is up by suing decker-compose file, we can open our [Pacco-sample-scenario.rest](..\Pacco.APIGateway\Pacco-sample-scenario.rest) file and we prepare our needed data here customerId and parcelId and then trigger saga with these information.

we create new user with `sign-up` then `sign-in` and create new `customer` then we add the `parcel` an we get get parcels and copt this newly created `parcelId` because we need it, also we need add a `vehicle` for exist a vehicle at least and also we need to create new `resource` so we are saying our availability service need this vehicle as a resource can be reserve so we pass our `vehicleId` as a `resourceId` to our `create resource` api. now we have a vehicle as a resource for reservation and we have new parcel added and also we have customerId, so here we have customerId and parcelId and let just directly hit our `order maker` service like internally (other services will call this internally) and for this case we skip api gateway but we can use API Gateway also and we open our [Pacco.Services.OrderMaker.rest](../Pacco.Services.OrderMaker/Pacco.Services.OrderMaker.rest) rest file and we set just parcelId and customerId in this file and now we have required parameters for starting saga and we should be able to proceed with this request to automatically create order for us.  

how we can start the saga? we have a web end point for this in our order maker service that defined for `MakeOrder` command and will handle in `AIOrderMakingHandler`  

``` csharp
.UseDispatcherEndpoints(endpoints => endpoints
    .Get("", ctx => ctx.Response.WriteAsync("Welcome to Pacco uber AI order maker Service!"))
    .Post<MakeOrder>("orders")))
```

[https://samueleresca.net/developing-apis-using-actor-model-in-asp-net/](https://samueleresca.net/developing-apis-using-actor-model-in-asp-net/)

How to saga fit in this microservice? there is some ways to fit saga in our infrastructure and one of its possible approach is popular `Actor Model`, so creating something like `stateful container` for our saga for each particular user that would be one approach and in this approach we treat saga as a class in our miccroservie, API will be take the request and we get data and pass it to particular class. we also have this microservice connected to rabbitmq so we have capability of exchange messages across distributed system and also as we've seen in availability service we can subscribe for particular messages and they will be also somehow pass to the `saga`.

now let's start with saga definition, we have some subscriptions and we subscribe to these events (or some commands) and we handle this events in `AIOrderMakingHandler`

``` csharp
public static IApplicationBuilder UseApp(this IApplicationBuilder app)
{
    app.UseErrorHandler()
        .UseSwaggerDocs()
        .UseConvey()
        .UseMetrics()
        .UseRabbitMq()
        .SubscribeEvent<OrderApproved>()
        .SubscribeEvent<OrderCreated>()
        .SubscribeEvent<ParcelAddedToOrder>()
        .SubscribeEvent<ResourceReserved>()
        .SubscribeEvent<VehicleAssignedToOrder>();

    return app;
}
```
also we have definition for commands that we want to send like `AddParcelToOrder` and `ApproveOrder` ,... also we have `OrderMakerSaga` in this class [AIOrderMakingHandler.cs](../Pacco.Services.OrderMaker/src/Pacco.Services.OrderMaker/Handlers/AIOrderMakingHandler.cs) which is actually our process manager for routing purpose and here we also use a `Saga Coordinator`. this process manager invoke same saga coordinator 

``` csharp
public class AIOrderMakingHandler : 
    ICommandHandler<MakeOrder>, 
    IEventHandler<OrderApproved>, 
    IEventHandler<OrderCreated>, 
    IEventHandler<ParcelAddedToOrder>,
    IEventHandler<VehicleAssignedToOrder>,
    IEventHandler<ResourceReserved>
{
    private readonly ISagaCoordinator _coordinator;

    public AIOrderMakingHandler(ISagaCoordinator coordinator)
    {
        _coordinator = coordinator;
    }
    
    public Task HandleAsync(MakeOrder command)
        => _coordinator.ProcessAsync(command, SagaContext.Empty);

    public Task HandleAsync(OrderApproved @event)
        => _coordinator.ProcessAsync(@event, SagaContext.Empty);

    public Task HandleAsync(OrderCreated @event)
        => _coordinator.ProcessAsync(@event, SagaContext.Empty);
    
    public Task HandleAsync(ParcelAddedToOrder @event)
        => _coordinator.ProcessAsync(@event, SagaContext.Empty);
    
    public Task HandleAsync(VehicleAssignedToOrder @event)
        => _coordinator.ProcessAsync(@event, SagaContext.Empty);

    public Task HandleAsync(ResourceReserved @event)
        => _coordinator.ProcessAsync(@event, SagaContext.Empty);
}
```
Chronicle is simple `process manager/saga pattern` implementation for .NET Core that helps you manage long-living and distirbuted transactions.

this is very simple `handler` and this connect the saga as a concept to our event driven architecture and this fit with our approach and coordinator itself is notching more a class that 6 for particular saga stack, one message could be let say initial state for one business process but also could be last step for other business process so there is a chance that one message will actually trigger multiple sagas on multiple states. so coordinator what is does is takes the `message` and something called `SagaContext` and in this sample
it is not important and set to `SagaContext.Empty` and basically looks for particular sagas and restore its state because we keep remember that we treat saga as a `process manager` so we need to its `internal state` and `data` that needs to process this message so we restore this and call particular step.

``` csharp
public Task HandleAsync(MakeOrder command)
    => _coordinator.ProcessAsync(command, SagaContext.Empty);
```

``` csharp
public interface ISagaCoordinator
{
    Task ProcessAsync<TMessage>(TMessage message, ISagaContext context = null) where TMessage : class;

    Task ProcessAsync<TMessage>(TMessage message, Func<TMessage, ISagaContext, Task> onCompleted = null,
        Func<TMessage, ISagaContext, Task> onRejected = null, ISagaContext context = null) where TMessage : class;       
}
```

when we speaking of the state, there is Sagas directory and we have 2 classed and first one is `AIMakingOrderData` and this would be out process manager internal data that we can keep to track what is the current order and what is the user and customer, nothing more container for the processing business process.

``` csharp
public class AIMakingOrderData
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
    public DateTime ReservationDate { get; set; }
    public int ReservationPriority { get; set; }
    public List<Guid> ParcelIds { get; set; } = new List<Guid>();
    public List<Guid> AddedParcelIds { get; set; } = new List<Guid>();
    public bool AllPackagesAddedToOrder => AddedParcelIds.Any() && AddedParcelIds.All(ParcelIds.Contains);
}
```
and in Sagas directory we also have [AIOrderMakingSaga.cs](../Pacco.Services.OrderMaker/src/Pacco.Services.OrderMaker/Handlers/AIOrderMakingHandler.cs) that is our actual saga

``` csharp
public class AIOrderMakingSaga : Saga<AIMakingOrderData>,
    ISagaStartAction<MakeOrder>,
    ISagaAction<OrderCreated>,
    ISagaAction<ParcelAddedToOrder>,
    ISagaAction<VehicleAssignedToOrder>,
    ISagaAction<OrderApproved>
{
    private const string SagaHeader = "Saga";
    private readonly IResourceReservationsService _resourceReservationsService;
    private readonly IVehiclesServiceClient _vehiclesServiceClient;
    private readonly IBusPublisher _publisher;
    private readonly ICorrelationContextAccessor _accessor;
    private readonly ILogger<AIOrderMakingSaga> _logger;

    public AIOrderMakingSaga(IResourceReservationsService resourceReservationsService,
        IVehiclesServiceClient vehiclesServiceClient, IBusPublisher publisher,
        ICorrelationContextAccessor accessor, ILogger<AIOrderMakingSaga> logger)
    {
        _resourceReservationsService = resourceReservationsService;
        _vehiclesServiceClient = vehiclesServiceClient;
        _publisher = publisher;
        _accessor = accessor;
        _logger = logger;
        _accessor.CorrelationContext = new CorrelationContext
        {
            User = new CorrelationContext.UserContext()
        };
    }

    public override SagaId ResolveId(object message, ISagaContext context)
        => message switch
        {
            MakeOrder m => (SagaId) m.OrderId.ToString(),
            OrderCreated m => (SagaId) m.OrderId.ToString(),
            ParcelAddedToOrder m => (SagaId) m.OrderId.ToString(),
            VehicleAssignedToOrder m => (SagaId) m.OrderId.ToString(),
            OrderApproved m => m.OrderId.ToString(),
            _ => base.ResolveId(message, context)
        };

    public async Task HandleAsync(MakeOrder message, ISagaContext context)
    {
        _logger.LogInformation($"Started a saga for order: '{message.OrderId}'.");
        Data.ParcelIds.Add(message.ParcelId);
        Data.OrderId = message.OrderId;
        Data.CustomerId = message.CustomerId;

        await _publisher.PublishAsync(new CreateOrder(Data.OrderId, message.CustomerId),
            messageContext: _accessor.CorrelationContext,
            headers: new Dictionary<string, object>
            {
                [SagaHeader] = SagaStates.Pending.ToString()
            });
    }

    public async Task HandleAsync(OrderCreated message, ISagaContext context)
    {
        var tasks = Data.ParcelIds.Select(id =>
            _publisher.PublishAsync(new AddParcelToOrder(Data.OrderId, id, Data.CustomerId),
                messageContext: _accessor.CorrelationContext,
                headers: new Dictionary<string, object>
                {
                    [SagaHeader] = SagaStates.Pending.ToString()
                }));

        await Task.WhenAll(tasks);
    }

    public async Task HandleAsync(ParcelAddedToOrder message, ISagaContext context)
    {
        Data.AddedParcelIds.Add(message.ParcelId);
        if (!Data.AllPackagesAddedToOrder)
        {
            return;
        }

        _logger.LogInformation("Searching for a vehicle...");
        var vehicle = await _vehiclesServiceClient.GetBestAsync();
        Data.VehicleId = vehicle.Id;
        _logger.LogInformation($"Found a vehicle with id: '{vehicle.Id}' for {vehicle.PricePerService}$.");

        _logger.LogInformation($"Reserving a date for vehicle: '{vehicle.Id}'...");
        var reservation = await _resourceReservationsService.GetBestAsync(Data.VehicleId);
        Data.ReservationDate = reservation.DateTime;
        Data.ReservationPriority = reservation.Priority;
        _logger.LogInformation($"Reserved a date: {Data.ReservationDate.Date} for vehicle: '{vehicle.Id}'.");

        await _publisher.PublishAsync(
            new AssignVehicleToOrder(Data.OrderId, Data.VehicleId, Data.ReservationDate),
            messageContext: _accessor.CorrelationContext,
            headers: new Dictionary<string, object>
            {
                [SagaHeader] = SagaStates.Pending.ToString()
            });
    }

    public Task HandleAsync(VehicleAssignedToOrder message, ISagaContext context)
        => _publisher.PublishAsync(new ReserveResource(Data.VehicleId, Data.CustomerId,
                Data.ReservationDate, Data.ReservationPriority),
            messageContext: _accessor.CorrelationContext,
            headers: new Dictionary<string, object>
            {
                [SagaHeader] = SagaStates.Pending.ToString()
            });

    public async Task HandleAsync(OrderApproved message, ISagaContext context)
    {
        _logger.LogInformation($"Completed a saga for order: '{message.OrderId}'.");
        await _publisher.PublishAsync(new MakeOrderCompleted(message.OrderId),
            messageContext: _accessor.CorrelationContext,
            headers: new Dictionary<string, object>
            {
                [SagaHeader] = SagaStates.Completed.ToString()
            });

        await CompleteAsync();
    }

    public Task CompensateAsync(MakeOrder message, ISagaContext context)
        => Task.CompletedTask;

    public Task CompensateAsync(OrderCreated message, ISagaContext context)
        => Task.CompletedTask;

    public Task CompensateAsync(ParcelAddedToOrder message, ISagaContext context)
        => _publisher.PublishAsync(new CancelOrder(message.OrderId, "Because I'm saga"),
            messageContext: _accessor.CorrelationContext,
            headers: new Dictionary<string, object>
            {
                [SagaHeader] = SagaStates.Rejected.ToString()
            });

    public Task CompensateAsync(VehicleAssignedToOrder message, ISagaContext context)
        => Task.CompletedTask;

    public Task CompensateAsync(OrderApproved message, ISagaContext context)
        => Task.CompletedTask;
}
```

saga basically is the class that inherits from either generic `Saga<T>` and none generic `Saga` version, an in this T as we can see is container for business data here `AIMakingOrderData` if we don't need any data for processing this that eventually needs to be persistent some where, if we actually need saga as a core definition in some sort of a `router` and we can use it without data parameter as `Saga` class. then we have definition for the particular steps and we have `ISagaStartAction<MakeOrder>` and saga start action is definition of initial step that something initiate saga and allow us process the messages that come so in this particular order and we do have `MakeOrder` message and mark with this interface `ISagaStartAction` as a initial action, once we get `MakeOrder` message we can start saga and saga eventually accept incoming messages. 

``` csharp
ISagaStartAction<MakeOrder>,
ISagaAction<OrderCreated>,
ISagaAction<ParcelAddedToOrder>,
ISagaAction<VehicleAssignedToOrder>,
ISagaAction<OrderApproved>
```

if we get `OrderCreated` message before `MakeOrder` message, saga will not started because it needs to be initiate with particular message, now if we look for definition for `ISagaStartAction<MakeOrder>` or `ISagaAction<>` we can see two method declaration

``` csharp
public interface ISagaAction<in TMessage>
{
    Task CompensateAsync(TMessage message, ISagaContext context);
    Task HandleAsync(TMessage message, ISagaContext context);
}
```

`HandleAsync` is definition for our business process step, what should I do in this part of my business process for this particular message and `CompensateAsync` we have corresponding method for eventual rollback once we actually decide to roll this transaction so what we want to do about the `Compensation` of this particular message. lets imagine we want handle async in our example `BookFlight` so the compensation method would probably do `BookingCancellation`.

we override `ResolveId` method in this class and this method uses a switch expression on our message type and the Idea about this that saga itself needs some identifier so we might have as a microservice get a lot of message as a same type, we might have a lot of `OrderCreated` messages now we need to distinguish some how, we need to know which saga for which particular user should I restore and to which saga I should pass this particular message. I need some sort of natural key for our saga to actually know corelate my in coming messages with the state eventually persisting. correlationId is not enough because when we send request to our gateway we have some correlationId because some messages involved but again someone else send another api request `UpdateOrder` and there might get another correlationId and if saga derived `OrderUpdated` corelationId on the message broker is not enough.
correlationId in our case if we think about the business process, correlationId might be trick because if we think business process as a do my order automatically. from user's perspective that is one http call to my api so if we remember how correlationID works this will be create once in api gateway and all new messages keep same correlationId we can use it for short living transaction but if we want to model long living transaction it doesn't work.

`resolveId` is responsible for seeking this natural key for example for special message type `MakeOrder` our natural key is `OrderId`. the we have definition for each step in our process manager and first one is receiving `MakeOrder` command that comes to saga and initiate saga and what we do is assign Saga `Data` with som information inside our message that called container nad that will eventually persisted we want starting creating order as part of process manager. so we publish `CreateOrder` and passing some additional data and there is no need save this data because its is of type of generic type that we have in the definition. and it persisted automatically there is no need to actually do this

``` csharp
public async Task HandleAsync(MakeOrder message, ISagaContext context)
{
    _logger.LogInformation($"Started a saga for order: '{message.OrderId}'.");
    Data.ParcelIds.Add(message.ParcelId);
    Data.OrderId = message.OrderId;
    Data.CustomerId = message.CustomerId;

    await _publisher.PublishAsync(new CreateOrder(Data.OrderId, message.CustomerId),
        messageContext: _accessor.CorrelationContext,
        headers: new Dictionary<string, object>
        {
            [SagaHeader] = SagaStates.Pending.ToString()
        });
}
```

then further step of course once we create an order eventually we should receive `OrderCreated` integration event to go step 2 of our saga so we can subscribe to this as a microservice and react with that. what we do here is we send `AddParcelToOrder`command for go to step 3 of our saga and as we can see we use Data that we persisted in the first and we have data `Data.ParcelIds` and we suing this and it restore automatically here so this is step number 2, we have CreatedOrder and we want to assign parcels to order and pass as a command.

``` csharp
public async Task HandleAsync(OrderCreated message, ISagaContext context)
{
    var tasks = Data.ParcelIds.Select(id =>
        _publisher.PublishAsync(new AddParcelToOrder(Data.OrderId, id, Data.CustomerId),
            messageContext: _accessor.CorrelationContext,
            headers: new Dictionary<string, object>
            {
                [SagaHeader] = SagaStates.Pending.ToString()
            }));

    await Task.WhenAll(tasks);
}
```

in step 3 once we get acknowledgment in form of an `ParcelAddedToOrder` integration event we want to check whether all parcels added to my order, and this is place we need to wait for all of parcels to be added and this is a beauty about this approach that we dont need any fluent API or tools to model this approach and if we think about business process that was a place which we send multiple requests because we have `Task.WhenAll` in our before step and we send multiple requests so let say we want create an order for 5 parcel so we send 5 parallel request to our microservice now in this simple if :

``` csharp
Data.AddedParcelIds.Add(message.ParcelId);
if (!Data.AllPackagesAddedToOrder)
{
    return;
}
```
we simply receive the message with ack (integration event ParcelAddedToOrder) so parcel has been added to our order. Do I have all my packages assigned to order? No.so it simply return and we wait for another ack and we wait for all acks from the microservice and we can finally stop searching for the particular vehicle for our order. GetBestAsync by using load balancer we have our AI algorithm for finding best vehicle and the take a reservation by another http call (these are internal service calls) to reserve the resource and finally we publish `AssignVehicleToOrder` command and this was step 3.



``` csharp
public async Task HandleAsync(ParcelAddedToOrder message, ISagaContext context)
{
    Data.AddedParcelIds.Add(message.ParcelId);
    if (!Data.AllPackagesAddedToOrder)
    {
        return;
    }

    _logger.LogInformation("Searching for a vehicle...");
    var vehicle = await _vehiclesServiceClient.GetBestAsync();
    Data.VehicleId = vehicle.Id;
    _logger.LogInformation($"Found a vehicle with id: '{vehicle.Id}' for {vehicle.PricePerService}$.");

    _logger.LogInformation($"Reserving a date for vehicle: '{vehicle.Id}'...");
    var reservation = await _resourceReservationsService.GetBestAsync(Data.VehicleId);
    Data.ReservationDate = reservation.DateTime;
    Data.ReservationPriority = reservation.Priority;
    _logger.LogInformation($"Reserved a date: {Data.ReservationDate.Date} for vehicle: '{vehicle.Id}'.");

    await _publisher.PublishAsync(
        new AssignVehicleToOrder(Data.OrderId, Data.VehicleId, Data.ReservationDate),
        messageContext: _accessor.CorrelationContext,
        headers: new Dictionary<string, object>
        {
            [SagaHeader] = SagaStates.Pending.ToString()
        });
}
        
```

in step 4, finally vehicle assigned to our order with `VehicleAssignedToOrder` integration event so vehicle can deliver our order and we just call `ReserveResource` command and it eventually raise a `OrderApproved` external event that would handle in our step 5

``` csharp
public Task HandleAsync(VehicleAssignedToOrder message, ISagaContext context)
    => _publisher.PublishAsync(new ReserveResource(Data.VehicleId, Data.CustomerId,
            Data.ReservationDate, Data.ReservationPriority),
        messageContext: _accessor.CorrelationContext,
        headers: new Dictionary<string, object>
        {
            [SagaHeader] = SagaStates.Pending.ToString()
        });
```

in step 5 order approved and saga completed the `CompleteAsync()` change state machine state and we marks this as a completed and saga itself not accept any message so its purpose has been fulfilled and no need accept any message. 

``` csharp
public async Task HandleAsync(OrderApproved message, ISagaContext context)
{
    _logger.LogInformation($"Completed a saga for order: '{message.OrderId}'.");
    await _publisher.PublishAsync(new MakeOrderCompleted(message.OrderId),
        messageContext: _accessor.CorrelationContext,
        headers: new Dictionary<string, object>
        {
            [SagaHeader] = SagaStates.Completed.ToString()
        });

    await CompleteAsync();
}
```

if there is any issue we can call `RejectAsync` and we want to cancel this saga but if there is any exception it will called anyway and reject start invoking `CompensateAsync` method but starting from the latest one. so we can think about it as a stack and this will be how we will implement this. in this CompensateAsync we can perform our rollback for each part separately and do whatever we want send another `reject command to` another services and handle Rejected Events in our sagas or call something with http or change in database

if we want to have cover all of the edge cases we probably need to not only subscribe to this successful events, most likely also subscribe for rejected events like `ApproveOrderRejected` we can also cover all rejected events to have all possible failure scenarios. one more thing when we talk about edge cases is consider the outbox pattern and inbox pattern and this is a crucial part. 

now start saga for MakeOrder, we call `orders` endpoint in our internal web api and it will trigger `AIOrderMakerHandler` and then execute our saga with using [Pacco.Services.OrderMaker.rest](../Pacco.Services.OrderMaker/Pacco.Services.OrderMaker.rest) file.

how we can notify user about ongoing process and operation service is nice but it mostly work in one to one scenarios, for example there is one command like a single command and do something and there is an event I did something or couldn't do this because there is an issue, because operation service doesn't have this information, but you have already notice here, we could also somehow correlate this with saga and what we do here is whenever we send a message to message broker with each message we provide some custom saga header and pass some header and include header here for set it to `saga state`,right now we have saga header and this header has this value for example `SagaStates.Pending`.

``` csharp
  private const string SagaHeader = "Saga";

  await _publisher.PublishAsync(new CreateOrder(Data.OrderId, message.CustomerId),
    messageContext: _accessor.CorrelationContext,
    headers: new Dictionary<string, object>
    {
        [SagaHeader] = SagaStates.Pending.ToString()
    });
```
right now if we follow this simple approach where we have `custom header` with the value `current saga state` and we open our operation service, operation service based on this header our generic handler for command and events(`GenericCommandHandler` , `GenericEventHandler`) in operation service. if I receive a message we check whether within this message I have this `saga header` 

``` csharp
var state = messageProperties.GetSagaState() ?? OperationState.Pending;

public static OperationState? GetSagaState(this IMessageProperties messageProperties)
{
    const string sagaHeader = "Saga";
    if (messageProperties?.Headers is null || !messageProperties.Headers.TryGetValue(sagaHeader, out var saga))
    {
        return null;
    }

    return saga is byte[] sagaBytes
        ? Encoding.UTF8.GetString(sagaBytes).ToLowerInvariant() switch
        {
            "pending" => OperationState.Pending,
            "completed" => OperationState.Completed,
            "rejected" => OperationState.Rejected,
            _ => (OperationState?) null
        }
        : null;
}
```
this method just look for header if there is header with this value `Saga` and if there is a value for saga state and it will then check and we take state of overall process based on saga state and for this operation has 3 state `pending`, `rejected`, `completed` and if we follow same conventions for saga state we can validate whether ongoing event or command is part of business transaction or jus simple scenario with one command or event and notify to user 

``` csharp
public class GenericCommandHandler<T> : ICommandHandler<T> where T : class, ICommand
{
    private readonly ICorrelationContextAccessor _contextAccessor;
    private readonly IMessagePropertiesAccessor _messagePropertiesAccessor;
    private readonly IOperationsService _operationsService;
    private readonly IHubService _hubService;

    public GenericCommandHandler(ICorrelationContextAccessor contextAccessor,
        IMessagePropertiesAccessor messagePropertiesAccessor,
        IOperationsService operationsService, IHubService hubService)
    {
        _contextAccessor = contextAccessor;
        _messagePropertiesAccessor = messagePropertiesAccessor;
        _operationsService = operationsService;
        _hubService = hubService;
    }

    public async Task HandleAsync(T command)
    {
        var messageProperties = _messagePropertiesAccessor.MessageProperties;
        var correlationId = messageProperties?.CorrelationId;
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return;
        }

        var context = _contextAccessor.GetCorrelationContext();
        var name = string.IsNullOrWhiteSpace(context?.Name) ? command.GetType().Name : context.Name;
        var userId = context?.User?.Id;
        var state = messageProperties.GetSagaState() ?? OperationState.Pending;
        //we can bypass storing in redis or any persistance mechanism and just send reviced payload to our push notification mechanism directly
        var (updated, operation) = await _operationsService.TrySetAsync(correlationId, userId, name, state);
        if (!updated)
        {
            return;
        }

        switch (state)
        {
            case OperationState.Pending:
                await _hubService.PublishOperationPendingAsync(operation);
                break;
            case OperationState.Completed:
                await _hubService.PublishOperationCompletedAsync(operation);
                break;
            case OperationState.Rejected:
                await _hubService.PublishOperationRejectedAsync(operation);
                break;
            default:
                throw new ArgumentException($"Invalid operation state: {state}", nameof(state));
        }
    }
}
```

when we use distributed transaction? Ideally never. try to re modeling business process and we can somehow encapsulated in a single service and we don't have to  deal with event choreography nor saga process manager because you can jus see there is quite a lot of code added and there is additional application logic has to be written. there some potential edge cases related to handling this rejected event so if one of service does fail we need somehow send a compensation for example command for rollback this action.
we use saga when we have some sort of long living business transaction and example is the situation which we have additional business requirement let say requirement is following:

we have a customer if this customer will create 10 order in our system we would like to give him a discount this is most of the cases will be a business process that would actually last for quite a long time because this might take for some customer take a week and some customer 1 year so basically we deal with such approach and we create a job and this job run in midnight and create a query that will look for the customers that have this threshold reached so this 10 order and we give them discount.
we could create once user register in our system we could create him a saga so saga will create for each customer so when ever customer finish his order we simply subscribe to this saga in the particular event for example order completed and inside saga we keep number of `completed orders` and we simply interment this and once we reach this particular threshold let say 10 orders the saga send a command to discount factors service and this is how we can deal with a long time process and this is more natural of course saga itself is not needed in this particular case because this could be achieve also using event choregraphy in this cases we could have a event handler and this is very nice place to put such a business logic