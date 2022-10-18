# SDK version 2 migration guide

This document outlines the changes made from this library's 1 releases to its 2 releases. Since this is
a major version upgrade, there are a number of breaking changes that will affect the ability to compile. Provided here
are outlines of the notable breaking changes as well as a mapping from version 1 APIs to version 2 APIs to aid migrating.

## Table of contents

 - [Why the version 1 SDK is being replaced](#why-the-version-1-sdk-is-being-replaced)
 - [What will happen to the version 1 SDK](#What-will-happen-to-the-version-1-sdk)
 - [Migration guide](#migration-guide)
   - [IoT hub device client](#iot-hub-device-client)
   - [IoT hub service client](#iot-hub-service-client)
   - [Device Provisioning Service (DPS) device client](#dps-device-client)
   - [DPS service client](#dps-service-client)
   - [Security provider client](#security-provider-client)
 - [Frequently asked questions](#frequently-asked-questions)

## Why the version 1 SDK is being replaced

There are a number of reasons why the Azure IoT SDK team chose to do a major version revision. Here are a few of the more important reasons:
  - Removing or upgrading several NuGet dependencies.
    - Upgraded
      - Microsoft.Azure.Devices.Client
      - Microsoft.Azure.Devices
      - Microsoft.Azure.Devices.Authentication
      - Microsoft.Azure.Devices.Provisioning.Client
      - Microsoft.Azure.Devices.Provisioning.Service
    - Removed
      - Microsoft.Azure.Devices.Shared
      - Microsoft.Azure.Devices.Provisioning.Transport.Amqp
      - Microsoft.Azure.Devices.Provisioning.Transport.Http
      - Microsoft.Azure.Devices.Provisioning.Transport.Mqtt
      - Microsoft.Azure.Devices.Provisioning.Security.Tpm

  - Consolidating IoT hub service clients and renaming to reflect the items or operations they support.
    - Many existing client classes (RegistryManager, ServiceClient, etc.) were confusingly named and contained methods that weren't always consistent with the client's assumed responsibilities.
  - Many existing clients had a mix of standard constructors (`new DeviceClient(...)`) and static builder methods (`DeviceClient.CreateFromConnectionString(...)`) that caused some confusion among users. The factory methods have been removed and the addition of constructors in clients enables unit testing.
  - Exception handling. (Exception handling in the v1 SDK required multiple exception types to be caught. This logic has been consolidated to always throw only `IotHubClientException` for exceptions arising from communication attempts with IoT Hub. Parameter validation will throw `ArgumentException`/`ArgumentNullException`/`FormatException`/`InvalidOperationException`.
  - RecommendedAction in connection status handling.
  
## What will happen to the version 1 SDK

We will have released one final LTS version of the version 1 SDK that we will support like any other LTS release (security bug fixes, some non-security bug fixes as needed),
but users are still encouraged to migrate to version 2 when they have the chance. For more details on LTS releases, see [this document](./readme.md#long-term-support-lts).

## Migration guide

### IoT hub device client

#### DeviceClient

| Version 1 API | Equivalent version 2 API |
|:---|:---|
| `DeviceClient` | `IotHubDeviceClient` |
| `SetConnectionStatusChangesHandler` | `SetConnectionStatusChangeHandler` |
| `MessageResponse` | `MessageAcknowledgement` |
| `Message` | `IncomingMessage`, `OutgoingMessage` |
| `SetRetryPolicy(...)` | `IoTHubClientOptions.RetryPolicy` |
| `ExponentialBackOff` | `ExponentialBackOffRetryPolicy` |
| `Message.CreationTimeUtc` | `OutgoingMessage.CreatedOnUtc`, `IncomingMessage.CreatedOnUtc` |
| `Message.EnqueuedTimeUtc` | `OutgoingMessage.EnqueuedtimeUtc`, `IncomingMessage.EnqueuedTimeUtc` |
| `Message.ExpiryTimeUtc` | `OutgoingMessage.ExpiresOnUtc`, `IncomingMessage.ExpiresOnUtc` |

#### Other notable breaking changes

- The transport default has changed from AMQP (TCP with web socket fail over) to MQTT TCP.
  - To override the transport default, create an instance of `IotHubClientOptions` and pass an instance of the transport settings you wish to use (i.e., `IotHubClientMqttSettings`, `IotHubClientAmqpSettings`).
  - TCP will be the default. For web socket, pass `IotHubClientTransportProtocol.WebSocket` to the transport settings constructors.
- HTTP has been removed as a transport option.
  - It had very limited support across the device options and some APIs behaved differently.
- Some options that were previously set in the `DeviceClient` constructor are now in the optional `IotHubClientOptions` parameter.
- The connection status callback parameters have changed. Instead of two parameters, a class with several properties is provided.
  - Two properties are the same as before, but with some renames (underscores removed) and obsolete members removed.
  - A new property has been added with a recommended action, which a device developer may observe or ignore.
- The file upload method has been split into the three individual steps that this method used to take. See [this file upload sample](./iothub/device/samples/getting%20started/FileUploadSample/) for an example of how to do file upload using these discrete steps.
- Cloud-to-device messages can be received by calling `SetMessageHandlerAsync` and providing a callback. Users no longer need to poll for messages with `ReceiveAsync`.
- Several callback handler set methods and definitions have changed, losing the `userContext` parameter.
- The exponential back-off retry policy has updated parameters and logic.
- Remote certificate validation is no natively longer supported for AMQP web socket connections.
  - The supported workaround is to provide a client web socket instance in the client options.

#### Notable additions

- The device and module clients now have a property (e.g., `IotHubDeviceClient.ConnectionStatusInfo`) with the latest connection status information on it, eliminating the need for a connection status callback method to cache the latest values.
- Added support for setting a client web socket instance in the client options so that users can have better control over AMQP web socket connections.
- The library now includes IncrementalDelayRetryStrategy and FixedDelayRetryStrategy.
- The client can now be re-opened after it has been closed. It cannot be re-opened after it has been disposed, though. Also, subscriptions do not carry over when the client is re-opened.

#### ModuleClient

| Version 1 API | Equivalent version 2 API |
|:---|:---|
| `ModuleClient` | `IotHubModuleClient` |
| `MessageResponse` | `MessageAcknowledgement` |
| `Message` | `IncomingMessage`, `OutgoingMessage` |

#### Other notable breaking changes

- See changes to `DeviceClient`.
- Reduced access levels to classes and methods that were never intended to be public where possible.

#### Notable additions

- The client can now be re-opened after it has been closed. It cannot be re-opened after it has been disposed, though. Also, subscriptions do not carry over when the client is re-opened.


### IoT hub service client

#### RegistryManager

| Version 1 API | Equivalent version 2 API |
|:---|:---|
| `RegistryManager` | `IotHubServiceClient`, `subclients`, `Devices`, `Twins`, `Configurations`, etc. |
| `RegistryManager.AddDeviceAsync(Device, ...)` | `IotHubServiceClient.Devices.CreateAsync(Device, ...)` |
| `RegistryManager.AddDevices2Async(...)` | `IotHubServiceClient.Devices.CreateAsync(IEnumerable<Device>,...)` |
| `RegistryManager.RemoveDeviceAsync(...)` | `IotHubServiceClient.Devices.DeleteAsync(...)` |
| `Device.ConnectionStateUpdatedTime` | `Device.ConnectionStateUpdatedOnUtc` |
| `Device.StatusUpdatedTime` | `Device.StatusUpdatedOnUtc` |
| `Device.LastActivityTime` | `Device.LastActiveOnUtc` |
| `Device.Capabilities.IotEdge` | `Device.Capabilities.IsIotEdge` |
| `Module.ConnectionStateUpdatedTime` | `Module.ConnectionStateUpdatedOnUtc` |
| `Module.LastActivityTime` | `Module.LastActiveOnUtc` |
| `RegistryManager.GetTwinAsync(...)` | `IotHubServiceClient.Twins.GetAsync(...)` |
| `RegistryManager.UpdateTwinAsync(...)` | `IotHubServiceClient.Twins.UpdateAsync(...)` |
| `Twin.StatusUpdatedOn` | `Twin.StatusUpdatedOnUtc` |
| `Twin.LastActivityOn` | `Twin.LastActiveOnUtc` |
| `TwinCollection.GetLastUpdatedOn()` | `TwinCollection.GetLastUpdatedOnUtc()` |
| `TwinCollectionArray.GetLastUpdatedOn()` | `TwinCollectionArray.GetLastUpdatedOnUtc()` |
| `TwinCollectionValue.GetLastUpdatedOn()` | `TwinCollectionValue.GetLastUpdatedOnUtc()` |
| `Metadata.LastUpdatedOn` | `TwinMetadata.LastUpdatedOnUtc` |
| `RegistryManager.CreateQuery(...)` | `IotHubServiceClient.Query.CreateAsync<T>(...)` |
| `RegistryManager.AddConfigurationAsync(...)` | `IotHubServiceClient.Configurations.CreateAsync(...)` |
| `RegistryManager.GetConfigurationsAsync(int maxCount)`| `IotHubServiceClient.Configurations.GetAsync(int maxCount)` |
| `RegistryManager.RemoveConfigurationAsync(...)` | `IotHubServiceClient.Configurations.DeleteAsync(...)` |
| `RegistryManager.ImportDevicesAsync(...)` | `IotHubServiceClient.Devices.ImportAsync(...)` |
| `RegistryManager.ExportDevicesAsync(...)` | `IotHubServiceClient.Devices.ExportAsync(...)` |
| `JobProperties.CreateForImportJob(...)` | `new JobProperties(Uri, Uri)` |
| `JobProperties.CreateForExportJob(...)` | `new JobProperties(Uri, bool)` |
| `RegistryManager.GetJobAsync(...)` | `IotHubServiceClient.Devices.GetJobAsync(...)` |
| `RegistryManager.CancelJobAsync(...)` | `IotHubServiceClient.Devices.CancelJobAsync(...)` |

#### Other notable breaking changes

- `CloudToDeviceMethod` took a constructor parameter for the method name, which is now used with `DirectMethodRequest` as a property initializer.
- Operations that offer concurrency protection using `ETag`s, now take a parameter `onlyIfUnchanged` that relies on the ETag property of the submitted entity.
- `IotHubServiceClient.Query.CreateAsync<T>(...)` is now async.
  - Call `QueryResponse<T>.MoveNextAsync()` in a loop (end when it returns `false`) and access `QueryResponse<T>.Current`.
- `JobProperties` properties that hold Azure Storage SAS URIs are now of type `System.Uri` instead of `string`.

#### Notable additions

- `JobProperties` now has a helper property `IsFinished` which returns true if the job status is in a terminal state.

#### ServiceClient

| Version 1 API | Equivalent version 2 API |
|:---|:---|
| `ServiceClient` | `IotHubServiceClient`, subclients `Messages`, `MessageFeedback`, `FileUploadNotifications` |
| `ServiceClient.SendAsync(...)` | `IotHubServiceClient.Messages.SendAsync(...)` |
| `Message.ExpiryTimeUtc` | `Message.ExpiresOnUtc` |
| `Message.CreationTimeUtc` | `Message.CreatedOnUtc` |
| `ServiceClient.InvokeDeviceMethodAsync(...)` | `IotHubServiceClient.DirectMethods.InvokeAsync(...)` |
| `CloudToDeviceMethod` | `DirectMethodRequest` |
| `CloudToDeviceMethodResult` | `DirectMethodResponse` |
| `ServiceClient.GetFeedbackReceiver(...)` | `IotHubServiceClient.MessageFeedback.MessageFeedbackProcessor` |
| `ServiceClient.GetFileNotificationReceiver()` | `IotHubServiceClient.FileUploadNotifications.FileUploadNotificationProcessor` |


#### Other notable breaking changes

- `FeedbackReceiver` is now callback assigned to `MessageFeedbackProcessor` property.
- `GetFileNotificationReceiver()` is now callback assigned to `FileUploadNotificationProcessor` property. These methods return a callback value.
- The `Message` class no longer requires disposal.

#### DeviceMethod

| Version 1 API | Equivalent version 2 API |
|:---|:---|
|    |    |

#### JobClient

| Version 1 API | Equivalent version 2 API |
|:---|:---|
| `JobsClient` | `IotHubServiceClient`, subclients  `ScheduledJobs` |
| `JobClient.GetJobAsync(...)` | `IotHubServiceClient.ScheduledJobs.GetAsync(...)` |
| `JobClient.CreateQuery()` | `IotHubServiceClient.ScheduledJobs.CreateQueryAsync()` |
| `JobsClient.ScheduleTwinUpdateAsync(...)` | `IotHubServiceClient.ScheduledJobs.ScheduledTwinUpdateAsync(...)` |

#### Other notable breaking changes
- `JobClient.ScheduleTwinUpdateAsync(...)` previously returned a `JobResponse`, now returns `ScheduledJob`.
- `ScheduleJobs.GetAsync()` return type has changed to `QueryResponse<ScheduledJob>` from `IEnumerable<JobResponse>`.

#### DigitalTwinClient

| Version 1 API | Equivalent version 2 API |
|:---|:---|
| `DigitalTwinClient` | `IotHubServiceClient.DigitalTwins` |
| `DigitalTwinClient.GetDigitalTwinAsync(...)` | `IotHubServiceClient.DigitalTwins.GetAsync(...)` |
| `DigitalTwinClient.UpdateDigitalTwinAsync(...)` | `IotHubServiceClient.DigitalTwins.UpdateAsync(...)` |
| `WritableProperty.LastUpdatedOn` | `WritableProperty.LastUpdatedOnUtc` |
| `UpdateOperationsUtility` | Removed. `Azure.JsonPatchDocument` from Azure.Core package is recommended. |

#### Other notable breaking changes

- Methods on this client have new, simpler return types. Check each method documentation comments for details.
  - Formerly `HttpOperationResponse` and now specific per method call. To get the body of the response before it would have been `HttpOperationResponse.Body` and now it will be, for example, `DigitalTwinGetReponse<T>.DigitalTwin`.
- The update method takes an `InvokeDigitalTwinCommandOptions` which holds the optional payload, connect timeout, and response timeout.
- The `HttpOperationException will no longer be thrown. Exceptions that might be thrown are documented on each method.

### DPS device client

| Version 1 API | Equivalent version 2 API |
|:---|:---|
| `ProvisioningDeviceClient.Create(...)` | `new ProvisioningDeviceClient(...)` |
| `ProvisioningDeviceClient` initializer parameter `transportHandler` replaced | `ProvisioningClientOptions` parameter added |
| `ProvisioningRegistrationAdditionalData` | `RegistrationRequestPayload`|
| `DeviceRegistrationResult.CreatedDateTimeUtc` | `DeviceRegistrationResult.CreatedOnUtc` |
| `DeviceRegistrationResult.LastUpdatedDateTimeUtc` | `DeviceRegistrationResult.LastUpdatedOnUtc` |

#### Other notable breaking changes

- The security providers that are used in conjunction with this client have changed. See [this section](#authentication-provider-client) for more details.
- The previous way of providing transport level settings (`ProvisioningTransportHandler`) has been replaced with `ProvisioningClientTransportSettings`.
- TPM support removed. The library used for TPM operations is broken on Linux and support for it is being shutdown. We'll reconsider how to support HSM.
- HTTP has been removed as a transport option to keep the provisioning device SDK consistent with IoT hub device SDK.

#### Notable additions
- Added support for setting a client web socket instance in the client options so that users can have better control over AMQP web socket connections.
- Added support for setting the web socket level keep alive interval for AMQP web socket connections.
- Added support for setting the remote certificate validation callback for AMQP TCP connections.

### DPS service client

| Version 1 API | Equivalent version 2 API |
|:---|:---|
| `ProvisioningServiceClient.CreateFromConnectionString(...)` | `new ProvisioningServiceClient()` |
| `QuerySpecification` | Type removed from public API. Methods take the parameters directly. |
| `ProvisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(IndividualEnrollment, ...)` | `ProvisioningServiceClient.IndividualEnrollments.CreateOrUpdateAsync(IndividualEnrollment, ...)` |
| `ProvisioningServiceClient.GetIndividualEnrollmentAsync(IndividualEnrollment, ...)` | `ProvisioningServiceClient.IndividualEnrollments.GetAsync(IndividualEnrollment, ...)` |
| `ProvisioningServiceClient.DeleteIndividualEnrollmentAsync(...)` | `ProvisioningServiceClient.IndividualEnrollments.DeleteAsync(...)` |
| `ProvisioningServiceClient.GetIndividualEnrollmentAttestationAsync(...)` | `ProvisioningServiceClient.IndividualEnrollments.GetAttestationAsync(...)` |
| `ProvisioningServiceClient.CreateIndividualEnrollmentQuery(...)` | `ProvisioningServiceClient.IndividualEnrollments.CreateQuery(...)` |
| `ProvisioningServiceClient.RunBulkEnrollmentOperationAsync(...)` | `ProvisioningServiceClient.IndividualEnrollments.RunBulkEnrollmentOperationAsync(...)` |
| `ProvisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(EnrollmentGroup, ...)` | `ProvisioningServiceClient.EnrollmentGroups.CreateOrUpdateAsync(EnrollmentGroup, ...)` |
| `ProvisioningServiceClient.GetEnrollmentGroupAsync(EnrollmentGroup, ...)` | `ProvisioningServiceClient.EnrollmentGroups.GetAsync(EnrollmentGroup, ...)` |
| `ProvisioningServiceClient.DeleteEnrollmentGroupAsync(...)` | `ProvisioningServiceClient.EnrollmentGroups.DeleteAsync(...)` |
| `ProvisioningServiceClient.GetEnrollmentGroupAttestationAsync(...)` | `ProvisioningServiceClient.EnrollmentGroups.GetAttestationAsync(...)` |
| `ProvisioningServiceClient.CreateEnrollmentGroupQuery(...)` | `ProvisioningServiceClient.EnrollmentGroups.CreateQuery(...)` |
| `ProvisioningServiceClient.GetDeviceRegistrationStateAsync(...)` | `ProvisioningServiceClient.DeviceRegistrationStates.GetAsync(...)` |
| `ProvisioningServiceClient.DeleteDeviceRegistrationStateAsync(...)` | `ProvisioningServiceClient.DeviceRegistrationStates.DeleteAsync(...)` |
| `ProvisioningServiceClient.CreateEnrollmentGroupRegistrationStateQuery(...)` | `ProvisioningServiceClient.DeviceRegistrationStates.CreateEnrollmentGroupQuery(...)` |
| `DeviceRegistrationState.CreatedDateTimeUtc` | `DeviceRegistrationState.CreatedOnUtc` |
| `DeviceRegistrationState.LastUpdatedDateTimeUtc` | `DeviceRegistrationState.LastUpdatedOnUtc` |
| `EnrollmentGroup.CreatedDateTimeUtc` | `EnrollmentGroup.CreatedOnUtc` |
| `EnrollmentGroup.LastUpdatedDateTimeUtc` | `EnrollmentGroup.LastUpdatedOnUtc` |
| `IndividualEnrollment.CreatedDateTimeUtc` | `IndividualEnrollment.CreatedOnUtc` |
| `IndividualEnrollment.LastUpdatedDateTimeUtc` | `IndividualEnrollment.LastUpdatedOnUtc` |
| `Twin.StatusUpdatedOn` | `Twin.StatusUpdatedOnUtc` |
| `Twin.LastActivityOn` | `Twin.LastActiveOnUtc` |
| `TwinCollection.GetLastUpdatedOn()` | `TwinCollection.GetLastUpdatedOnUtc()` |
| `TwinCollectionArray.GetLastUpdatedOn()` | `TwinCollectionArray.GetLastUpdatedOnUtc()` |
| `TwinCollectionValue.GetLastUpdatedOn()` | `TwinCollectionValue.GetLastUpdatedOnUtc()` |
| `Metadata.LastUpdatedOn` | `TwinMetadata.LastUpdatedOnUtc` |

#### Other notable breaking changes

- Query methods (like for individual and group enrollments) now take a query string (and optionally a page size parameter), and the `Query` result no longer requires disposing.
- ETag fields on the classes `IndividualEnrollment`, `EnrollmentGroup`, and `DeviceRegistrationState` are now taken as the `Azure.ETag` type instead of strings.

### Security provider client

Breaking changes:

  - Microsoft.Azure.Devices.Shared.SecurityProvider* types moved from Microsoft.Azure.Devices.Shared.dll into Microsoft.Azure.Devices.Authentication.dll and renamed.

| Version 1 API | Equivalent version 2 API |
|:---|:---|
| `SecurityProvider` | `AuthenticationProvider` |
| `SecurityProvider.GetRegistrationID()` | `AuthenticationProvider.GetRegistrationId()` |
| `SecurityProviderSymmetricKey` | `AuthenticationProviderSymmetricKey` |
| `SecurityProviderX509Certificate` | `AuthenticationProviderX509` |
| `SecurityProviderX509` abstract base class | removed |
| `SecurityProviderTpm` | removed |

#### Other notable breaking changes

- Derived `AuthenticationProvider` types no longer require disposal because of the base class; only select derived types will (e.g., `AuthenticationProviderTpmHsm`.)
- TPM support removed. The library used for TPM operations is broken on Linux and support for it is being shutdown. We'll reconsider how to support HSM.

## Frequently asked questions

Question:
> What do I gain by upgrading to the 2 release?

Answer:
> A smaller set of dependencies which makes for a lighter SDK overall, a more concise and clearer API surface, and unit testability.

Question:
> Will the 1 releases continue to be supported?

Answer:
> The long-term support (LTS) releases of the 1 SDK will continue to have support during their lifespans.
> Newer features in the services will not be brought into to the v 1 SDKs. Users are encouraged to upgrade to the 2 SDK for all the best feature support, stability, and bug fixes.

Question:
> Can I still upload files to Azure Storage using this SDK now that deviceClient.UploadToBlobAsync() has been removed?

Answer:
> Yes, you will still be able to upload files to Azure Storage after upgrading. 
>
> This SDK allows you to get the necessary credentials to upload your files to Azure Storage, but you will need to bring in the Azure Storage SDK as a dependency to do the actual file upload step. 
> 
> For an example of how to do file upload after upgrading, see [this sample](./iothub/device/samples/getting%20started/FileUploadSample/).

Question:
> I was using a deprecated API that was removed in the 2 upgrade, what should I do?

Answer:
> The deprecated API in the 1 version documents which API you should use instead of the deprecated API. This guide
also contains a mapping from Version 1 API to equivalent version 2 API that should tell you which version 2 API to use.

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
> Is the version 2 library backwards compatible?

Answer:
> No. Please refer to [Semver rules](https://semver.org/) and see above in the [Migration guide](#migration-guide).
