# DeviceClient Requirements

## Overview

DeviceClient class allows devices to communicate with an Azure IoTHub.  It can send event messages and receive messages.  The client handles communication with IoTHub through a transport protocol specified.
 
## References


##Exposed API
```csharp

public sealed class DeviceClient
#if !WINDOWS_UWP && !PCL
    , IDisposable
#endif
{
    DeviceClient(IotHubConnectionString iotHubConnectionString, ITransportSettings[] transportSettings)

    DefaultDelegatingHandler CreateTransportHandler(IotHubConnectionString iotHubConnectionString, ITransportSettings transportSetting)

    public static DeviceClient Create(string hostname, IAuthenticationMethod authenticationMethod)
    public static DeviceClient Create(string hostname, IAuthenticationMethod authenticationMethod, TransportType transportType)
    public static DeviceClient Create(string hostname, IAuthenticationMethod authenticationMethod, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArrayAttribute] ITransportSettings[] transportSettings)

    public static DeviceClient CreateFromConnectionString(string connectionString)
    public static DeviceClient CreateFromConnectionString(string connectionString, string deviceId)
    public static DeviceClient CreateFromConnectionString(string connectionString, TransportType transportType)
    public static DeviceClient CreateFromConnectionString(string connectionString, string deviceId, TransportType transportType)
    public static DeviceClient CreateFromConnectionString(string connectionString, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArrayAttribute] ITransportSettings[] transportSettings)
    public static DeviceClient CreateFromConnectionString(string connectionString, string deviceId, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArrayAttribute] ITransportSettings[] transportSettings)

    public AsyncTask OpenAsync()

    public AsyncTask CloseAsync()

    public AsyncTaskOfMessage ReceiveAsync()
    public AsyncTaskOfMessage ReceiveAsync(TimeSpan timeout)

    public AsyncTask CompleteAsync(string lockToken)
    public AsyncTask CompleteAsync(Message message)

    public AsyncTask AbandonAsync(string lockToken)
    public AsyncTask AbandonAsync(Message message)

    public AsyncTask RejectAsync(string lockToken)
    public AsyncTask RejectAsync(Message message)

    public AsyncTask SendEventAsync(Message message)
    public AsyncTask SendEventBatchAsync(IEnumerable<Message> messages)

    public AsyncTask UploadToBlobAsync(String blobName, System.IO.Stream source)

    public void Dispose()

    public RetryStrategyType RetryStrategy

    public int OperationTimeoutInMilliseconds

    public event ConnectEventHandler OnConnect;

    static ITransportSettings[] PopulateCertificateInTransportSettings(IotHubConnectionStringBuilder connectionStringBuilder, TransportType transportType)
    static ITransportSettings[] PopulateCertificateInTransportSettings(IotHubConnectionStringBuilder connectionStringBuilder, ITransportSettings[] transportSettings)

    public Task<Twin> GetTwinAsync();
    public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties)
    public Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext)
    
    public async Task SendEventAsync(string outputName, Message message);
    public async Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages);
    public async Task SetEventHandlerAsync(string inputName, MessageHandler messageHandler, object userContext);
    public async Task SetEventDefaultHandlerAsync(MessageHandler messageHandler, object userContext);

    internal async Task OnReceiveEventMessageCalled(EventMessageInternal eventMessageInternal);

    public async Task<Twin> GetTwinAsync();
    public async Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext)
    public void SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler statusChangesHandler)
}
```


### OpenAsync
```csharp
public AsyncTask OpenAsync()
```

**SRS_DEVICECLIENT_28_006: [** The async operation shall retry using retry policy specified in the RetryStrategy property. **]**

**SRS_DEVICECLIENT_28_007: [** The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable (authentication, quota exceed) error occurs. **]**


### ReceiveAsync
```csharp
public AsyncTaskOfMessage ReceiveAsync()
public AsyncTaskOfMessage ReceiveAsync(TimeSpan timeout)
```

**SRS_DEVICECLIENT_28_010: [** The async operation shall retry using retry policy specified in the RetryStrategy property. **]**

**SRS_DEVICECLIENT_28_011: [** The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable (authentication, quota exceed) error occurs. **]**


### CompleteAsync
```csharp
public AsyncTask CompleteAsync(string lockToken)
public AsyncTask CompleteAsync(Message message)
```

**SRS_DEVICECLIENT_28_012: [** The async operation shall retry using retry policy specified in the RetryStrategy property. **]**

**SRS_DEVICECLIENT_28_013: [** The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error (authentication, quota exceed) occurs. **]**


### AbandonAsync
```csharp
public AsyncTask AbandonAsync(string lockToken)
public AsyncTask AbandonAsync(Message message)
```

**SRS_DEVICECLIENT_28_014: [** The async operation shall retry using retry policy specified in the RetryStrategy property. **]**

**SRS_DEVICECLIENT_28_015: [** The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error (authentication, quota exceed) occurs. **]**


### RejectAsync
```csharp
public AsyncTask RejectAsync(string lockToken)
public AsyncTask RejectAsync(Message message)
```

