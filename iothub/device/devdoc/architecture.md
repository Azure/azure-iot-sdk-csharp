# IoT Hub Device SDK Architecture

## Abstract

This document describes the internal architecture for the Azure IoT Hub Device SDK. Applications use `DeviceClient` and `ModuleClient` which represent the two types of device identities. The objects are internally represented by the same `InternalClient` class that is built by `ClientFactory`. During object creation, a transport pipeline, following the _chain of responsibility_ pattern, is also created. All but the last element of the pipeline will perform alterations of application requests. The last element, based on `TransportHandler`, has the responsibility of converting IoT Hub request messages into the selected protocol and perform network I/O.

## Architecture diagram
<!--arhitecture.puml-->
![csharpDeviceClientSDKArchitecture](https://www.plantuml.com/plantuml/png/0/ZLLTRzem57r7uZzOVMAhaWUUfwg8q1PLQ0WwxTau5x1gxDI-OKFR_lliVBHnuZGy8C6zvvpxRUuDKetvIH1cZbHd2PmvWxdW876RFCyqEt84Xhc6yOW9QWmfdG-KeT8NCXxziPz_ur7jNae4BQaeOSu_7X5oIpAUWU0Ivie2VcDfN2OWu42HoyCJbQa0JRYKeAiVdd0pjMxObKfp0SsWXTDFDehkMlcDHwrye-Yh5oa8Q0Rh0kx8pOkcqcHA8rbNPT-uR3BANka1WlwwKzofxIc3K7CSe40H4iSV8bka275SEcx9zmpap5magmrpeGnubf-KuuFIxn858lKWI_F3q9s0EbpP5OBAoUKfcIJJ-PUFe5kGwU9i6z0GYUCIQZaLUpAV9WtI1XZmiv_QR6UBLQq2r2aJWE1fbrDmujMtoMgwQeWlf4k_oAaRnz81ZoerRNnbziAxsahx1coxfF6LNdvnwJV2MHu1EoBElgR8Zfwd3Dpr5HjC2DqPr7Q3INq7UveB_6GvAbu9tm3goxNWXTPANmjUQuMAAR5HkGCFqrg5l2zVOBlj4ilMSZvQAknp8Iu1IC8DK_RbO0Xf9z7PwIth3-P1Ls-8LqAV48TL0pVyxMyKzGxHSNe7N333ynpcXKSxp1kQyh12kqcEqMbSWGmDgXyTcbK_EZGkX-wv3_kAztUivqmivvfk3TwDrUbcMYpKlNr3x9_rVm00 "csharpDeviceClientSDKArchitecture")

## DeviceClient and ModuleClient

`DeviceClient` and `ModuleClient` are the main public APIs that perform IoT Hub device-side operations. They represent the two identity types available on Azure IoT Hub. There are 4 classes of methods/properties available to applications:

1. Factory methods (`Create`, `CreateFromConnectionString`) are used to create new Device or Module clients by aggregating:
    1. The authentication provider (connection string, `IAuthenticationMethod` and deviceId/moduleId)
    1. The transport configuration (`ITransportSettings`)
1. Network connectivity control and status (`OpenAsync`, `CloseAsync`, `Dispose`, `SetConnectionStatusChangesHandler`)
1. IoT Hub operations
    1. Request/Response (`SendEventAsync`, `ReceiveAsync`, `GetTwinAsync`, `UploadToBlobAsync`, etc.)
    1. Callback (`SetMethodHandlerAsync`, `SetDesiredPropertyUpdateCallbackAsync`, etc.)
1. Configuration (`SetRetryPolicy`, `OperationTimeoutInMilliseconds`)

## InternalClient

The `InternalClient` class performs adaptation from public API calls to the internal IoT Hub interface (`IDelegatingHandler`) adapter. All methods in `DeviceClient` and `ModuleClient` should have an equivalent in `InternalClient`.

_Note_: Above description identifies an anti-pattern in both public and internal API shape that needs to be addressed in the future. i.e. extract the commonalities from DeviceClient and ModuleClient into a public abstract class.

#### Functional requirements
1. Handles public API parameter validation. Internal interfaces should avoid validating parameters (use `Debug.Assert` instead).
1. Creates and configures the internal pipeline.
1. Handles legacy `OperationTimeoutInMilliseconds` adaptation to `CancellationToken` APIs.
1. Handles exception adaptation to maintain SemVer behaviors for existing APIs.
1. Handles thread-safe aspects for _callback API_ registrations by converting into the appropriate `Enable*Async` transport requests.
1. Provides default _callback API_ implementations that will handle events in a default way if the application only subscribed to filtered events or removed its handler after subscribing.

## The transport pipeline

_Note_: The pipeline architecture has changed in v1.19.0.

### DefaultDelegatingHandler

The `DefaultDelegatingHandler` class implements the delegating portion from the "chain of responsibility" pattern by routing messages to the next handler in the chain (`InnerHandler`).

Functional requirements:
1. Implements `IDisposable`: throws when pipeline use-after-dispose is detected.
1. Implements the `IDelegatingHandler` internal IoT Hub protocol methods (see the Architecture diagram).

### RetryDelegatingHandler

`RetryDelegatingHandler` is responsible with keeping the client in sync with the state requested by the application. This class is also responsible with applying the operation retry policy.<br>
__Important:__ No other pipeline component should attempt operation retry or hold state. <br>

Functional requirements:
1. Holds the open / closed state.
1. Holds individual subscription state for the following callback APIs:
    1. Methods
    1. Twin
    1. Module Events
1. Performs _implicit open_ (e.g. when `SendEventAsync` is called before `OpenAsync`).
1. Implements the default `RetryStrategy`
1. Updates the `ConnectionStatusChangesHandler`.
1. Performs reconnect when the `TransportHandler` reports `OnStatusDisconnected`
1. Updates the internal status when the `TransportHandler` reports `OnStatusClosedGracefully` 
1. Maintains the `WaitForTransportClosedAsync` request.

#### Design considerations
Subscriptions can be enabled but never disabled during the lifetime of a client. This simplifies state management and allows common code to work with all protocols. (Only AMQP would support this optimization.) Subscribing twice to the same service is considered an internal bug that causes the code to `Debug.Assert`.

Requests made when the client state is not opened will be enqueued after a single `InnerHandler.OpenAsync` request. This queuing is performed by having operations wait on `SemaphoreSlim::WaitAsync`:

![csharpDeviceClientOpenAsync](https://www.plantuml.com/plantuml/png/0/bLFBJiCm4Bn7oZ_i3xGIzDP3LKIHUWEL8-9Wd4coaci7sodqxzay2ka3jSd1YZoPtNaeon2LZ_NMa0wbyjKAEzPuD0mRdolOXx2tEas6rvF51j7lLp0eL6HRh9ND33pDwHKsQndqTlUU9jP5a44UquJa-KE_E9R4sygmvaWTwcGR1MoLZQp3D4taQsecfCdbVPF52rSmR36dyL8tqE0TUhNtGxNaShhCwvJACY-tpWPI7WJxMQD6rlluGCcYNBLUgj9vJ3lWwHkwRcZaV2OQL1xb79ZNJX91H20EZVCUAshb9HGWEmfbV2KpCNc8x3_68CfVFRSKSDByACBi9i9vOTUzd786bjgsFHTbM_TXr5d1FQ5zxy2cygQSjaHfdVCUgFePnpLT5taKtK3XAEPio5pAlVpIFm00 "csharpDeviceClientOpenAsync")

The internal `RetryStrategy` aggregates two concepts:
1. Exception handling strategy: `IotHubException.IsTransient` is the only exception class considered transient. See [Exception handling](#exception-handling) for more details.
1. Retry timing strategy: see [Retry Policy](retrypolicy.md) for details.

#### Handling disconnects

A `WaitForTransportClosedAsync` request is made right after a successfull open. The design avoids mixing event-based and task-based notifications:

![csharpDeviceClientDisconnect](https://www.plantuml.com/plantuml/png/0/lLHDJyCm3BrNwd_m4Igw2exJD0rjukC2qvZ4OTeXIzms8arAuZBjtvEk-s1ei08IFKLn_FpivzU18sQfAXHmbpDRZl1DSXmgYPf6qd6ZDN8AWmcINOrGOSP8wkoEQQ7GnlsnB559ZLruB55VkvNcOR2zZFiFI4jZNxemR82aqO2-v499bwFu2hSN7y45RefI8M4OQ4C8LcOvWKdXE4OOWF_kKp0U05p5EQ90SG5DO6mS0UK8OjVb9kB1PIevMvEr-5fvmfqmG2tg49wU1NGXYfwPf1jZfvPfLnfBGsKSYgQHsUKyYjhCTEQvspKHDsypgyFts_jUl9DXBscxuRVe0v9aAun8aXzkok_a_xi0L_yYm6dyDQdMisZHLAq6qnoLQjJOxy3UYxUMSKmhR_aw-xERjXcD6OwJwlWzAdtknZ5BgHIGHyZG0jCWYmA5PBJ1q4tXZquxcpnDppjD-BkUQkJcbQEwo09Xrzw19mV-wFzYxm00 "csharpDeviceClientDisconnect")

Handling disconnects is going to apply the configured `RetryStrategy` indefinitely to recover existing subscriptions (e.g. methods, twin, etc.)

### ErrorDelegatingHandler

`ErrorDelegatingHandler` performs error adaptation for non-Azure IoT Hub specific errors thrown by the transport layer (e.g. SocketException or AmqpException) to `IoTHubException` types. Ideally this code should be pushed within the respective handlers. The pipeline class is present for legacy reasons only.

The handler is responsible with logging application-level exceptions. It is guaranteed that these exceptions will be available to applications either through custom `IRetryStrategy` implementations or by having them thrown by the public APIs.

#### Exception handling

Exceptions have been unified under the `IotHubException` root type.

|Fault / Type | Retriable | Not retriable |
|---|---|---|
|Server (catch `IotHubException`) | `ServerErrorException`, `QuotaExceededException`, `ServiceBusyException`, `IotHubThrottledException` | `DeviceMessageLockLostException`, `IotHubSuspendedException`, `MessageTooLargeException`, `DeviceMaximumQueueDepthExceededException`, `DeviceAlreadyExistsException`, `UnauthorizedException`, `DeviceNotFoundException` |
|Network | `IotHubCommunicationException` | `System.Security.Authentication.AuthenticationException` | 

For all above exceptions, the `InnerException` will contain the original exceptions. In case `AggregateException` is thrown we will report only the first one. Handlers can decide to log the exception collection. <br>

### ProtocolRoutingDelegatingHandler

`ProtocolRoutingDelegatingHandler` is iterating through the specified transport types (`ITransportSettings` for HTTP/MQTT/AMQP). Once the protocol is selected and the client successfully connected (`OpenAsync` was successful), it will not change it for the life-time of the client. <br>

Since `ProtocolRoutingDelegatingHandler` is the last in the pipeline, it is also responsible with `Dispose`ing the `TransportHandler` when they report disconnect.

This handler is also intercepting the `WaitForTransportClosedAsync` notification request for unexpected disconnect. (`CloseAsync` and `Dispose` will pass through the cancellation of this request to upstream handlers.) On disconnect, the handler will `Dispose` the `TransportHandler`. This is done to allow `TransportHandler`s to keep minimal or no state which simplifies the design.

### TransportHandler

The pipeline ends with one of the transport handlers: MQTT, AMQP, HTTP

Functional requirements:
1. Transport handlers must be stateless.
1. Notify the pipeline the connectivity state through `OnTransportDisconnected` and `OnTransportClosedGracefuly`
1. Transport handlers must convert exceptions particular to the protocol to follow the [Exception handling](#exception-handling) guidelines.
1. Implement proper `Dispose` semantics.
1. Transport handlers should not take a direct dependency on any library and, instead, have a Platform Adaptation Layer that abstracts the protocol library from the implementation.

Detailed transport handler documentation:
[AMQP Transport stack](amqpstack.md)

### Cancellation

`DeviceClient`/`ModuleClient` timeout is now achieved only via `CancellationToken`, using cooperative cancellation. This means that, in certain cases `OperationTimeoutMilliseconds` may not be honored precisely, as I/O cannot always be cancelled immediately.
The `CancellationToken` is propagated throughout all pipeline components down to the particular `TransportHandler`. In certain transport implementations (e.g. `ReceiveMessageAsync`) the protocol library only accepts a timeout value. We can only cancel when control is returned back to the SDK code which is only after the Timeout used expired.

Immediate cancellation is achieved by using `Dispose()` which closes and disposes all communication objects and the pipeline. If `Dispose` is called while pending operations are active, all will receive an `ObjectDisposedException`.

### ConnectionStatusCallback

| Callback status | Reason |
| --- | ---|
| `(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok)` | Client connected (initially and after a successful retry). |
| `(ConnectionStatus.Disabled, ConnectionStatusChangeReason.Client_Close)` | Application disposed the client. |
| `(ConnectionStatus.Disconnected, ConnectionStatusChangeReason.Communication_Error)` | If no callback subscriptions exist, the client will not automatically connect. A future operation will attempt to reconnect the client. |
| `(ConnectionStatus.Disconnected_Retrying, ConnectionStatusChangeReason.Communication_Error)` | If any callback subscriptions exist (methods, twin, events) and connectivity is lost, the client will try to reconnect. |
| `(ConnectionStatus.Disconnected, ConnectionStatusChangeReason.Retry_Expired)` | Retry timeout. The `RetryDelegatingHandler` will attempt to recover links for a duration of `OperationTimeoutInMilliseconds` (default 4 minutes) according to [this retry policy](https://github.com/Azure/azure-iot-sdk-csharp/blob/main/iothub/device/devdoc/requirements/retrypolicy.md). |
| `(ConnectionStatus.Disconnected, ConnectionStatusChangeReason.Bad_Credential)` | UnauthorizedException during Retry. |
| `(ConnectionStatus.Disconnected, ConnectionStatusChangeReason.Device_Disabled)` | DeviceDisabledException during Retry. |

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
