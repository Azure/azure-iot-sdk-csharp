namespace Microsoft.Azure.Devices.Client
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Shared;

    public interface IModuleClient
    {
        int DiagnosticSamplingPercentage { get; set; }

        uint OperationTimeoutInMilliseconds { get; set; }

        string ProductInfo { get; set; }

        Task AbandonAsync(Message message);

        Task AbandonAsync(string lockToken);

        Task CloseAsync();

        Task CompleteAsync(Message message);
        
        Task CompleteAsync(string lockToken);
        
        Task<Twin> GetTwinAsync();

        Task<MethodResponse> InvokeMethodAsync(string deviceId, MethodRequest methodRequest);

        Task<MethodResponse> InvokeMethodAsync(string deviceId, MethodRequest methodRequest, CancellationToken cancellationToken);

        Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId, MethodRequest methodRequest);

        Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId, MethodRequest methodRequest, CancellationToken cancellationToken);
        Task OpenAsync();

        Task SendEventAsync(Message message);

        Task SendEventAsync(string outputName, Message message);

        Task SendEventBatchAsync(IEnumerable<Message> messages);

        Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages);

        void SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler statusChangesHandler);
        
        Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext);
        
        Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler, object userContext);
        
        Task SetMessageHandlerAsync(MessageHandler messageHandler, object userContext);

        Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler, object userContext);

        Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext);

        void SetRetryPolicy(IRetryPolicy retryPolicy);

        Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties);
    }
}