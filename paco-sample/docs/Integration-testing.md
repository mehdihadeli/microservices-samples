[End-to-End Integration Testing with NServiceBus](https://jimmybogard.com/end-to-end-integration-testing-with-nservicebus/)

now we focus on the async scenario which will like to send a command but this time using rabbitmq and we will eventually see whether the data has been `persisted to mongodb` and this is lightly more advances because we have now `two piece of infrastructure` and we need to create integration test the we will see whether our microservice `integrate well` with both `mongodb` and `rabbitmq`.

we start with similar piece of code, we create dedicated class for this particular case, we create `Async folder` with message broker involve during this test so lets add new class called `AddResourceTests`, here we use `IDisposable` again because we use `MongoFixture` also `PaccoApplicationFactory`. purpose of having this factory here is `bootstrapping application` once we start the test but we not reach the web api or http-client directly. `Act` method in sync scenario uses http-client because we want send post or get request now we want publish message to particular `exchange` in rabbitmq we can create another `virtual host` for test purpose or different `exchange` for event corrupt real exchange data or different `instance` or `cluster` in rabbitmq whatever you want, but we need something allow us publish the message, we did something like `mongodb` and we have dedicated fixture for rabbitmq. if we go to shared package we will have `RabbitmqFixture` and this is pure rabbitmq client and we have a method publish async that take a message and exchange name so we can use of this particular method inside our `Act`. now we provide namespace and exchange and here if we are using default availability exchange by define a constant string like `Exchange = availability` and now we have fixture ready for usage. we can call our `PublishAsync` method inside `Act` for testing async functionality for `CreateResource` command, and I want to publish particular exchange which in this case is availability.

```csharp
private Task Act(AddResource command) => _rabbitMqFixture.PublishAsync(command, Exchange);
```

now we can thinking about scenario, and we create pretty similar scenario and we create a method called `add_resource_command_should_add_document_with_given_id_to_database` and first step is create the command like sync approach, now we get to trick part because if we think about testing purpose we need to consider 2 purposes, what we should do first? :

- should be first subscribe to the message and wait for this message and then publish actual message
- or should we do other way around and we should publish message and next line subscribe and get this

the issue might have here is that we need to remember that in synchronous scenario that is straight forward and once we get 201 create I'm sure my resource already is in database and this is immediate consistency now I have eventually consistency so I need some sort of trigger to actually know when can I actually look into my database and get the data and this is `pretty tricky part` because we need some sort of mechanism for this purpose:

we can think about most trivial and nice scenario like wait on our Act command and then we wait with Delay in 5 seconds because within this 5 second this command should be processed but this is not the way we should handle things like this, the first issue is that this approach make our test pretty slow because we need to wait for this and even though probably gonna work sometimes.

```csharp
[Fact]
public async Task add_resource_command_should_add_document_with_given_id_to_database()
{
    var command = new AddResource(Guid.NewGuid(), new[] {"tag"});

    await Act(command);

    await Task.Delay(5000);// 5s
}
```

but we will do slightly different things because if we think about how our system react to incoming command, if we get back to whole orchestration process the last part is `publishing` the `integration event`, the event is something already happen in our system so we can usage of this so what we could do? we could `subscribe` to integration event and in our case is `ResourceAdded` integration event so once we get this event we can go to mongodb and look for data and get this we can do this by calling special method inside our rabbitmq fixture we have a method with name of `SubscribeAndGet` that gives us a `tcs` or `TaskCompletionSource`, the TaskCompletionSource main usage can be found when you want to work with `Old Api` written in csharp based on `Event` and we can usage of this if we look to definition of this particular method we have couple of parameter here

```csharp
string exchange, Func<Guid, TaskCompletionSource<TEntity>, Task> onMessageReceived, Guid id
```

first is exchange that we want to subscribe to then we have the function `Func<Guid, TaskCompletionSource<TEntity>, Task> onMessageReceived` which would be nothing more about `callback` so what we want to perform once the message consumed from the queue and then there is id that is id of resource that we would to like get from database because name is `SubscribeAndGet` so we will make a few things, in this method we create exchange (this methods are idempotent and might already created) and then we create a queue and bind to this exchange (we create a test queue for our test and bind to main exchange for prevent data corruption) and this similar that we seed before in subscriber, and here we have a random queue name and then we have `Receive` callback from rabitmq client library and this is actually rabbitmq subscriber and in this callback we can do any thing we want to do, and inside in this callback we call our callback `onMessageReceived` and pass id and task completion source to this callback method and what we can do is pass function to get data from mongodb and in mongo fixture we have a method that get `id` and a `TaskCompletionSource`

```csharp
public TaskCompletionSource<TEntity> SubscribeAndGet<TMessage, TEntity>(string exchange,
    Func<Guid, TaskCompletionSource<TEntity>, Task> onMessageReceived, Guid id)
{
    var taskCompletionSource = new TaskCompletionSource<TEntity>();

    _channel.ExchangeDeclare(exchange: exchange,
        durable: true,
        autoDelete: false,
        arguments: null,
        type: "topic");

    var queue = $"test_{SnakeCase(typeof(TMessage).Name)}";

    _channel.QueueDeclare(queue: queue,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    _channel.QueueBind(queue, exchange, SnakeCase(typeof(TMessage).Name));
    _channel.BasicQos(0, 1, false);

    var consumer = new AsyncEventingBasicConsumer(_channel);
    consumer.Received += async (model, ea) =>
    {
        var body = ea.Body;
        var json = Encoding.UTF8.GetString(body);
        var message = JsonConvert.DeserializeObject<TMessage>(json);

        await onMessageReceived(id, taskCompletionSource);
    };

    _channel.BasicConsume(queue: queue,
        autoAck: true,
        consumer: consumer);

    return taskCompletionSource;
}
```

```csharp
public async Task GetAsync(TKey expectedId, TaskCompletionSource<TEntity> receivedTask)
{
    if (expectedId is null)
    {
        throw new ArgumentNullException(nameof(expectedId));
    }

    var entity = await GetAsync(expectedId);

    if (entity is null)
    {
        receivedTask.TrySetCanceled();
        return;
    }

    receivedTask.TrySetResult(entity);
}
```

so if data is there or not we set task completion source to either `TrySetResult` for complete task or `TrySetCanceled` to cancel task. so we will call SubscribeAndGet method and method return `TaskCompletionSource` and `TaskCompletionSource` has a property called Task which we could await so awaiting will last as long as we wait for the subscribed message and our case is `ResourceAdded` and once message got received we pass callback to this `GetAsync` method and we use also Id and look for data.

lets to try use of it

```csharp
[Fact]
public async Task add_resource_command_should_add_document_with_given_id_to_database()
{
    var command = new AddResource(Guid.NewGuid(), new[] {"tag"});

    var tcs = _rabbitMqFixture.SubscribeAndGet<ResourceAdded, ResourceDocument>(Exchange, _mongoDbFixture.GetAsync, command.ResourceId);

    await Act(command);

    var document = await tcs.Task;

    document.ShouldNotBeNull();
    document.Id.ShouldBe(command.ResourceId);
    document.Tags.ShouldBe(command.Tags);
}
```

we want to subscribe to `ResourceAdded` event because once our AddResource handler complete, it should publish `ResourceAdded` event to booker and we expect to have this `ResourceDocument` in database and function or callback here is `_mongoDbFixture.GetAsync` and callback will receive data. we subscribe first and now we can make a publish because do this other way around so publishing message first then subscribe makes no sense to us because we have the situation we publish the message then we do subscription but we are too late. once we publish command and the resource will actually added to database and raise an event on the broker we can await on this task with `tcs.Task`. and this task will return `MongoDocument` and this set data for our task will do in GetAsync in mongo and will change state of our task to complete and execution go further from `await` keyword.

now we can do some assertion like these:

```csharp
  document.ShouldNotBeNull();
  document.Id.ShouldBe(command.ResourceId);
  document.Tags.ShouldBe(command.Tags);
```

so at first we want to send a command to message broker then our api that has a subscription to this command process this asynchronously and once it publishes `ResourceAdded` event we will just wait for event to be process using this `tcs`.

the above approach actually is a End to End Integration Test (mixing end to end and integration test), but also we can do integration test jus ba testing our `HandlerAsync` method or CreateResourceHandler directly.

```csharp
   private readonly ICommandHandler<AddResource> _handler;
   private Task Act(AddResource command) => _handler.HandleAsync(command);
```

the sample available [here](../Pacco.Services.Availability/tests/Pacco.Services.Availability.Tests.Integration/Async/AddResourceTest2.cs)