# SDK v2 migration guide

This document outlines the changes made from this library's 1.X.X releases to its 2.X.X releases. Since this is
a major version upgrade, there are a number of breaking changes that will affect the ability to compile. Provided here
are outlines of the notable breaking changes as well as a mapping from v1 APIs to v2 APIs to aid migrating.

## Table of contents

 - [Why the v1 SDK is being replaced](#Why-the-v1-sdk-is-being-replaced)
 - [What will happen to the v1 SDK](#What-will-happen-to-the-v1-sdk)
 - [Migration guide](#migration-guide)
   - [IoT hub device client](#iot-hub-device-client)
   - [IoT hub service client](#iot-hub-service-client)
   - [Device Provisioning Service (DPS) device client](#dps-device-client)
   - [DPS service slient](#dps-service-client)
   - [Security provider clients](#security-provider-clients)
 - [Frequently asked questions](#frequently-asked-questions)

## Why the v1 SDK is being replaced

There are a number of reasons why the Azure IoT SDK team chose to do a major version revision. Here are a few of the more important reasons:
  - Removing or upgrading several NuGet dependencies (TODO: list).
  - Consolidate IoT hub service clients and rename to reflect the items or operations they support. Many existing client classes (RegistryManager, ServiceClient, etc.) were confusingly named and contained methods that weren't always consistent with the client's assumed responsibilities.
  - Many existing clients had a mix of standard constructors (`new DeviceClient(...)`) and static builder methods (`DeviceClient.CreateFromConnectionString(...)`) that caused some confusion among users. The factory methods have been removed and the addition of constructors in clients enables unit testing.

## What will happen to the v1 SDK

We have released [one final LTS version](TODO:) of the v1 SDK that
we will support like any other LTS release (security bug fixes, some non-security bug fixes as needed), but users are still encouraged
to migrate to v2 when they have the chance. For more details on LTS releases, see [this document](./readme.md#long-term-support-lts).

## Migration guide

### IoT hub device client

#### DeviceClient

| V1 class#method | Changed? | Equivalent V2 class#method |
|:------------------------------------------------------------------------------------------------------------------------------|:---------|:-----------------------------------------------------------------------------------------------------------------------------|
| `DeviceClient` | yes | `IotHubDeviceClient` |
| `TransportType` | yes | `Transport` and `IotHubClientTransportProtocol` |
| `ReceiveAsync` | yes | `ReceiveMessageAsync` |
| `CompleteAsync` | yes | `CompleteMessageAsync` |
| `SetConnectionStatusChangesHandler` | yes | `SetConnectionStatusChangeHandler` |

(TODO: elaborate on breaking changes)

#### ModuleClient

| V1 class#method | Changed? | Equivalent V2 class#method |
|:------------------------------------------------------------------------------------------------------------------------------|:---------|:-----------------------------------------------------------------------------------------------------------------------------|
| `ModuleClient` | yes | `IotHubModuleClient` |

#### Other notable breaking changes

TODO: list breaking changes
- Reduced access levels to classes and methods that were never intended to be public where possible.

### IoT hub service client

#### RegistryManager

| V1 class#method | Equivalent V2 class#method |
|:---|:---|:---|
| `RegistryManager` | `IotHubServiceClient`, subclients `Devices`, `Twins`, `Configurations`, etc. |
| `RegistryManager.GetTwinAsync(...)` | `IotHubServiceClient.Twins.GetAsync(...)` |
| `RegistryManager.UpdateTwinAsync(...)` | `IotHubServiceClient.Twins.UpdateAsync(...)` |
| `ServiceClient.InvokeDeviceMethodAsync(...)` | `ServiceClient.DirectMethods.InvokeAsync(...)` |
| `CloudToDeviceMethod` | `DirectMethodRequest` |
| `CloudToDeviceMethodResult` | `DirectMethodResponse` |
| `RegistryManager.AddConfigurationAsync(...)` | `IotHubServiceClient.Configurations.CreateAsync(...)` |
| `RegistryManager.GetConfigurationsAsync(int maxCount)`| `IotHubServiceClient.Configurations.GetAsync(int maxCount)` |
| `RegistryManager.RemoveConfigurationAsync(...)` | `IotHubServiceClient.Configurations.DeleteAsync(...)` |

#### Other notable breaking changes

- `CloudToDeviceMethod` took a constructor parameter for the method name, which is now used with `DirectMethodRequest` as a property initializer.
- Operations that offer concurrency protection using `ETag`s, now take a parameter `onlyIfUnchanged` that relies on the ETag property of the submitted entity.

#### DeviceMethod

| V1 class#method | Changed? | Equivalent V2 class#method |
|:---|:---|:---|
|    |    |    |

#### DigitalTwinClient

| V1 class#method | Equivalent V2 class#method |
|:---|:---|
| `DigitalTwinClient` | `IotHubServiceClient.DigitalTwins` |
| `DigitalTwinClient.GetDigitalTwinAsync(...)` | `IotHubServiceClient.DigitalTwins.GetAsync(...)` |
| `DigitalTwinClient.UpdateDigitalTwinAsync(...)` | `IotHubServiceClient.DigitalTwins.UpdateAsync(...)` |
| `UpdateOperationsUtility` | Removed. Use `Azure.JsonPatchDocument` from Azure.Core package. |

#### Other notable breaking changes

- Methods on this client have new, simpler return types. Check each method documentation comments for details.
  - Formerly `HttpOperationResponse` and now specific per method call. To get the body of the response before it would have been `HttpOperationResponse.Body` and now it will be, for example, `DigitalTwinGetReponse<T>.DigitalTwin`.
- The update method takes an `InvokeDigitalTwinCommandOptions` which holds the optional payload, connect timeout, and response timeout.
- The `HttpOperationException will no longer be thrown. Exceptions that might be thrown are documented on each method.

### DPS device client

| V1 API  | Equivalent V2 API |
|:---|:---|
| ProvisioningDeviceClient.Create() | new ProvisioningDeviceClient() |
| ProvisioningDeviceClient initializer parameter `transportHandler` replaced | `ProvisioningClientOptions` parameter added |

### Other notable changes

- The security providers that are used in conjunction with this client have changed. See [this section](#security-provider-clients) for more details.

### DPS service client

| V1 API  | Equivalent V2 API |
|:---|:---|
| ProvisioningServiceClient.CreateFromConnectionString() | new ProvisioningServiceClient() |
| QuerySpecification | Type removed from public API. Methods take the parameters directly. |

### Other notable changes

- Query methods (like for individual and group enrollments) now take a query string (and optionally a page size parameter), and the `Query` result no longer requires disposing.

### Authentication provider client

Breaking changes:

  - Microsoft.Azure.Devices.Shared.SecurityProvider* types moved from Microsoft.Azure.Devices.Shared.dll into Microsoft.Azure.Devices.Authentication.dll and renamed.

| V1 API | Equivalent V2 API |
|:---|:---|
| SecurityProvider | AuthenticationProvider |
| SecurityProvider was IDisposable |  AuthenticationProvider is not Disposable and only some derived types are |
| SecurityProvider.GetRegistrationID() | AuthenticationProvider.GetRegistrationId() |
| SecurityProviderSymmetricKey | AuthenticationProviderSymmetricKey |
| SecurityProviderTpm | AuthenticationProviderTpm |
| SecurityProviderX509 | AuthenticationProviderX509 |
| SecurityProviderX509Certificate | AuthenticationProviderX509Certificate |

## Frequently Asked Questions

Question:
> What do I gain by upgrading to the 2.X.X release?

Answer:
> A smaller set of dependencies which makes for a lighter SDK overall, a more concise and clearer API surface, and unit testability.

Question:
> Will the 1.X.X releases continue to be supported?

Answer:
> The long-term support (LTS) releases of the 1.X.X SDK will continue to have support during their lifespans.
> Newer features in the services will not be brought into to the v 1.X.X SDKs. Users are encouraged to upgrade to the 2.X.X SDK for all the best feature support, stability, and bug fixes.

Question:
> Can I still upload files to Azure Storage using this SDK now that deviceClient.UploadToBlobAsync() has been removed?

Answer:
> Yes, you will still be able to upload files to Azure Storage after upgrading. 
>
> This SDK allows you to get the necessary credentials to upload your files to Azure Storage, but you will need to bring in the Azure Storage SDK as a dependency to do the actual file upload step. 
> 
> For an example of how to do file upload after upgrading, see [this sample](./iothub/device/samples/getting%20started/FileUploadSample/).

Question:
> I was using a deprecated API that was removed in the 2.X.X upgrade, what should I do?

Answer:
> The deprecated API in the 1.X.X version documents which API you should use instead of the deprecated API. This guide
also contains a mapping from v1 API to equivalent v2 API that should tell you which v2 API to use.

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
> Is the v2 library backwards compatible in any way?

Answer:
> No. Please refer to [Semver rules](https://semver.org/) and see above in the [Migration guide](#migration-guide).