**SRS_DEVICECLIENT_28_016: [** The async operation shall retry using retry policy specified in the RetryStrategy property. **]**

**SRS_DEVICECLIENT_28_017: [** The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error (authentication, quota exceed) occurs. **]**


### SendEventAsync
```csharp
public AsyncTask SendEventAsync(Message message)
public AsyncTask SendEventBatchAsync(IEnumerable<Message> messages)
```

**SRS_DEVICECLIENT_28_018: [** The async operation shall retry using retry policy specified in the RetryStrategy property. **]**

**SRS_DEVICECLIENT_28_019: [** The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error (authentication or quota exceed) occurs. **]**


### OnMethodCalled
```csharp
internal async Task OnMethodCalled(MethodRequestInternal methodRequestInternal)
```

**SRS_DEVICECLIENT_10_011: [** The OnMethodCalled shall invoke the associated MethodHandler. **]**

**SRS_DEVICECLIENT_24_002: [** The OnMethodCalled shall invoke the default delegate if there is no specified delegate for that method. **]**

**SRS_DEVICECLIENT_03_012: [** If the MethodResponse does not contain result, the MethodResponseInternal constructor shall be invoked with no results. **]**

**SRS_DEVICECLIENT_03_013: [** Otherwise, the MethodResponseInternal constructor shall be invoked with the result supplied. **]**

**SRS_DEVICECLIENT_10_012: [** If the given methodRequestInternal argument is null, failed silently **]**

**SRS_DEVICECLIENT_10_013: [** If the given method does not have an associated delegate and no default delegate was registered, respond with status code 501 (METHOD NOT IMPLEMENTED) **]**

**SRS_DEVICECLIENT_28_020: [** If the given methodRequestInternal data is not valid json, respond with status code 400 (BAD REQUEST) **]**

**SRS_DEVICECLIENT_28_021: [** If the MethodResponse from the MethodHandler is not valid json, respond with status code 500 (USER CODE EXCEPTION) **]**


### SetMethodHandler
```csharp
public void SetMethodHandler(string methodName, MethodCallback methodHandler, object userContext)
```

**SRS_DEVICECLIENT_10_001: [** It shall lazy-initialize the DeviceMethods property. **]**

**SRS_DEVICECLIENT_10_005: [** It shall EnableMethodsAsync when called for the first time. **]**

**SRS_DEVICECLIENT_10_002: [** If the given methodName has an associated delegate, the existing delegate shall be replaced with the newly given delegate. **]**

**SRS_DEVICECLIENT_10_003: [** The given delegate will only be added if it is not null. **]**

**SRS_DEVICECLIENT_10_004: [** The DeviceMethods property shall be deleted if the last delegate has been removed. **]**

**SRS_DEVICECLIENT_10_006: [** It shall DisableMethodsAsync when the last delegate has been removed. **]**

### SetMethodHandlerAsync
```csharp
public void SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext)
```

**SRS_DEVICECLIENT_10_001: [** It shall lazy-initialize the DeviceMethods property. **]**

**SRS_DEVICECLIENT_10_005: [** It shall EnableMethodsAsync when called for the first time. **]**

**SRS_DEVICECLIENT_10_002: [** If the given methodName has an associated delegate, the existing delegate shall be replaced with the newly given delegate. **]**

**SRS_DEVICECLIENT_10_003: [** The given delegate will only be added if it is not null. **]**

**SRS_DEVICECLIENT_10_004: [** The DeviceMethods property shall be deleted if the last delegate has been removed. **]**

**SRS_DEVICECLIENT_10_006: [** It shall DisableMethodsAsync when the last delegate has been removed. **]**

### SetMethodDefaultHandlerAsync
```csharp
public void SetMethodDefaultHandlerAsync(MethodCallback methodHandler, object userContext)
```

**SRS_DEVICECLIENT_10_005: [** It shall EnableMethodsAsync when called for the first time if there is no associated delegate . **]**

**SRS_DEVICECLIENT_24_001: [** If the default callback has already been set, it is replaced with the new callback. **]**

**SRS_DEVICECLIENT_10_006: [** It shall DisableMethodsAsync when the last delegate has been removed. **]**

### RetryStrategy
```csharp
public RetryStrategyType RetryStrategy
```

**SRS_DEVICECLIENT_28_001: [** This property shall be defaulted to the exponential retry strategy with backoff parameters for calculating delay in between retries. **]** 


### OperationTimeoutInMilliseconds
```csharp
public uint OperationTimeoutInMilliseconds
```

**SRS_DEVICECLIENT_28_002: [** This property shall be defaulted to 240000 (4 minutes). **]**

**SRS_DEVICECLIENT_28_003: [** If this property is set to 0, subsequent operations shall be retried indefinitely until successful or until an unrecoverable error (authentication or quota exceed) is detected **]**


### GetTwinAsync
```csharp
public Task<Twin> GetTwinAsync();
```

**SRS_DEVICECLIENT_18_001: [** `GetTwinAsync` shall call `SendTwinGetAsync` on the transport to get the twin state **]**


