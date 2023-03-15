# SDK version 2 migration guide

This document outlines the changes made from this library's v1 releases to its v2 releases.
Since this is a major version update, there are a number of breaking changes that will affect compilation and runtime behavior.
Provided here are lists of the notable changes as well as a mapping from v1 to v2 APIs to aid migrating.

## Table of contents

 - [Why the v1 SDK is being replaced](#why-the-v1-sdk-is-being-replaced)
 - [What will happen to the v1 SDK](#What-will-happen-to-the-v1-sdk)
 - [Migration guide](#migration-guide)
   - [IoT hub device client](#iot-hub-device-client)
   - [IoT hub service client](#iot-hub-service-client)
   - [Device Provisioning Service (DPS) device client](#dps-device-client)
   - [DPS service client](#dps-service-client)
   - [Security provider client](#security-provider-client)
 - [Frequently asked questions](#frequently-asked-questions)

## Why the v1 SDK is being replaced

There are a number of reasons why the Azure IoT SDK team chose to do a major version revision. Here are a few of the more important reasons:

### Removing or upgrading several NuGet dependencies.

The v1 SDK is supplied in many NuGet packages. This causes several problems of discoverability and challenges of managing updates.
In v2, we've aimed to simplify this experience.

- Updated in place
  - Microsoft.Azure.Devices (IoT hub service)
  - Microsoft.Azure.Devices.Client (IoT hub device)
  - Microsoft.Azure.Devices.Provisioning.Client
  - Microsoft.Azure.Devices.Provisioning.Service
- Removed
  - Microsoft.Azure.Devices.Shared
    > We removed as much shared code as possible.
    > Classes, like Twin, have moved to the libraries that use them. They've been trimmed down to only include properties and methods that are applicable to that client, which removes confusing/useless API surface.
  - Microsoft.Azure.Devices.Provisioning.Transport.Amqp
    > Rolled into Microsoft.Azure.Devices.Provisioning.Client.
  - Microsoft.Azure.Devices.Provisioning.Transport.Http
    > Rolled into Microsoft.Azure.Devices.Provisioning.Client.
  - Microsoft.Azure.Devices.Provisioning.Transport.Mqtt
    > Rolled into Microsoft.Azure.Devices.Provisioning.Client.
  - Microsoft.Azure.Devices.Provisioning.Security.Tpm
    > Deprecated.
  - From preview001, Microsoft.Azure.Devices.Authentication.
    > Rolled into Microsoft.Azure.Devices.Provisioning.Client.

### Namespace changes

Namespaces have been simplified to reduce confusion and improve discoverability of all available types in the library.
Each client library has its own namespace, and the namespace is the same as the package name.

The folllowing namespaces have been deprecated:

- `Microsoft.Azure.Devices.Shared`
- `Microsoft.Azure.Devices.Common.Exceptions`
- `Microsoft.Azure.Devices.Client.Exceptions`

### Consolidating IoT hub service clients

Many existing client clients (e.g., RegistryManager, ServiceClient) were confusingly named and contained methods that weren't always consistent with the client's assumed responsibilities.

1. It isn't always clear based on the name which classes were clients.
1. It is difficult to know which client is needed for different operations.
1. It is overly difficult to need to initialize each client and manage lifetime.

With the advent of v2, there is **one** client to initalize, `IotHubServiceClient`.
From it, one will find subclients with names representing those kinds of operations.
For example, all operations for devices are found under the `IotHubServiceClient.Devices` subclient, twins under `IothubServiceClient.Twins`, and so forth.

The goal is to simplify client lifetime and operation discovery.

### Client constructors and mocking

Existing clients had a mix of standard constructors (`new RegistryManager(...)`) and static builder methods (`DeviceClient.CreateFromConnectionString(...)`) that caused some confusion among users and prohibited mocking.

The factory methods have been removed and the addition of constructors in clients also enables unit test mocking.

### Exception handling

Exception handling in the v1 SDK required a wide variety of exception types to be caught. Many exception types have been consolidated.

The v2 strategy can be grouped into 3 categories.

1. Parameter validation and invalid client state. These are standard .NET exception types. The following exceptions may be observed:
  - `ArgumentException` for basic parameter validation.
    - Also, `ArgumentNullException` and `ArgumentOutOfRangeException` inherit from it and provide more specific feedback.
  - `FormatException` when a string parameter format does not match what is expected (e.g., connection string with embedded key/value pairs).
  - `InvalidOperationException`
    - For example, when calling device client operations before explicility calling `IotHubDeviceClient.OpenAsync()` first.
    - When some runtime validated user input is invalid in some other way, for example, importing or exporting devices where there is a null value in the list.
    - `ObjectDisposedException` when the client has been disposed but an operation has been issued.
1. Handle canceled operations.
  - `OperationCanceledException` when a user's cancelation token was signaled and the operation was stopped prematurely.
    > All async operations accept an optional `CancellationToken` parameter.
    > It is recommended to use cancelation tokens with a timed expiration as the SDK no longer has a default operation timeout.
    > Without that, an operation could continue indefinitely, depending on retry policy.
1. When an operation fails due to a service error or network issues prevent issuance.
  - `IotHubClientException` for device client and `IotHubServiceException` for service client for any exceptions arising from communication attempts with IoT hub.
    - Review the `IsTransient` property to determine if an exception is transient.
    - Review the `ErrorCode` property for specific, structured error details.
    - For the service client only, the `StatusCode` property is the HTTP status response.
    - The exception message is meant for human readable expanation.
    - Inner exceptions may offer more insight and are worth logging out.
    - `TrackingId` is for uniquely identifying specific operations and are useful in sharing with IoT hub support to assist them in more quickly identifying errors that may have been logged by the service.
  - `ProvisioningClientException` for provisioning device client and `ProvisioningServiceException` for provisioning service client for exceptions arising from communication attempts with DPS.
    > As with the IoT hub exceptions, primarily observe the `IsTransient` and `ErrorCode` properties.
    > Other properties may be valuable for logging or debugging.

### Connection monitoring and client lifetime

The IoT hub device client now caches the latest connection status information so the client app does not have to do it. It can be found at `IotHubDeviceClient.ConnectionStatusInfo` (same property for module client). It is useful, for example, in avoiding starting an operation when the client is temporarily disconnected.

In the connection status callback, a third value, `RecommendedAction`, is now provided to assist in knowing what action the client app is recommended to take. Previously, one would need to know based on every status and reason what action the client app should take, a nearly impossible task without reading a code sample.

## What will happen to the v1 SDK

After the v2 SDK is released as GA (general availability), meaning it is no longer only in preview, there will be one final LTS of the v1 SDK released that we will support like any other LTS release (security bug fixes, some non-security bug fixes as needed).
Users are still encouraged to migrate to v2 when they have the chance.

> For more details on LTS releases, see [this document](./readme.md#long-term-support-lts).

## Migration guide

This guide has been organized by the previous clients.
Find a client you currently use below, read the table of API name changes and use that to update your client app for when type and property names have changed.

### General guidance

- Many models have changed to be nullable to prevent default values (e.g., DateTimeOffset defaults to Jan 01 0001) instead of null.
  - This may mean having to change referencing code to use the `Value` property or the null conditional operator (e.g., given a model named `model` with a nullable property named `NullableProperty`: `model.NullableProperty.Value.SubProperty` or `model.NullableProperty?.SubProperty`) to avoid a NullReferenceException.
- Most properties that were `DateTime` have changed to the [preferred](https://learn.microsoft.com/dotnet/standard/datetime/choosing-between-datetime#the-datetimeoffset-structure) `DateTimeOffset`.

### IoT hub device clients

#### DeviceClient

#### Notable breaking changes

- The transport default has changed from AMQP (TCP with web socket fail over) to MQTT TCP.
  - To override the transport default, create an instance of `IotHubClientOptions` and pass an instance of the transport settings you wish to use (i.e., `IotHubClientMqttSettings`, `IotHubClientAmqpSettings`).
  - TCP will be the default. For web socket, pass `IotHubClientTransportProtocol.WebSocket` to the transport settings constructors.
- HTTP has been removed as a transport option.
  > It had very limited support across the device options and some APIs behaved differently.
  > We feel this caused more harm than good and as such it was best to remove.
- Some options that were previously set in the `DeviceClient` constructor are now in the optional `IotHubClientOptions` parameter.
- The connection status callback parameters have changed. Instead of two parameters, a class with several properties is provided.
  - Two properties enum values are mostly the same as before, with some renames (underscores removed) and obsolete members removed.
  - A new property has been added with a recommended action, which a device developer may observe or ignore.
- The file upload method has been split into the three individual steps that this method used to take. See [this file upload sample](./iothub/device/samples/getting%20started/FileUploadSample/) for an example of how to do file upload using these discrete steps.
- Cloud-to-device messages can be received by calling `SetMessageIncomingMessageCallbackAsync` and providing a callback. Users no longer need to poll for messages with `ReceiveAsync`.
- Support for sending a batch of events over MQTT by calling `SendEventBatchAsync` has been removed. MQTT v3.1 does not support true batching but instead sends the messages one after another. True batching is still supported over AMQP.
- Several callback handler set methods and definitions have changed, losing the `userContext` parameter which was deemed unnecessary and a vestige of the C device client.
- The exponential back-off retry policy has updated parameters and logic.
- Remote certificate validation is no natively longer supported for AMQP web socket connections.
  - The supported workaround is to provide a client web socket instance in the client options.
- The authentication classes for devices and modules have been consolidated.
- Reduced access levels to classes and methods that were never intended to be public where possible.
- The device and module clients now only support `IAsyncDisposable`, which will ensure `CloseAsync()` is called before disposing.
  - The syntax for this might be quite new for some and feel awkward. You can choose from the following options:
    1. Manually call `await client.DisposeAsync();`.
    1. Initialize the client with the **await** keyword: `await using client = new IotHubDeviceClient(...);`. The client will be disposed when it goes out of scope.
  - For more information, see <https://learn.microsoft.com/dotnet/api/system.iasyncdisposable>.
  - `DirectMethodRequest` constructor for module-initiated direct method calls requires the method name as a parameter; the property is not settable directly.

#### Notable additions

- The device and module clients now have a property (e.g., `IotHubDeviceClient.ConnectionStatusInfo`) with the latest connection status information on it, eliminating the need for a connection status callback method to cache the latest values.
- Added support for setting a client web socket instance in the client options so that users can have better control over AMQP web socket connections.
- The library now includes 2 new `IIotHubClientRetryPolicy` implementations: `IotHubClientIncrementalDelayRetryPolicy` and `IotHubClientFixedDelayRetryPolicy`.
- The client can now be re-opened after it has been closed, removing the need to close/dispose/initialize/open; simply initialize/open, then if necessary close/open, and finally when done dispose.
  > Be advised, subscriptions do not carry over when the client is re-opened.
  >
  > It cannot be re-opened after disposal.

#### API mapping

| v1 API | Equivalent v2 API | Notes |
|:---|:---|:---|
| `DeviceClient` | `IotHubDeviceClient` | Specify the service it is a device client for. |
| `DeviceClient.Dispose()` | `IotHubDeviceClient.DisposeAsync()` | Ensures the client is closed before disposing. |
| `DeviceClient.SendEventAsync(...)` | `IotHubDeviceClient.SendTelemetryAsync(TelemetryMessage, ...)` | Even our public documentation calls this telemetry, so we renamed the method to describe this better.¹ |
| `DeviceClient.SendEventBatchAsync(...)` | `IotHubDeviceClient.SendTelemetryAsync(IEnumerable<TelemetryMessage>, ...)` | This is now only supported over AMQP. Support over MQTT has been removed. Also, see¹. |
| `DeviceClient.SetConnectionStatusChangesHandler(...)` | `IotHubDeviceClient.ConnectionStatusChangeCallback` | Local operation doesn't require being a method. |
| `DeviceClient.SetReceiveMessageHandlerAsync(...)` | `IotHubDeviceClient.SetIncomingMessageCallbackAsync(...)` | Disambiguate from telemetry messages. |
| `DeviceClient.GetTwinAsync(...)` | `IotHubDeviceClient.GetTwinPropertiesAsync(...)` | The device client doesn't get the full twin, just the properties so this helps avoid that confusion.² |
| `Twin` | `TwinProperties` | See² |
| `Twin.Properties.Desired` | `TwinProperties.Desired` | See² |
| `Twin.Properties.Reported` | `TwinProperties.Reported` | See² |
| `MessageResponse` | `MessageAcknowledgement` | It isn't a full response, just a simple acknowledgement. |
| `Message` | `TelemetryMessage`, `IncomingMessage` | Distinguished between the different messaging operations. |
| `DeviceClient.SetRetryPolicy(...)` | `IotHubClientOptions.RetryPolicy` | Should be specified at initialization time, and putting it in the client options object reduces the client API surface. |
| `ExponentialBackOff` | `IotHubClientExponentialBackOffRetryPolicy` | Clarify it is a retry policy. |
| `Message.CreationTimeUtc` | `TelemetryMessage.CreatedOnUtc`, `IncomingMessage.CreatedOnUtc` | Conforming to the naming guidelines by the Azure SDK team, where DateTime/Offset types have an "On" suffix (and "Utc" suffix when explicitly in UTC).³ |
| `Message.EnqueuedTimeUtc` | `TelemetryMessage.EnqueuedtimeUtc`, `IncomingMessage.EnqueuedTimeUtc` | See³ |
| `Message.ExpiryTimeUtc` | `TelemetryMessage.ExpiresOnUtc`, `IncomingMessage.ExpiresOnUtc` | See³ |
| `MethodRequest` | `DirectMethodRequest` | Use full name of the operation type.⁴ |
| `MethodResponse` | `DirectMethodResponse` | See⁴ |
| `IotHubException` | `IotHubClientException` | Specify the exception is for Hub device and module client only. |
| `DeviceAuthenticationWithTokenRefresh` and `ModuleAuthenticationWithTokenRefresh` | `ClientAuthenticationWithTokenRefresh` | More descriptive naming and reduce duplication.⁵ |
| `DeviceAuthenticationWithToken` and `ModuleAuthenticationWithToken` | `ClientAuthenticationWithSharedAccessSignature` | See⁵ |
| `DeviceAuthenticationWithSakRefresh` and `ModuleAuthenticationWithSakRefresh` | `ClientAuthenticationWithSharedAccessKeyRefresh` | See⁵ |
| `AuthenticationWithTokenRefresh.SafeCreateNewToken(...)` and derived classes | `ClientAuthenticationWithTokenRefresh.SafeCreateNewTokenAsync(...)` and derived classes. | Async suffix for async methods. |
| `RetryPolicyBase` | `IIotHubClientRetryPolicy` | Introducing an interface for client retry policy. |

#### ModuleClient

The device client and module client share a lot of API surface and underlying implementation. See changes to the IoT hub [device client](#deviceclient).

#### Notable breaking changes

| v1 API | Equivalent v2 API | Notes |
|:---|:---|:---|
| `ModuleClient.SendEventAsync(string outputName, ...)` | `IotHubModuleClient.SendMessageToRouteAsync(string outputName, ...)` | Change the name to be more descriptive about sending messages between Edge modules.¹ |
| `ModuleClient.SendEventBatchAsync(string outputName, ...)` | `IotHubModuleClient.SendMessagesToRouteAsync(string outputName, ...)` | See¹ |

#### Notable additions

N/A

#### API mapping

| v1 API | Equivalent v2 API | Notes |
|:---|:---|:---|
| `ModuleClient` | `IotHubModuleClient` | Specify the service it is a device client for. |

### IoT hub service client

This service client has probably seen more updates than any other client library in this v2.
What was a loose affiliation of separate clients is now a consolidated client with organized subclients by operation type.

#### Exception changes

These span across all service clients.

| v1 API | Equivalent v2 API | Notes |
|:---|:---|:---|
| `ErrorCode` | `IotHubServiceErrorCode` | See⁵ |
| `IotHubException` | `OperationCanceledException` | When a cancelation token is signaled. |
| `IotHubException` | `IotHubServiceException` | When an invalid delivery acknowledgement was specified. |
| `ConfigurationNotFoundException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.ConfigurationNotFound`. |
| `DeviceAlreadyExistsException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.DeviceAlreadyExists`. |
| `DeviceMaximumQueueDepthExceededException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.DeviceMaximumQueueDepthExceeded`. |
| `DeviceNotFoundException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.DeviceNotFound`. |
| `InvalidProtocolVersionException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.InvalidProtocolVersion`. |
| `IotHubSuspendedException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.IotHubSuspended`. |
| `IotHubThrottledException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.ThrottlingException`. |
| `IotHubCommunicationException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.Unknown` when network errors occurred. |
| `IotHubCommunicationException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.Unknown` when an operation timed out. |
| `IotHubNotFoundException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.Unknown`. |
| `JobNotFoundException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.JobNotFound`. |
| `JobQuotaExceededException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.JobQuotaExceeded`. |
| `MessageTooLargeException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.MessageTooLarge`. |
| `ModuleAlreadyExistsException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.ModuleAlreadyExistsOnDevice`. |
| `ModuleNotFoundException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.ModuleNotFound`. |
| `PreconditionFailedException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.PreconditionFailed`. |
| `QuotaExceededException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.IotHubQuotaExceeded`. |
| `ServerBusyException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.ServiceUnavailable`. |
| `ServerErrorException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.ServerError`. |
| `ThrottlingException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.ThrottlingException` or `IotHubServiceErrorCode.ThrottlingBacklogTimeout`. |
| `TooManyDevicesException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.TooManyDevices`. |
| `TooManyModulesOnDeviceException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.TooManyModulesOnDevice`. |
| `UnauthorizedException` | `IotHubServiceException` | With an `ErrorCode` of `IotHubServiceErrorCode.IotHubUnauthorizedAccess`. |
| `DeviceInvalidResultCountException` | Deprecated. | Was not thrown by v1 client¹. |
| `DeviceMessageLockLostException` | Deprecated. | See¹ |
| `IotHubSerializationVersionException` | Deprecated. | See¹ |
| `JobCancelledException` | Deprecated. | See¹ |

#### RegistryManager

#### Notable breaking changes

- Operations that offer concurrency protection using `ETag`s, now take a parameter `onlyIfUnchanged` that relies on the ETag property of the submitted entity.
- `IotHubServiceClient.Query.CreateAsync<T>(...)` is now async.
  - Call `QueryResponse<T>.MoveNextAsync()` in a loop (end when it returns `false`) and access `QueryResponse<T>.Current`.
- `JobProperties` properties that hold Azure Storage SAS URIs are now of type `System.Uri` instead of `string`.
- `JobProperties` has been split into several classes with only the necessary properties for the specified operation.
  - See `ExportJobProperties`, `ImportJobProperties`, and `IotHubJobResponse`.
- Twin.Tags is now of type `IDictionary<string, object>`.

#### Notable additions

- `JobProperties` now has a helper property `IsFinished` which returns true if the job status is in a terminal state.
- `TryGetValue<T>(...)` is available off of the desired and reported properties on `ClientTwinProperties`.
- Added type `ImportJobError` to deserialize the error details of an import job.

#### API mapping

| v1 API | Equivalent v2 API | Notes |
|:---|:---|:---|
| `RegistryManager` | `IotHubServiceClient`, subclients: `Devices`, `Twins`, `Configurations`, etc. | |
| `RegistryManager.AddDeviceAsync(Device, ...)` | `IotHubServiceClient.Devices.CreateAsync(Device, ...)` | |
| `RegistryManager.AddDevices2Async(...)` | `IotHubServiceClient.Devices.CreateAsync(IEnumerable<Device>,...)` | |
| `RegistryManager.RemoveDeviceAsync(...)` | `IotHubServiceClient.Devices.DeleteAsync(...)` | |
| `Device.ConnectionStateUpdatedTime` | `Device.ConnectionStateUpdatedOnUtc` | Conforming to the naming guidelines by the Azure SDK team, where DateTime/Offset types have an "On" suffix (and "Utc" suffix when explicitly in UTC).¹ |
| `Device.StatusUpdatedTime` | `Device.StatusUpdatedOnUtc` | See¹ |
| `Device.LastActivityTime` | `Device.LastActiveOnUtc` | See¹ |
| `Device.Capabilities.IotEdge` | `Device.Capabilities.IsIotEdge` | Boolean properties should start with a verb, usually "Is". |
| `Module.ConnectionStateUpdatedTime` | `Module.ConnectionStateUpdatedOnUtc` | See¹ |
| `Module.LastActivityTime` | `Module.LastActiveOnUtc` | See¹ |
| `RegistryManager.GetTwinAsync(...)` | `IotHubServiceClient.Twins.GetAsync(...)` | |
| `RegistryManager.UpdateTwinAsync(...)` | `IotHubServiceClient.Twins.UpdateAsync(...)` | |
| `Twin` | `TwinProperties` | The device only gets properties, not the full twin. |
| `Twin.StatusUpdatedOn` | `ClientTwin.StatusUpdatedOnUtc` | See¹ |
| `Twin.LastActivityOn` | `ClientTwin.LastActiveOnUtc` | See¹ |
| `TwinCollection` | `ClientTwinProperties` | "Client" is a word we often use to indicate the device- or module-side of the data-plane.² |
| `TwinCollection.GetLastUpdatedOn()` | `ClientTwinProperties.Metadata.LastUpdatedOnUtc` | See¹ |
| `TwinCollectionValue` | `ClientTwinProperties.TryGetValue(...)` | Now just get the property as a type you know expect to be. |
| `Metadata` | `ClientTwinMetadata` | See² |
| `Metadata.LastUpdatedOn` | `ClientTwinMetadata.LastUpdatedOnUtc` | See¹ |
| `AuthenticationType` | `ClientAuthenticationType` | See² |
| `DeviceConnectionState` | `ClientConnectionState` | See² |
| `DeviceStatus` | `ClientStatus` | See² |
| `DeviceCapabilities` | `ClientCapabilities` | See² |
| `RegistryManager.CreateQuery(...)` | `IotHubServiceClient.Query.CreateAsync<T>(...)` | |
| `RegistryManager.AddConfigurationAsync(...)` | `IotHubServiceClient.Configurations.CreateAsync(...)` | |
| `RegistryManager.GetConfigurationsAsync(int maxCount)`| `IotHubServiceClient.Configurations.GetAsync(int maxCount)` | |
| `RegistryManager.RemoveConfigurationAsync(...)` | `IotHubServiceClient.Configurations.DeleteAsync(...)` | |
| `RegistryManager.ImportDevicesAsync(...)` | `IotHubServiceClient.Devices.ImportAsync(...)` | |
| `RegistryManager.ExportDevicesAsync(...)` | `IotHubServiceClient.Devices.ExportAsync(...)` | |
| `JobProperties.CreateForImportJob(...)` | `new JobProperties(Uri, Uri)` | |
| `JobProperties.CreateForExportJob(...)` | `new JobProperties(Uri, bool)` | |
| `RegistryManager.GetJobAsync(...)` | `IotHubServiceClient.Devices.GetJobAsync(...)` | |
| `RegistryManager.CancelJobAsync(...)` | `IotHubServiceClient.Devices.CancelJobAsync(...)` | |
| `JobProperties.Type` | `JobProperties.JobType` | Other parts of the API use "JobType" and "Type" is otherwise too ambiguous with `System.Type`. |
| `ExportImportDevice.Properties.DesiredProperties` | `ExportImportDevice.Properties.Desired` | |
| `ExportImportDevice.Properties.ReportedProperties` | `ExportImportDevice.Properties.Reported` | |

#### ServiceClient

#### Notable breaking changes

- The `Message` class no longer requires disposal!
- `FeedbackReceiver` is now a callback assigned to the `MessageFeedbackProcessor` property.
- `GetFileNotificationReceiver(...)` is now a callback assigned to `FileUploadNotificationProcessor` property. These methods return a callback value.
- `FileUploadNotification.BlobUriPath` was a string and is now of type `System.Uri`.

#### Notable additions

- The library now includes `IIotHubServiceRetryPolicy` implementations: `IotHubServiceExponentialBackoffRetryPolicy`, `IotHubServiceFixedDelayRetryPolicy`, `IotHubServiceIncrementalDelayRetryPolicy` and `IotHubServiceNoRetry`,
 which can be set via `IotHubServiceClientOptions.RetryPolicy`.
 - `DirectMethodClientResponse` now has a method `TryGetValue<T>` to deserialize the payload to a type of your choice.
 - Added `ImportJobError` class to help deserialize errors from device/module/configuration import job.
   - Use the `ImportErrorsBlobName` to load the output errors file, if it exists, in the blob container specified in `ImportJobProperties.InputBlobContainerUri`.
- `IsFinished` convenience property now exists on `CloudToDeviceMethodScheduledJob`, `ScheduledJob`, and `TwinScheduledJob` which is **true** when `Status` is `Completed`, `Failed`, or `Cancelled`.

#### API mapping

| v1 API | Equivalent v2 API | Notes |
|:---|:---|:---|
| `ServiceClient` | `IotHubServiceClient`, subclients `Messages`, `MessageFeedback`, `FileUploadNotifications` | |
| `ServiceClient.SendAsync(...)` | `IotHubServiceClient.Messages.SendAsync(...)` | |
| `Message` | `OutgoingMessage` | Disambiguate from the other kinds of messages used in IoT hub. |
| `Message.ExpiryTimeUtc` | `OutgoingMessage.ExpiresOnUtc` | Conforming to the naming guidelines by the Azure SDK team, where DateTime/Offset types have an "On" suffix (and "Utc" suffix when explicitly in UTC).¹ |
| `Message.CreationTimeUtc` | `OutgoingMessage.CreatedOnUtc` | See¹ |
| `ServiceClient.InvokeDeviceMethodAsync(...)` | `IotHubServiceClient.DirectMethods.InvokeAsync(...)` | |
| `CloudToDeviceMethod` | `DirectMethodServiceRequest` | Disambiguate from types in the device client.² |
| `CloudToDeviceMethodResult` | `DirectMethodClientResponse` | See² |
| `CloudToDeviceMethodResult.GetPayloadAsJson()` | `DirectMethodClientResponse.PayloadAsString` | |
| `ServiceClient.GetFeedbackReceiver(...)` | `IotHubServiceClient.MessageFeedback.MessageFeedbackProcessor` | |
| `ServiceClient.GetFileNotificationReceiver()` | `IotHubServiceClient.FileUploadNotifications.FileUploadNotificationProcessor` | |
| `IotHubException` | `IotHubServiceException` | Specify the exception is for Hub service client only. |

#### JobClient

#### Notable breaking changes
- `JobClient.ScheduleTwinUpdateAsync(...)` previously returned a `JobResponse`, now returns `ScheduledJob`.
- `ScheduleJobs.GetAsync()` return type has changed to `QueryResponse<ScheduledJob>` from `IEnumerable<JobResponse>`.

| v1 API | Equivalent v2 API | Notes |
|:---|:---|:---|
| `JobsClient` | `IotHubServiceClient`, subclients  `ScheduledJobs` | |
| `JobClient.GetJobAsync(...)` | `IotHubServiceClient.ScheduledJobs.GetAsync(...)` | |
| `JobClient.CreateQuery()` | `IotHubServiceClient.ScheduledJobs.CreateQueryAsync()` | |
| `JobsClient.ScheduleTwinUpdateAsync(...)` | `IotHubServiceClient.ScheduledJobs.ScheduledTwinUpdateAsync(...)` | |
| `JobType.ExportDevices` | `JobType.Export` | Matches the actual value expected by the service.¹ |
| `JobType.ImportDevices` | `JobType.Import` | See¹ |

#### DigitalTwinClient

#### Notable breaking changes

- Methods on this client have new, simpler return types. Check each method documentation comments for details.
  - Formerly `HttpOperationResponse` and now specific per method call. To get the body of the response before it would have been `HttpOperationResponse.Body` and now it will be, for example, `DigitalTwinGetReponse<T>.DigitalTwin`.
- The update method takes an `InvokeDigitalTwinCommandOptions` which holds the optional payload, connect timeout, and response timeout.
- The `HttpOperationException will no longer be thrown. Exceptions that might be thrown are documented on each method.
- `UpdateDigitalTwinOptions.IfMatch` type changed from `string` to `ETag`.

#### API mapping

| v1 API | Equivalent v2 API | Notes |
|:---|:---|:---|
| `DigitalTwinClient` | `IotHubServiceClient.DigitalTwins` | |
| `DigitalTwinClient.GetDigitalTwinAsync(...)` | `IotHubServiceClient.DigitalTwins.GetAsync(...)` | |
| `DigitalTwinClient.UpdateDigitalTwinAsync(...)` | `IotHubServiceClient.DigitalTwins.UpdateAsync(...)` | |
| `WritableProperty.LastUpdatedOn` | `WritableProperty.LastUpdatedOnUtc` | Conforming to the naming guidelines by the Azure SDK team, where DateTime/Offset types have an "On" suffix (and "Utc" suffix when explicitly in UTC). |
| `UpdateOperationsUtility` | Removed.  | `Azure.JsonPatchDocument` from Azure.Core package is recommended. |

### DPS device client

#### Notable breaking changes

- The security providers that are used in conjunction with this client have changed. See [this section](#authentication-provider-client) for more details.
- The previous way of providing transport level settings, `ProvisioningTransportHandler`, has been replaced with `ProvisioningClientTransportSettings`.
- TPM support removed. The library used for TPM operations is broken on Linux and support for it is being shutdown. We'll reconsider how to support HSM.
- HTTP has been removed as a transport option to keep the provisioning device SDK consistent with IoT hub device SDK transport options.

#### Notable additions

- Added support for setting a client web socket instance in the client options so that users can have better control over AMQP web socket connections.
- Added support for setting the web socket level keep alive interval for AMQP web socket connections.
- Added support for setting the remote certificate validation callback for AMQP TCP connections.
- The library now includes `IProvisioningClientRetryPolicy` implementations: `ProvisioningClientExponentialBackoffRetryPolicy`, `ProvisioningClientFixedDelayRetryPolicy`, `ProvisioningClientIncrementalDelayRetryPolicy` and `ProvisioningClientNoRetry`,
 which can be set via `ProvisioningClientOptions.RetryPolicy`.
 - ProvisioningRegistrationSubstatus.ReprovisionedToInitalAssignment value added meaning the device has been reprovisioned to a previously assigned IoT hub.
 - `DeviceRegistrationResult` now has a method `TryGetPayload<T>(...)`.
 - `ProvisioningClientTransportSettings` and derived classes now have settable property `CertificateRevocationCheck`.

#### API mapping

| v1 API | Equivalent v2 API | Notes |
|:---|:---|:---|
| `ProvisioningDeviceClient.Create(...)` | `new ProvisioningDeviceClient(...)` | |
| `ProvisioningDeviceClient` initializer parameter `transportHandler` replaced | `ProvisioningClientOptions` parameter added | |
| `ProvisioningRegistrationAdditionalData` | `RegistrationRequestPayload`| |
| `ProvisioningRegistrationAdditionalData.JsonData` | `RegistrationRequestPayload.Payload` | |
| `DeviceRegistrationResult.CreatedDateTimeUtc` | `DeviceRegistrationResult.CreatedOnUtc` | Conforming to the naming guidelines by the Azure SDK team, where DateTime/Offset types have an "On" suffix (and "Utc" suffix when explicitly in UTC).¹ |
| `DeviceRegistrationResult.LastUpdatedDateTimeUtc` | `DeviceRegistrationResult.LastUpdatedOnUtc` | See¹ |
| `ProvisioningTransportException` | `ProvisioningClientException` | |
| `PnpConvention` | `ModelIdPayload` | Added model class to replace a JSON helper. |
| `ProvisioningRegistrationStatusType` | `ProvisioningRegistrationStatus` | Renamed, because not a type.² |
| `ProvisioningRegistrationSubstatusType` | `ProvisioningRegistrationSubstatus` | See² |

### DPS service client

#### Notable breaking changes

- Query methods (like for individual and group enrollments) now take a query string (and optionally a page size parameter), and the `Query` result no longer requires disposing.
- ETag fields on the classes `IndividualEnrollment`, `EnrollmentGroup`, and `DeviceRegistrationState` are now taken as the `Azure.ETag` type instead of strings.
- Twin.Tags is now of type `IDictionary<string, object>`.
- `CustomAllocationDefinition.WebhookUri` is now of type `System.Uri` instead of `System.String`.

#### Notable additions

- The library now includes `IProvisioningServiceRetryPolicy` implementations: `ProvisioningServiceExponentialBackoffRetryPolicy`, `ProvisioningServiceFixedDelayRetryPolicy`, `ProvisioningServiceIncrementalDelayRetryPolicy` and `ProvisioningServiceNoRetry`,
 which can be set via `ProvisioningServiceOptions.RetryPolicy`.

#### API mapping

| v1 API | Equivalent v2 API | Notes |
|:---|:---|:---|
| `ProvisioningServiceClient.CreateFromConnectionString(...)` | `new ProvisioningServiceClient()` | Constructors enable mocking for unit tests. |
| `QuerySpecification` | Type removed from public API. | Methods take the parameters directly. |
| `ProvisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(IndividualEnrollment, ...)` | `ProvisioningServiceClient.IndividualEnrollments.CreateOrUpdateAsync(IndividualEnrollment, ...)` | |
| `ProvisioningServiceClient.GetIndividualEnrollmentAsync(IndividualEnrollment, ...)` | `ProvisioningServiceClient.IndividualEnrollments.GetAsync(IndividualEnrollment, ...)` | |
| `ProvisioningServiceClient.DeleteIndividualEnrollmentAsync(...)` | `ProvisioningServiceClient.IndividualEnrollments.DeleteAsync(...)` | |
| `ProvisioningServiceClient.GetIndividualEnrollmentAttestationAsync(...)` | `ProvisioningServiceClient.IndividualEnrollments.GetAttestationAsync(...)` | |
| `ProvisioningServiceClient.CreateIndividualEnrollmentQuery(...)` | `ProvisioningServiceClient.IndividualEnrollments.CreateQuery(...)` | |
| `ProvisioningServiceClient.RunBulkEnrollmentOperationAsync(...)` | `ProvisioningServiceClient.IndividualEnrollments.RunBulkEnrollmentOperationAsync(...)` | |
| `ProvisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(EnrollmentGroup, ...)` | `ProvisioningServiceClient.EnrollmentGroups.CreateOrUpdateAsync(EnrollmentGroup, ...)` | |
| `ProvisioningServiceClient.GetEnrollmentGroupAsync(EnrollmentGroup, ...)` | `ProvisioningServiceClient.EnrollmentGroups.GetAsync(EnrollmentGroup, ...)` | |
| `ProvisioningServiceClient.DeleteEnrollmentGroupAsync(...)` | `ProvisioningServiceClient.EnrollmentGroups.DeleteAsync(...)` | |
| `ProvisioningServiceClient.GetEnrollmentGroupAttestationAsync(...)` | `ProvisioningServiceClient.EnrollmentGroups.GetAttestationAsync(...)` | |
| `ProvisioningServiceClient.CreateEnrollmentGroupQuery(...)` | `ProvisioningServiceClient.EnrollmentGroups.CreateQuery(...)` | |
| `ProvisioningServiceClient.GetDeviceRegistrationStateAsync(...)` | `ProvisioningServiceClient.DeviceRegistrationStates.GetAsync(...)` | |
| `ProvisioningServiceClient.DeleteDeviceRegistrationStateAsync(...)` | `ProvisioningServiceClient.DeviceRegistrationStates.DeleteAsync(...)` | |
| `ProvisioningServiceClient.CreateEnrollmentGroupRegistrationStateQuery(...)` | `ProvisioningServiceClient.DeviceRegistrationStates.CreateEnrollmentGroupQuery(...)` | |
| `DeviceRegistrationState.CreatedDateTimeUtc` | `DeviceRegistrationState.CreatedOnUtc` | Conforming to the naming guidelines by the Azure SDK team, where DateTime/Offset types have an "On" suffix (and "Utc" suffix when explicitly in UTC).¹ |
| `DeviceRegistrationState.LastUpdatedDateTimeUtc` | `DeviceRegistrationState.LastUpdatedOnUtc` | See¹ |
| `EnrollmentGroup.EnrollmentGroupId` | `EnrollmentGroup.Id` | Simplify property name. |
| `EnrollmentGroup.CreatedDateTimeUtc` | `EnrollmentGroup.CreatedOnUtc` | See¹ |
| `EnrollmentGroup.LastUpdatedDateTimeUtc` | `EnrollmentGroup.LastUpdatedOnUtc` | See¹ |
| `IndividualEnrollment.CreatedDateTimeUtc` | `IndividualEnrollment.CreatedOnUtc` | See¹ |
| `IndividualEnrollment.LastUpdatedDateTimeUtc` | `IndividualEnrollment.LastUpdatedOnUtc` | See¹ |
| `Twin` | `InitialTwin` | Disambiguate between similar types in the IoT hub service and device clients, and make it clearer that this is only represents the initial state of the device's twin.² |
| `Twin.StatusUpdatedOn` | `InitalTwin.StatusUpdatedOnUtc` | See¹ |
| `Twin.LastActivityOn` | `InitialTwin.LastActiveOnUtc` | See¹ |
| `TwinCollection` | `InitialTwinPropertyCollection` | See² |
| `TwinCollection.GetLastUpdatedOn()` | `InitialTwinProperties.GetLastUpdatedOnUtc()` | See¹ |
| `TwinCollectionValue` | `InitialTwinPropertyValue` | See² |
| `TwinCollectionValue.GetLastUpdatedOn()` | `InitialTwinPropertyValue.GetLastUpdatedOnUtc()` | See¹ |
| `TwinCollectionArray` | `InitialTwinPropertyArray` | See² |
| `TwinCollectionArray.GetLastUpdatedOn()` | `InitialTwinPropertyArray.GetLastUpdatedOnUtc()` | See¹ |
| `Metadata` | `InitialTwinMetadata` | See² |
| `Metadata.LastUpdatedOn` | `InitialTwinMetadata.LastUpdatedOnUtc` | See¹ |
| `DeviceCapabilities` | `InitialTwinCapabilities` | See² |
| `X509Attestation.CreateFromCAReferences(...)` | `X509Attestation.CreateFromCaReferences(...)` | Pascal casing.³ |
| `X509Attestation.CAReferences` | `X509Attestation.CaReferences` | See³ |
| `X509CAReferences` | `X509CaReferences` | See³ |
| `X509CertificateInfo.SHA1Thumbprint` | `X509CertificateInfo.Sha1Thumbprint` | See³ |
| `X509CertificateInfo.SHA256Thumbprint` | `X509CertificateInfo.Sha256Thumbprint` | See³ |
| `ProvisioningServiceClientException` | `ProvisioningServiceException` | |
| `ProvisioningClientCapabilities.IotEdge` | `InitialClientCapabilities.IsIotEdge` | Boolean properties should start with a verb, usually "Is". |

### Security provider client

#### Notable breaking changes

- Microsoft.Azure.Devices.Shared.SecurityProvider* types moved from Microsoft.Azure.Devices.Shared.dll into Microsoft.Azure.Devices.Provisioning.Client.dll and renamed.
- Derived `AuthenticationProvider` types no longer require disposal because of the base class.
- TPM support removed. The library used for TPM operations is broken on Linux and support for it is being shutdown. We'll reconsider how to support HSM.

#### API mapping

| v1 API | Equivalent v2 API | Notes |
|:---|:---|:---|
| `SecurityProvider` | `AuthenticationProvider` | These classes are about authentication, not security. ¹ |
| `SecurityProvider.GetRegistrationID()` | `AuthenticationProvider.GetRegistrationId()` | |
| `SecurityProviderSymmetricKey` | `AuthenticationProviderSymmetricKey` | See¹ |
| `SecurityProviderX509Certificate` | `AuthenticationProviderX509` | See¹ |
| `SecurityProviderX509` abstract base class | removed | Unnecessary class. |
| `SecurityProviderTpm` | removed | Deprecated. |

## Frequently asked questions

Question:
> What do I gain by upgrading to the v2 release?

Answer:
> A smaller set of dependencies which makes for a lighter SDK overall, a more concise and clearer API surface, fewer types requiring disposal, and mocking in unit tests.

Question:
> Will the v1 releases continue to be supported?

Answer:
> The long-term support (LTS) releases of the v1 SDK will continue to have support during their lifespans.
> Newer features in the services will not be brought into to the v 1 SDKs. Users are encouraged to upgrade to the 2 SDK for all the best feature support, stability, and bug fixes.

Question:
> Can I still upload files to Azure Storage using this SDK now that `DeviceClient.UploadToBlobAsync(...)` has been removed?

Answer:
> Yes, you will still be able to upload files to Azure Storage after upgrading. 
>
> This SDK allows you to get the necessary credentials to upload your files to Azure Storage, but you will need to bring in the Azure Storage SDK as a dependency to do the actual file upload step. 
> 
> For an example of how to do file upload after upgrading, see [this sample](./iothub/device/samples/getting%20started/FileUploadSample/).

Question:
> I was using a deprecated API that was removed in the v2 upgrade, what should I do?

Answer:
> The deprecated API in v1 documents which API you should use instead of the deprecated API.
> This guide also contains a mapping from v1 API to equivalent v2 API that should tell you which API to use.

Question:
> After upgrading, some catch handlers no longer work because the API I was using no longer declares that it throws that exception. What exception type should be caught?

Answer:
> The SDK has an updated exception strategy. Please refer to the documentation comments on each API where the relevant exceptions are documented.

Question:
> Does this major version bump bring any changes to what platforms this SDK supports?

Answer:
> These SDKs now only target .NET Standard 2.0, which is usable by all modern .NET targets, including .NET 6.0.
> The SDKs are tested with [this matrix](./supported_platforms.md).

Question:
> Is the v2 library backwards compatible?

Answer:
> No. Please refer to [Semver rules](https://semver.org/) and see above in the [Migration guide](#migration-guide).
