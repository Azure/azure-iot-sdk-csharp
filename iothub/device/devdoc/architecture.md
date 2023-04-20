# IoT Hub Device SDK Architecture

## Abstract

This document describes the internal architecture for the Azure IoT Hub Device SDK. Applications use `IotHubDeviceClient` and `IotHubModuleClient` which represent the two types of device identities. During object creation, a transport pipeline, following the _chain of responsibility_ pattern, is also created. All but the last element of the pipeline will perform alterations of application requests. The last element, based on `TransportHandlerBase`, has the responsibility of converting IoT Hub request messages into the selected protocol and perform network I/O.

## Architecture diagram
<!--arhitecture.puml-->
![csharpDeviceClientSDKArchitecture](https://www.plantuml.com/plantuml/png/bLJDKjim4BxxAJISQ9dumBd3G1CWEGGo9fcU5siJDCYIaDP0XlBkZHpNZiF6JO_Q_NxMgq--30IUoa8dkOOGnCJIk7py2G67Mg6Xv2CHuapCb4Ej30wj8Nod_NE5cOnGumf8cTKOZDJenSGOzFKX-KngZn6-ghpM5yc7Q3jJAqlTTc9ZVo0n67plAB28Leb7N8YJkGSJdFNjnvmaYOFy6LR8TaMr2sK8_H9ovBvtquyO8TAq5NWW84pJgducxXsQgx0sUNqUQXjmj_0B5DrOt_0hWDn5JC8YmAF6sccx344sQfLId8xEXzyQlmSqSeuQ2oI_RbNIj097Liq_7PwFMjgrYGuPvgXUSOeXmAepvKY5Fa2QJf6Uh0nxWdMLKM8wSVHjESFWh0aTW_cRCetwBL8yxx8NJW9XXd67A6VUElIai75eXuhC6L3-n8p5zD9qF9_FHDG3KjQ4tIbkbCQ5qnRa0uI5LvshjeNJIDXYEhTgNktpTWrDVPD_8Vu-cYMKclZCTQ53QmpcSRGgUbyeNnbP3CSnqjVlERged641plLfDW7c_UkLU0J2Etnhq9FbdQKCGQwFU0-r8Jm6yf3JldFj5n_-ZkUzsTEwtN1nMMgz_ehOn23VhXDbz_kkYXVNSU8tDdqXEW8s9kfLu_usFtuF1WTpaOYsnHBvb1mlm8Nc25fVXFgP_Odgf_AHOCiLWRssFW8NXtu2T-E9-EUrYC75mStpZKZfs_hXteRNktE-MLl-pyuZdDt7PtA9HfQ5_WC0 "csharpDeviceClientSDKArchitecture")

## IotHubDeviceClient and IotHubModuleClient

`IotHubDeviceClient` and `IotHubModuleClient` are the main public APIs that perform IoT Hub client-side operations. They represent the two identity types available on Azure IoT Hub. An `IotHubModuleClient` can be an IoT device module or an IoT Edge module. There are 3 classes of methods/properties available to applications:

1. Constructors are used to create new device or module clients by aggregating:
    1. The authentication provider (connection string, `IAuthenticationMethod` and deviceId/moduleId)
    1. The optional configuration (`IotHubClientOptions`) which can be used to specify `IotHubClientTransportSettings`, `RetryPolicy`, `PayloadConvention`, etc.
1. Network connectivity control and status (`OpenAsync`, `CloseAsync`, `DisposeAsync`, `ConnectionStatusChangeCallback`, `ConnectionStatusInfo`)
1. IoT Hub operations
    1. Request/Response (`SendTelemetryAsync`, `GetTwinPropertiesAsync`, `UpdateReportedPropertiesAsync`, `GetFileUploadSasUriAsync`, etc.)
    1. Callback (`SetIncomingMessageCallbackAsync`, `SetDirectMethodCallbackAsync`, `SetDesiredPropertyUpdateCallbackAsync`, etc.)

#### Functional requirements
1. Handles public API parameter validation. Internal interfaces should avoid validating parameters (use `Debug.Assert` instead).
1. Creates and configures the internal pipeline.
1. Handles exception adaptation to maintain SemVer behaviors for existing APIs.
1. Handles thread-safe aspects for _callback API_ registrations by converting into the appropriate `Enable*Async` transport requests.

## The transport pipeline

### DefaultDelegatingHandler

The `DefaultDelegatingHandler` class implements the delegating portion from the "chain of responsibility" pattern by routing messages to the next handler in the chain (`InnerHandler`).

Functional requirements:
1. Implements `IDisposable`: throws when pipeline use-after-dispose is detected.
1. Implements the `IDelegatingHandler` internal IoT Hub protocol methods (see the Architecture diagram).

### ConnectionStateDelegatingHandler

The `ConnectionStateDelegatingHandler` class is responsible for keeping the client in sync with the state requested by the application.
__Important:__ No other pipeline component should hold state.<br>

Functional requirements:
1. Holds the open / closed state.
1. Holds individual subscription state for the following callback APIs:
    1. Incoming messages
    1. Methods
    1. Twin
1. Verifies the client state before attempting any non-state-change operation. Client operations executed without opening the client result in an `InvalidOperationException`.
1. Updates the `ConnectionStatusChangeCallback`.
1. Performs reconnect when the `TransportHandlerBase` reports `OnTransportDisconnected`
1. Updates the internal status when the `TransportHandlerBase` reports `OnTransportClosedGracefully` 
1. Maintains the `WaitForTransportClosedAsync` request.
1. Cancels any in-progress client operations when `CloseAsync` is called.

#### Handling disconnects

A `WaitForTransportClosedAsync` request is made right after a successfull open. The design avoids mixing event-based and task-based notifications:

![csharpDeviceClientDisconnect](https://www.plantuml.com/plantuml/png/0/lLHDJyCm3BrNwd_m4Igw2exJD0rjukC2qvZ4OTeXIzms8arAuZBjtvEk-s1ei08IFKLn_FpivzU18sQfAXHmbpDRZl1DSXmgYPf6qd6ZDN8AWmcINOrGOSP8wkoEQQ7GnlsnB559ZLruB55VkvNcOR2zZFiFI4jZNxemR82aqO2-v499bwFu2hSN7y45RefI8M4OQ4C8LcOvWKdXE4OOWF_kKp0U05p5EQ90SG5DO6mS0UK8OjVb9kB1PIevMvEr-5fvmfqmG2tg49wU1NGXYfwPf1jZfvPfLnfBGsKSYgQHsUKyYjhCTEQvspKHDsypgyFts_jUl9DXBscxuRVe0v9aAun8aXzkok_a_xi0L_yYm6dyDQdMisZHLAq6qnoLQjJOxy3UYxUMSKmhR_aw-xERjXcD6OwJwlWzAdtknZ5BgHIGHyZG0jCWYmA5PBJ1q4tXZquxcpnDppjD-BkUQkJcbQEwo09Xrzw19mV-wFzYxm00 "csharpDeviceClientDisconnect")

Handling disconnects is going to apply the configured `RetryStrategy` indefinitely to recover existing subscriptions (e.g. methods, twin, etc.)

### RetryDelegatingHandler

`RetryDelegatingHandler` class is responsible with applying the operation retry policy.<br>
__Important:__ No other pipeline component should attempt operation retry.<br>

Functional requirements:
1. Implements the default `RetryStrategy`

### ExceptionRemappingHandler

`ExceptionRemappingHandler` performs error adaptation for non-Azure IoT Hub specific errors thrown by the transport layer (e.g. SocketException or AmqpException) to `IoTHubClientException` types. Ideally this code should be pushed within the respective handlers. The pipeline class is present for legacy reasons only.

Exceptions that are transient have `IotHubClientException.IsTransient` set to `true`.

The handler is responsible with logging application-level exceptions. It is guaranteed that these exceptions will be available to applications either through custom `IRetryStrategy` implementations or by having them thrown by the public APIs.

### TransportDelegatingHandler

This handler intercepts the `WaitForTransportClosedAsync` notification request for unexpected disconnect. (`CloseAsync` and `DisposeAsync` will pass through the cancellation of this request to upstream handlers.)

### TransportHandlerBase

The pipeline ends with one of the transport handlers: MQTT or AMQP

Functional requirements:
1. Transport handlers must be stateless.
1. Notify the pipeline the connectivity state through `OnTransportDisconnected` and `OnTransportClosedGracefuly`
1. Transport handlers must convert exceptions particular to the protocol into `IotHubClientException`.
1. Implement proper `Dispose` semantics.

#### HttpTransportHandler

There are some operations that are available only over HTTP. These operations are imeplemented over HTTP even though the client itself is never initialized over HTTP.
The HTTP transport layer is placed at the end of the delegating handler pipeline so that it can implement these operations.

The operations implemented over HTTP are:
1. `GetFileUploadSasUriAsync`
1. `CompleteFileUploadAsync`
1. `InvokeMethodAsync`

__Important:__ HTTP transport communicates over port 443 so you will need to ensure this port is open for traffic.<br>

### Cancellation

`IotHubDeviceClient`/`IotHubModuleClient` timeout is achieved only via `CancellationToken`, using cooperative cancellation.
The `CancellationToken` is propagated throughout all pipeline components down to the particular `TransportHandler`. 

Immediate cancellation is achieved by using `DisposeAsync()` which closes and disposes all communication objects and the pipeline. If `DisposeAsync` is called while pending operations are active, all will receive an `ObjectDisposedException`.

## Logging
Logging is implemented using EventSource which is available in most NetStandard2.0 platforms.

### Log messages
The following rules apply when adding internal logging:

1. Each assembly must have a separate EventSource name.
2. To avoid unnecessary processing of logging parameters (string format, etc) logging must be called after an `if (Logging.IsEnabled)` statement.
3. Avoid `Info` free form messages and favor strong typed messages.

#### Common logging

| Log type | Usage |
|----------|-------|
| Enter | Log immediately after entering a method |
| Exit  | Log right before existing. This should be logged when expected exceptions are thrown (in a finally block) |
| Associate | Always specify at least 3 parameters: `Logging.Associate(thisOrContextObject:this, first:deviceClient, second:internalClient);` (parameter names added for clarity).<br> `first` must correspond to the parent object and `second` to the child. This relation is used by our logging parsing scripts to extract single device/module logs from larger logs. |
| Info | Free form informational message. Avoid and favor typed messages (see `Logging.DeviceClient.cs`).
| Error | Use to report exceptions. |
| Fail | Use to report critical exceptions. Using this will cause the process to crash in `Debug` mode. |

#### Strong typed logging

Logging allows for new EventSource IDs to be created. These must be placed in the assembly-specific EventSource definition file (e.g. Logging.DeviceClient.cs). The advantage of these logs is consistency between releases. Applications can take dependencies on the format which will be maintained considering SemVer semantics.