### UpdateReportedPropertiesAsync
```csharp
public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties)
```

**SRS_DEVICECLIENT_18_002: [** `UpdateReportedPropertiesAsync` shall call `SendTwinPatchAsync` on the transport to update the reported properties **]**

**SRS_DEVICECLIENT_18_006: [** `UpdateReportedPropertiesAsync` shall throw an `ArgumentNull` exception if `reportedProperties` is null **]**
 
### SetDesiredPropertyUpdateCallbackAsync
```csharp
public Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext)

```

**SRS_DEVICECLIENT_18_003: [** `SetDesiredPropertyUpdateCallbackAsync` shall call the transport to register for PATCHes on it's first call. **]**

**SRS_DEVICECLIENT_18_004: [** `SetDesiredPropertyUpdateCallbackAsync` shall not call the transport to register for PATCHes on subsequent calls. **]**

**SRS_DEVICECLIENT_18_005: [** When a patch is received from the service, the `callback` shall be called. **]**

**SRS_DEVICECLIENT_18_007: [** `SetDesiredPropertyUpdateCallbackAsync` shall throw an `ArgumentNull` exception if `callback` is null **]**
 

### SetConnectionStatusChangesHandler
```csharp
public void SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler statusChangesHandler)
```

**SRS_DEVICECLIENT_28_025: [** `SetConnectionStatusChangesHandler` shall set connectionStatusChangesHandler **]**

**SRS_DEVICECLIENT_28_026 [** `SetConnectionStatusChangesHandler` shall unset connectionStatusChangesHandler if `statusChangesHandler` is null **]**


### OnConnectionClosed
```csharp
internal async void OnConnectionClosed(object sender, ConnectionEventArgs e)
```

**SRS_DEVICECLIENT_28_022: [** `OnConnectionClosed` shall invoke the RecoverConnections process. **]**

**SRS_DEVICECLIENT_28_023: [** `OnConnectionClosed` shall invoke the connectionStatusChangesHandler if ConnectionStatus is changed. **]**

**SRS_DEVICECLIENT_28_027: [** `OnConnectionClosed` shall invoke the connectionStatusChangesHandler if RecoverConnections throw exception **]**
**SRS_DEVICECLIENT_28_023: [** If the invoked operations throw exception, the OnConnectionClosed shall failed silently **]**


### OnConnectionOpened
```csharp
internal async void OnConnectionOpened(object sender, ConnectionEventArgs e)
```

**SRS_DEVICECLIENT_28_024: [** `OnConnectionOpened` shall invoke the connectionStatusChangesHandler if ConnectionStatus is changed **]**


### SendEventAsync
```csharp
public async Task SendEventAsync(string outputName, Message message)
public AsyncTask SendEventBatchAsync(string outputName, IEnumerable<Message> messages)
```

**SRS_DEVICECLIENT_10_011: [** The `SendEventAsync` operation shall retry sending `message` until the `BaseClient::RetryStrategy` tiemspan expires or unrecoverable error (authentication or quota exceed) occurs. **]**
**SRS_DEVICECLIENT_10_012: [** If `outputName` is `null` or empty, an `ArgumentNullException` shall be thrown. **]**
**SRS_DEVICECLIENT_10_013: [** If `message` is `null` or empty, an `ArgumentNullException` shall be thrown. **]**
**SRS_DEVICECLIENT_10_014: [** The `SendEventBatchAsync` operation shall retry sending `messages` until the `BaseClient::RetryStrategy` tiemspan expires or unrecoverable error (authentication or quota exceed) occurs. **]**
**SRS_DEVICECLIENT_10_015: [** The `outputName` property of a given `message` shall be assigned the value `outputName` before submitting each request to the transport layer. **]**

### OnReceiveEventMessageCalled
```csharp
internal async Task OnReceiveEventMessageCalled(EventMessageInternal eventMessageInternal)
```

**SRS_DEVICECLIENT_33_001: [** If the given eventMessageInternal argument is null, fail silently **]**
**SRS_DEVICECLIENT_33_006: [** The OnReceiveEventMessageCalled shall get the default delegate if a delegate has not been assigned. **]**
**SRS_DEVICECLIENT_33_002: [** The OnReceiveEventMessageCalled shall invoke the specified delegate. **]**


### SetEventHandlerAsync
```csharp
public async Task SetEventHandlerAsync(string input, MessageHandler messageHandler, object userContext)
public async Task SetEventDefaultHandlerAsync(MessageHandler messageHandler, object userContext)
```

**SRS_DEVICECLIENT_33_003: [** It shall EnableEventReceiveAsync when called for the first time. **]**
**SRS_DEVICECLIENT_33_005: [** It shall lazy-initialize the receiveEventEndpoints property. **]**
**SRS_DEVICECLIENT_33_004: [** It shall call DisableEventReceiveAsync when the last delegate has been removed. **]**

### GetTwinAsync
```csharp
public async Task<Twin> GetTwinAsync()
```

**SRS_DEVICECLIENT_13_031: [** `GetTwinAsync` shall return the module twin's desired properties and metadata. **]
