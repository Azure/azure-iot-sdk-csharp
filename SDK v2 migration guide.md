# SDK version 2.x migration guide

This document outlines the changes made from this library's 1.x releases to its 2.x releases. Since this is
a major version upgrade, there are a number of breaking changes that will affect the ability to compile. Provided here
are outlines of the notable breaking changes as well as a mapping from version 1.x APIs to version 2.x APIs to aid migrating.

## Table of contents

 - [Why the version 1.x SDK is being replaced](#Why-the-version-1.x-sdk-is-being-replaced)
 - [What will happen to the version 1.x SDK](#What-will-happen-to-the-Version 1.x-sdk)
 - [Migration guide](#migration-guide)
   - [IoT hub device client](#iot-hub-device-client)
   - [IoT hub service client](#iot-hub-service-client)
   - [Device Provisioning Service (DPS) device client](#dps-device-client)
   - [DPS service slient](#dps-service-client)
   - [Security provider clients](#security-provider-clients)
 - [Frequently asked questions](#frequently-asked-questions)

## Why the version 1.x SDK is being replaced

There are a number of reasons why the Azure IoT SDK team chose to do a major version revision. Here are a few of the more important reasons:
  - Removing or upgrading several NuGet dependencies (TODO: list).
  - Consolidate IoT hub service clients and rename to reflect the items or operations they support. Many existing client classes (RegistryManager, ServiceClient, etc.) were confusingly named and contained methods that weren't always consistent with the client's assumed responsibilities.
  - Many existing clients had a mix of standard constructors (`new DeviceClient(...)`) and static builder methods (`DeviceClient.CreateFromConnectionString(...)`) that caused some confusion among users. The factory methods have been removed and the addition of constructors in clients enables unit testing.

## What will happen to the version 1.x SDK

We have released [one final LTS version](TODO:) of the Version 1.x SDK that
we will support like any other LTS release (security bug fixes, some non-security bug fixes as needed), but users are still encouraged
to migrate to version 2.x when they have the chance. For more details on LTS releases, see [this document](./readme.md#long-term-support-lts).

## Migration guide

### IoT hub device client

#### DeviceClient

| Version 1.x API | Equivalent version 2.x API |
|:---|:---|
| `DeviceClient` | `IotHubDeviceClient` |
| `SetConnectionStatusChangesHandler` | `SetConnectionStatusChangeHandler` |
| `MessageResponse` | `MessageAcknowledgement` |

#### Other notable breaking changes

- The transport default has changed from AMQP (TCP with web socket fail over) to MQTT TCP.
  - To override the transport default, create an instance of `IotHubClientOptions` and pass an instance of the transport settings you wish to use (i.e., `IotHubClientMqttSettings`, `IotHubClientAmqpSettings`).
  - TCP will be the default. For web socket, pass `IotHubClientTransportProtocol.WebSocket` to the transport settings constructors.
- HTTP has been removed as a transport option.
  - It had very limited support across the device options and some APIs behaved differently.
- Some options that were previously set in the `DeviceClient` constructor are now in the optional `IotHubClientOptions` parameter.
- This method has been split into the three individual steps that this method used to take. See [this file upload sample](./iothub/device/samples/getting%20started/FileUploadSample/) for an example of how to do file upload using these discrete steps.
- Cloud-to-device messages can be received by calling `SetMessageHandlerAsync` and providing a callback. Users no longer need to poll for messages with `ReceiveAsync`.

#### ModuleClient

| Version 1.x API | Equivalent version 2.x API |
|:---|:---|
| `ModuleClient` | `IotHubModuleClient` |
| `MessageResponse` | `MessageAcknowledgement` |

#### Other notable breaking changes

- See changes to `DeviceClient`.
- Reduced access levels to classes and methods that were never intended to be public where possible.

### IoT hub service client

#### RegistryManager

| Version 1.x API | Equivalent version 2.x API |
|:---|:---|
| `RegistryManager` | `IotHubServiceClient`, subclients `Devices`, `Twins`, `Configurations`, etc. |
| `RegistryManager.AddDeviceAsync(Device, ...)` | `IotHubServiceClient.Devices.CreateAsync(Device, ...)` |
| `RegistryManager.AddDevices2Async(...)` | `IotHubServiceClient.Devices.CreateAsync(IEnumerable<Device>,...)` |
| `RegistryManager.RemoveDeviceAsync(...)` | `IotHubServiceClient.Devices.DeleteAsync(...)` |
| `Device.Capabilities.IotEdge` | `Device.Capabilities.IsIotEdge` |
| `RegistryManager.GetTwinAsync(...)` | `IotHubServiceClient.Twins.GetAsync(...)` |
| `RegistryManager.UpdateTwinAsync(...)` | `IotHubServiceClient.Twins.UpdateAsync(...)` |
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

| Version 1.x API | Equivalent version 2.x API |
|:---|:---|
| `ServiceClient` | `IotHubServiceClient`, subclients `Messages`, `MessageFeedback`, `FileUploadNotifications` |
| `ServiceClient.SendAsync(...)` | `IotHubServiceClient.Messages.SendAsync(...)` |
| `ServiceClient.InvokeDeviceMethodAsync(...)` | `IotHubServiceClient.DirectMethods.InvokeAsync(...)` |
| `CloudToDeviceMethod` | `DirectMethodRequest` |
| `CloudToDeviceMethodResult` | `DirectMethodResponse` |
| `ServiceClient.GetFeedbackReceiver(...)` | `IotHubServiceClient.MessageFeedback.MessageFeedbackProcessor` |
| `ServiceClient.GetFileNotificationReceiver()` | `IotHubServiceClient.FileUploadNotifications.FileUploadNotificationProcessor`

#### Other notable breaking changes

- `FeedbackReceiver` is now callback assigned to `MessageFeedbackProcessor` property.
- `GetFileNotificationReceiver()` is now callback assigned to `FileUploadNotificationProcessor` property. These methods return a callback value.
- The `Message` class no longer requires disposal.

#### DeviceMethod

| Version 1.x API | Equivalent version 2.x API |
|:---|:---|
|    |    |

#### JobClient

| Version 1.x API | Equivalent version 2.x API |
|:---|:---|
| `JobsClient` | `IotHubServiceClient`, subclients  `ScheduledJobs` |
| `JobClient.GetJobAsync(...)` | `IotHubServiceClient.ScheduledJobs.GetAsync(...)` |
| `JobClient.CreateQuery()` | `IotHubServiceClient.ScheduledJobs.CreateQueryAsync()` |
| `JobsClient.ScheduleTwinUpdateAsync(...)` | `IotHubServiceClient.ScheduledJobs.ScheduledTwinUpdateAsync(...)` |

#### Other notable breaking changes
- `JobClient.ScheduleTwinUpdateAsync(...)` previously returned a `JobResponse`, now returns `ScheduledJob`.
- `ScheduleJobs.GetAsync()` return type has changed to `QueryResponse<ScheduledJob>` from `IEnumerable<JobResponse>`.

#### DigitalTwinClient

| Version 1.x API | Equivalent version 2.x API |
|:---|:---|
| `DigitalTwinClient` | `IotHubServiceClient.DigitalTwins` |
| `DigitalTwinClient.GetDigitalTwinAsync(...)` | `IotHubServiceClient.DigitalTwins.GetAsync(...)` |
| `DigitalTwinClient.UpdateDigitalTwinAsync(...)` | `IotHubServiceClient.DigitalTwins.UpdateAsync(...)` |
| `UpdateOperationsUtility` | Removed. `Azure.JsonPatchDocument` from Azure.Core package is recommended. |

#### Other notable breaking changes

- Methods on this client have new, simpler return types. Check each method documentation comments for details.
  - Formerly `HttpOperationResponse` and now specific per method call. To get the body of the response before it would have been `HttpOperationResponse.Body` and now it will be, for example, `DigitalTwinGetReponse<T>.DigitalTwin`.
- The update method takes an `InvokeDigitalTwinCommandOptions` which holds the optional payload, connect timeout, and response timeout.
- The `HttpOperationException will no longer be thrown. Exceptions that might be thrown are documented on each method.

### DPS device client

| Version 1.x API | Equivalent version 2.x API |
|:---|:---|
| `ProvisioningDeviceClient.Create(...)` | `new ProvisioningDeviceClient(...)` |
| `ProvisioningDeviceClient` initializer parameter `transportHandler` replaced | `ProvisioningClientOptions` parameter added |

#### Other notable breaking changes

- The security providers that are used in conjunction with this client have changed. See [this section](#security-provider-clients) for more details.
- The previous way of providing transport level settings (`ProvisioningTransportHandler`) has been replaced with `ProvisioningClientTransportSettings`.
- TPM support removed. The library used for TPM operations is broken on Linux and support for it is being shutdown. We'll reconsider how to support HSM.

### DPS service client

| Version 1.x API | Equivalent version 2.x API |
|:---|:---|
| `ProvisioningServiceClient.CreateFromConnectionString(...)` | `new ProvisioningServiceClient()` |
| `QuerySpecification` | Type removed from public API. Methods take the parameters directly. |

#### Other notable breaking changes

- Query methods (like for individual and group enrollments) now take a query string (and optionally a page size parameter), and the `Query` result no longer requires disposing.

### Authentication provider client

Breaking changes:

  - Microsoft.Azure.Devices.Shared.SecurityProvider* types moved from Microsoft.Azure.Devices.Shared.dll into Microsoft.Azure.Devices.Authentication.dll and renamed.

| Version 1.x API | Equivalent version 2.x API |
|:---|:---|
| `SecurityProvider` | `AuthenticationProvider` |
| `SecurityProvider.GetRegistrationID()` | `AuthenticationProvider.GetRegistrationId()` |
| `SecurityProviderSymmetricKey` | `AuthenticationProviderSymmetricKey` |
| `SecurityProviderTpm` | `AuthenticationProviderTpm` |
| `SecurityProviderX509` | `AuthenticationProviderX509` |
| `SecurityProviderX509Certificate` | `AuthenticationProviderX509Certificate` |

#### Other notable breaking changes

- Derived `AuthenticationProvider` types no longer require disposal because of the base class; only select derived types will (e.g., `AuthenticationProviderTpmHsm`.)

## Frequently asked questions

Question:
> What do I gain by upgrading to the 2.x release?

Answer:
> A smaller set of dependencies which makes for a lighter SDK overall, a more concise and clearer API surface, and unit testability.

Question:
> Will the 1.x releases continue to be supported?

Answer:
> The long-term support (LTS) releases of the 1.x SDK will continue to have support during their lifespans.
> Newer features in the services will not be brought into to the v 1.x SDKs. Users are encouraged to upgrade to the 2.x SDK for all the best feature support, stability, and bug fixes.

Question:
> Can I still upload files to Azure Storage using this SDK now that deviceClient.UploadToBlobAsync() has been removed?

Answer:
> Yes, you will still be able to upload files to Azure Storage after upgrading. 
>
> This SDK allows you to get the necessary credentials to upload your files to Azure Storage, but you will need to bring in the Azure Storage SDK as a dependency to do the actual file upload step. 
> 
> For an example of how to do file upload after upgrading, see [this sample](./iothub/device/samples/getting%20started/FileUploadSample/).

Question:
> I was using a deprecated API that was removed in the 2.x upgrade, what should I do?

Answer:
> The deprecated API in the 1.x version documents which API you should use instead of the deprecated API. This guide
also contains a mapping from Version 1.x API to equivalent version 2.x API that should tell you which version 2.x API to use.

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
> Is the version 2.x library backwards compatible in any way?

Answer:
> No. Please refer to [Semver rules](https://semver.org/) and see above in the [Migration guide](#migration-guide).