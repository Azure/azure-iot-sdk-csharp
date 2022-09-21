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

| V1 class#method                                                                                                               | Changed? | Equivalent V2 class#method                                                                                                   |
|:------------------------------------------------------------------------------------------------------------------------------|:---------|:-----------------------------------------------------------------------------------------------------------------------------|
| TODO: fill in changes                                                                                                         | yes      |                                                                                                                              |

(TODO: do these apply to c#?)
** This method has been split into the three individual steps that this method used to take. See [this file upload sample](./iothub/device/samples/getting%20started/FileUploadSample/) for an example of how to do file upload using these discrete steps.

*** The options that were previously set in this method are now set at DeviceClient constructor time in the optional ClientOptions parameter.

**** Proxy settings are now set at DeviceClient constructor time in the optional ClientOptions parameter,

#### ModuleClient

| V1 class#method                                                                                                               | Changed? | Equivalent V2 class#method                                                                                                   |
|:------------------------------------------------------------------------------------------------------------------------------|:---------|:-----------------------------------------------------------------------------------------------------------------------------|
| TODO: fill in changes                                                                                                         | yes      |                                                                                                                              |

#### Other notable breaking changes

TODO: list breaking changes
- Reduced access levels to classes and methods that were never intended to be public where possible.


### IoT hub service client

| V1 class  | Equivalent V2 Class(es)|
|:---|:---|
| RegistryManager | IotHubServiceClient, subclients Devices, Twins, Configurations, etc. |
| RegistryManager.AddConfigurationAsync(...) | IotHubServiceClient.Configurations.CreateAsync(...) |
| RegistryManager.GetConfigurationsAsync(int maxCount) | IotHubServiceClient.Configurations.GetAsync(int maxCount) |
| RegistryManager.RemoveConfigurationAsync(...) | IotHubServiceClient.Configurations.DeleteAsync(...) |

For v1 classes with more than one equivalent v2 classes, the methods that were in the v1 class have been split up to 
create clients with more cohesive capabilities. For instance, TODO: add example

#### RegistryManager

| V1 class#method | Changed? | Equivalent V2 class#method |
|:---|:---|:---|
|    |    |    |

TODO: is DeviceMethod a class in C#?
#### DeviceMethod

| V1 class#method | Changed? | Equivalent V2 class#method |
|:---|:---|:---|
|    |    |    |

#### JobClient

| V1 class#method | Changed? | Equivalent V2 class#method |
|:---|:---|:---|
|    |    |    |

#### Other notable breaking changes

TODO: verify for C#
- Trust certificates are read from the physical device's trusted root certification authorities certificate store rather than from source.
  - Users are expected to install the required public certificates into this certificate store if they are not present already.
  - See [this document](./upcoming_certificate_changes_readme.md) for additional context on which certificates need to be installed.
  - For most users, no action is needed here since IoT Hub uses the [DigiCert Global G2 CA root](https://global-root-g2.chain-demos.digicert.com/info/index.html) certificate which is already installed on most devices.
- The Bouncycastle dependencies have been removed.
  - The Bouncycastle dependencies were used for some certificate parsing logic that has been removed from the SDK.
- Reduced access levels to classes and methods that were never intended to be public where possible.
- Removed service error code descriptions that the service would never return the error code for.
- Reduce default SAS token time to live from 1 year to 1 hour for security purposes.
- Removed unnecessary synchronization on service client APIs to allow for a single client to make service API calls simultaneously.
- Removed asynchronous APIs for service client APIs.
  - These were wrappers on top of the existing sync APIs. Users are expected to write async wrappers that better fit their preferred async framework.
- Fixed a bug where dates retrieved by the client were converted to local time zone rather than keeping them in UTC time.  

### DPS Device Client

| V1 API  | Equivalent V2 API |
|:---|:---|
| ProvisioningDeviceClient.Create() | new ProvisioningDeviceClient() |
| ProvisioningDeviceClient initializer parameter `transportHandler` replaced | `ProvisioningClientOptions` parameter added |

TODO: verify for c#
No notable changes, but the security providers that are used in conjunction with this client have changed. See [this section](#security-provider-clients) for more details.

### DPS Service Client

TODO: verify for c#
No client APIs have changed for this package, but there are a few notable breaking changes:

- Trust certificates are read from the physical device's trusted root certification authorities certificate store rather than from source.
  - Users are expected to install the required public certificates into this certificate store if they are not present already.
  - See [this document](./upcoming_certificate_changes_readme.md) for additional context on which certificates need to be installed.
  - For most users, no action is needed here since IoT Hub uses the [DigiCert Global G2 CA root](https://global-root-g2.chain-demos.digicert.com/info/index.html) certificate which is already installed on most devices.
- Reduced access levels to classes and methods that were never intended to be public where possible.
- Reduce default SAS token time to live from 1 year to 1 hour for security purposes.

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