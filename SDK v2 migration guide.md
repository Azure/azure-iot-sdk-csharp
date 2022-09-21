# SDK v2 migration guide

This document outlines the changes made from this library's 1.X.X releases to its 2.X.X releases. Since this is
a major version upgrade, there are a number of breaking changes that will affect the ability to compile. Provided here
are outlines of the notable breaking changes as well as a mapping from v1 APIs to v2 APIs to aid migrating.

## Table of contents

 - [Why the v1 SDK is being replaced](#Why-the-v1-sdk-is-being-replaced)
 - [What will happen to the v1 SDK](#What-will-happen-to-the-v1-sdk)
 - [Migration Guide](#migration-guide)
   - [IoT Hub Device Client](#iot-hub-device-client)
   - [IoT Hub Service Client](#iot-hub-service-client)
   - [DPS Device Client](#dps-device-client)
   - [DPS Service Client](#dps-service-client)
   - [Security Provider Clients](#security-provider-clients)
   - [Deps](#deps)
 - [Frequently asked questions](#frequently-asked-questions)

## Why the v1 SDK is being replaced

There are a number of reasons why the Azure IoT SDK team chose to do a major version revision. Here are a few of the more important reasons:
  - [Upcoming certificate changes](./upcoming_certificate_changes_readme.md) dictated that the SDK needed to stop pinning on a specific IoT Hub public certificate and start reading certificates from the device certificate store.
  - Several 3rd party dependencies (Bouncycastle, Azure Storage SDK, TODO: other 3rd party dependencies) were becoming harder to carry due to security concerns and they could only be removed by removing or alterring existing APIs.
  - Many existing client classes (RegistryManager, DeviceTwin, DeviceMethod, ServiceClient, etc.) were confusingly named and contained methods that weren't always consistent with the client's assumed responsibilities.
  - Many existing clients had a mix of standard constructors (```new DeviceClient(...)```) and static builder constructors (```DeviceClient.createFromSecurityProvider(...)```) that caused some confusion among users.
  - ```DeviceClient``` and ```ModuleClient``` had unneccessarily different method names for the same operations (```deviceClient.startDeviceTwin(...)``` vs ```moduleClient.startTwin(...)``` TODO: verify this issue exists in c#) that could be easily unified for consistency.
  - ```DeviceClient``` and ```ModuleClient``` had many asynchronous methods whose naming did not reflect that they were asynchronous. This led to some users calling these methods as though they were synchronous.

## What will happen to the v1 SDK

We have released [one final LTS version](https://github.com/Azure/azure-iot-sdk-java/releases/tag/2022-03-04) of the v1 SDK that
we will support like any other LTS release (security bug fixes, some non-security bug fixes as needed), but users are still encouraged
to migrate to v2 when they have the chance. For more details on LTS releases, see [this document](./readme.md#long-term-support-lts).

## Migration Guide

### IoT Hub Device Client

#### DeviceClient

| V1 class#method                                                                                                               | Changed? | Equivalent V2 class#method                                                                                                   |
|:------------------------------------------------------------------------------------------------------------------------------|:---------|:-----------------------------------------------------------------------------------------------------------------------------|

(TODO: do these apply to c#?)
** This method has been split into the three individual steps that this method used to take. See [this file upload sample](./iothub/device/samples/getting%20started/FileUploadSample/) for an example of how to do file upload using these discrete steps.

*** The options that were previously set in this method are now set at DeviceClient constructor time in the optional ClientOptions parameter.

**** Proxy settings are now set at DeviceClient constructor time in the optional ClientOptions parameter,

#### ModuleClient

| V1 class#method                                                                                                               | Changed? | Equivalent V2 class#method                                                                                                   |
|:------------------------------------------------------------------------------------------------------------------------------|:---------|:-----------------------------------------------------------------------------------------------------------------------------|

#### Other notable breaking changes

TODO: do these apply to C#?
- Trust certificates are read from the physical device's trusted root certification authorities certificate store rather than from source.
  - Users are expected to install the required public certificates into this certificate store if they are not present already.
  - See [this document](./upcoming_certificate_changes_readme.md) for additional context on which certificates need to be installed.
  - For most users, no action is needed here since IoT Hub uses the [DigiCert Global G2 CA root](https://global-root-g2.chain-demos.digicert.com/info/index.html) certificate which is already installed on most devices.
- DeviceClient and ModuleClient constructors that take public certificates and private keys as strings have been removed.
  - Users must provide an instance of SSLContext that has their public certificates and private keys loaded into it instead.
  - See [this sample](./device/iot-device-samples/send-event-x509) for the recommended way to create this SSLContext and how to construct your DeviceClient and/or ModuleClient with it.
- deviceClient.uploadToBlobAsync() has been removed.
  - Users can still use deviceClient.getFileUploadSasUri() to get a SAS URI that can be used with the Azure Storage SDK to upload the file.
  - See [this sample](./device/iot-device-samples/file-upload-sample) for the recommended way to upload files.
- The Bouncycastle and Azure Storage SDK dependencies have been removed.
  - Bouncycastle dependencies were removed as they were large and only used for parsing certificates. Now that users are expected to parse certificates, it was safe to remove these dependencies.
  - Azure Storage SDK dependency was removed because it made more sense for the user to pick which version of the Azure Storage SDK works best for their application rather than forcing a particular version onto them.
- Reduced access levels to classes and methods that were never intended to be public where possible.


### IoT hub Service Client
 
| V1 class  | Equivalent V2 Class(es)|
|:---|:---|

For v1 classes with more than one equivalent v2 classes, the methods that were in the v1 class have been split up to 
create clients with more cohesive capabilities. For instance, TODO: add example


#### RegistryManager

| V1 class#method | Changed? | Equivalent V2 class#method |
|:---|:---|:---|


TODO: is DeviceMethod a class in C#?
#### DeviceMethod

| V1 class#method | Changed? | Equivalent V2 class#method |
|:---|:---|:---|


#### JobClient

| V1 class#method | Changed? | Equivalent V2 class#method |
|:---|:---|:---|


#### Other notable breaking changes

TODO: verify for C#
- Trust certificates are read from the physical device's trusted root certification authorities certificate store rather than from source.
  - Users are expected to install the required public certificates into this certificate store if they are not present already.
  - See [this document](./upcoming_certificate_changes_readme.md) for additional context on which certificates need to be installed.
  - For most users, no action is needed here since IoT Hub uses the [DigiCert Global G2 CA root](https://global-root-g2.chain-demos.digicert.com/info/index.html) certificate which is already installed on most devices.
- The Bouncycastle dependencies have been removed.
  - The Bouncycastle dependencies were used for some certificate parsing logic that has been removed from the SDK.
- Reduced access levels to classes and methods that were never intended to be public where possible .
- Removed service error code descriptions that the service would never return the error code for.
- Reduce default SAS token time to live from 1 year to 1 hour for security purposes.
- Removed unnecessary synchronization on service client APIs to allow for a single client to make service API calls simultaneously.
- Removed asynchronous APIs for service client APIs.
  - These were wrappers on top of the existing sync APIs. Users are expected to write async wrappers that better fit their preferred async framework.
- Fixed a bug where dates retrieved by the client were converted to local time zone rather than keeping them in UTC time.  


### DPS Device Client

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


### Security Provider Clients

TODO: verify for c#
Breaking changes:
- Trust certificates are read from the physical device's trusted root certification authorities certificate store rather than from source.
  - Users are expected to install the required public certificates into this certificate store if they are not present already.
  - See [this document](./upcoming_certificate_changes_readme.md) for additional context on which certificates need to be installed.
  - For most users, no action is needed here since IoT Hub uses the [DigiCert Global G2 CA root](https://global-root-g2.chain-demos.digicert.com/info/index.html) certificate which is already installed on most devices.
- Users of the X509 SecurityProvider are expected to pass in the parsed certificates and keys as Java security primitives rather than as strings.
  - See [this sample](./provisioning/provisioning-samples/provisioning-X509-sample) for a demonstration on how to create these Java security primitives from strings.
  

## Frequently Asked Questions

Question:
> What do I gain by upgrading to the 2.X.X release?

Answer:
> You get a smaller set of dependencies which makes for a lighter SDK overall. You also get a more concise and clear API surface. 
> Lastly, and most importantly, you get an SDK that is decoupled from a particular IoT hub and Device Provisioning 
> Service public certificate. This makes these versions more future proof since they aren't tied to a certificate
> that will be changed within a few years and may be changed again beyond then. 

Question:
> Will the 1.X.X releases still be supported in any way?

Answer:
> We will continue to support the long term support releases of the 1.X.X SDK for their lifespans, but we will not bring 
> newer features to the 1.X.X SDK. Users who want access to the upcoming features are encouraged to upgrade to the 2.X.X SDK.

Question:
> After upgrading, when I try to open my client, I get an exception like:
> ```
> TODO: add exception
> ```

Answer:
> TODO: add solution

Question:
> If the SDK now reads certificates from my device's trusted root certification authorities certificate store, does it also read any private keys that I have installed on my device?

Answer:
TODO: does this apply to c#
> No. This SDK only reads the public certificates from your device's trusted root certification authorities certificate store. 
>
> For x509 authenticated connections, you will need to construct the SSLContext with your trusted certificates and private keys and provide it to the SDK.
> See [this sample](./iothub/device/samples/how%20to%20guides/X509DeviceCertWithChainSample/) for an example of how to construct an SSLContext instance with your public certificates and private keys.

Question:
> Can I still upload files to Azure Storage using this SDK now that deviceClient.uploadToBlobAsync() has been removed? (TODO: does this apply to c#?)

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
> After upgrading, some of my catch statements no longer work because the API I was using no longer declares that it throws that exception. Do I still need to catch something there?

Answer:
> In this upgrade, we removed any thrown exceptions from our APIs if the API never threw that exception. Because of that, you are safe to simply remove
> the catch clause in cases like this.

Question:
TODO: verify for c#
> What if I don't want this SDK to read from my device's trusted root certification authorities certificate store? Is there a way to override this behavior?

Answer:
> Yes, there is a way to override this behavior. For a given client, there is an optional parameter that allows you to provide
> the SSLContext to the client rather than allow the client to build the SSLContext for you from the trusted root certification 
> authorities certificate store. In this SSLContext, you have complete control over what certificates to trust. 
>
> For an example of injecting your own SSLContext, see [this sample](./iothub/device/samples/how%20to%20guides/X509DeviceCertWithChainSample/).

Question:
> Does this major version bump bring any changes to what platforms this SDK supports?

Answer:
> No. If you are using a platform that is supported in our 1.X.X releases, then your platform will still be supported in our 2.X.X releases.

Question:
> Is the v2 library backwards compatible in any way?

Answer:
> No. Several APIs have been added to v2 that are not present in v1. On top of that, several APIs in v2 are expected to behave differently than they did in v1.