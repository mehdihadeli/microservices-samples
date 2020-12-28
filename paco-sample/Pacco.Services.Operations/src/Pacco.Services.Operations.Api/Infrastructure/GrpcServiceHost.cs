using System.Collections.Concurrent;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Pacco.Services.Operations.Api.DTO;
using Pacco.Services.Operations.Api.Services;
using Services.Operations;

namespace Pacco.Services.Operations.Api.Infrastructure
{
    public class GrpcServiceHost : GrpcOperationsService.GrpcOperationsServiceBase
    {
        private readonly IOperationsService _operationsService;
        private readonly ILogger<GrpcServiceHost> _logger;
        //https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/blockingcollection-overview
        //http://dotnetpattern.com/csharp-blockingcollection
        //https://weblogs.asp.net/morteza/an-introduction-to-blockingcollection
        //https://makolyte.com/event-driven-dotnet-concurrent-producer-consumer-using-blockingcollection/
        private readonly BlockingCollection<OperationDto> _operations = new BlockingCollection<OperationDto>();

        public GrpcServiceHost(IOperationsService operationsService, ILogger<GrpcServiceHost> logger)
        {
            _operationsService = operationsService;
            _logger = logger;
            _operationsService.OperationUpdated += (s, e) => _operations.TryAdd(e.Operation);
        }

        public override async Task<GetOperationResponse> GetOperation(GetOperationRequest request,
            ServerCallContext context)
        {
            _logger.LogInformation($"Received 'Get operation' (id: {request.Id}) request from: {context.Peer}");

            return Map(await _operationsService.GetAsync(request.Id));
        }

        public override async Task SubscribeOperations(Empty request,
            IServerStreamWriter<GetOperationResponse> responseStream, ServerCallContext context)
        {
            _logger.LogInformation($"Received 'Subscribe operations' request from: {context.Peer}");
            while (true)
            {
                //BlocingCollection offers a method name Take. This method returns (moves) an item from the collections if any exists and otherwise blocks the thread until a new item is available
                //in future (that means a new email is added to the collection later on). So we no longer need to pause the operation for 1 second and then start polling again, or even care about if the collection is empty or not.
                var operation = _operations.Take();
                await responseStream.WriteAsync(Map(operation));
            }
        }

        private static GetOperationResponse Map(OperationDto operation)
            => operation is null
                ? new GetOperationResponse()
                : new GetOperationResponse
                {
                    Id = operation.Id,
                    UserId = operation.UserId,
                    Name = operation.Name,
                    Code = operation.Code,
                    Reason = operation.Reason,
                    State = operation.State.ToString().ToLowerInvariant()
                };
    }
}