Below is the behavior of the SDK on receiving an exception over Amqp transport protocol. If the exception is marked as retryable, the exception will get translated to an `AmqpIoTResourceException` internally, and the SDK will implement the default retry-policy and attempt to reconnect. For exceptions not marked as retryable, it is advised to inspect the exception details and perform the necessary action as indicated below.

* NOTE - The SDK default [retry policy](./retrypolicy.md) is `ExponentialBackoff`. 

#### Retried by the SDK (until retry policy is exhausted):

|Error code | Mapped to | Details/Inner exception | Notes |
|------|------|------|------|
| | | | | 
| amqp:connection:forced error | AmqpIoTResourceException | SDK will retry, this exception will not bubble up to the application directly  |  |
| amqp:connection:framing-error | AmqpIoTResourceException | SDK will retry, this exception will not bubble up to the application directly  |  |
| amqp:connection:redirect | AmqpIoTResourceException | SDK will retry, this exception will not bubble up to the application directly  |  |
| amqp:link :detach-forced | AmqpIoTResourceException | SDK will retry, this exception will not bubble up to the application directly |  |
| amqp:link :redirect | AmqpIoTResourceException | SDK will retry, this exception will not bubble up to the application directly |  | 
| amqp:link stolen | AmqpIoTResourceException | SDK will retry, this exception will not bubble up to the application directly |  |
| amqp:link :transfer-limit-exceeded | AmqpIoTResourceException | SDK will retry, this exception will not bubble up to the application directly |  |
| amqp:resource-locked | AmqpIoTResourceException | SDK will retry, this exception will not bubble up to the application directly |  |
| amqp:session:errant-link | AmqpIoTResourceException | SDK will retry, this exception will not bubble up to the application directly |  |
| amqp:session:handle-in-use | AmqpIoTResourceException | SDK will retry, this exception will not bubble up to the application directly |  |
| amqp:session:unattached-handle | AmqpIoTResourceException | SDK will retry, this exception will not bubble up to the application directly |  |
| amqp:session:window-violation | AmqpIoTResourceException | SDK will retry, this exception will not bubble up to the application directly |  |
| amqp:internal-error | IotHubCommunicationException | SDK will retry, this exception will not bubble up to the application directly |  |
| amqp:transaction :rollback | IotHubCommunicationException | SDK will retry, this exception will not bubble up to the application directly |  |
| amqp:transaction :timeout | IotHubCommunicationException | SDK will retry, this exception will not bubble up to the application directly |  |
| com.microsoft:timeout | TimeoutException | SDK will retry, this exception will not bubble up to the application directly |  |
| com.microsoft:device-container-throttled | IotHubThrottledException | SDK will retry, this exception will not bubble up to the application directly | Always generated from an AmqpOutcome |

#### Not retried by the SDK:

|Error code | Mapped to | Details/Inner exception | Notes |
|------|------|------|------|
| | | | | 
| amqp:link :message-size-exceeded | MessageTooLargeException | InnerException: AmqpException.Error.Condition = AmqpSymbol.MessageSizeExceeded | The AMQP message size exceeded the value supported by the link, collect logs and contact service |
| amqp:invalid-field | InvalidOperationException | InnerException: AmqpException.Error.Condition = AmqpSymbol.InvalidField | Inspect the exception details, collect logs and contact service |
| amqp:decode-error | IotHubException | InnerException: AmqpException.Error.Condition = AmqpSymbol.DecodeError | Mismatch between AMQP message sent by client and received by service; collect logs and contact service |
| amqp:frame-size-too-small | IotHubException | InnerException: AmqpException.Error.Condition = AmqpSymbol.FrameSizeTooSmall | The AMQP message is not being formed correctly by the SDK, collect logs and contact SDK team |
| amqp:illegal-state | IotHubException | InnerException: AmqpException.Error.Condition = AmqpSymbol.IllegalState | Inspect the exception details, collect logs and contact service |
| amqp:not-allowed | InvalidOperationException	| InnerException: AmqpException.Error.Condition = AmqpSymbol.NotAllowed | Inspect the exception details, collect logs and contact service |
| amqp:not-found | DeviceNotFoundException | This is the exception thrown when the error is received as an AmqpException | Verify that your device exists, and is enabled (on your IoT hub instance) |
| amqp:not-found | DeviceMessageLockLostException | This is the exception thrown when the error is received as a Rejected outcome for an Amqp `DisposeMessageAsync()` operation | The device client attempted to complete/reject/abandon a received cloud-to-device message, but the lock token was expired (possible cause is that device reconnected after receiving the c2d message). <br/> Call `ReceiveAsync()` again to retrieve an updated lock token, and then complete/reject/abandon the message. De-duplication logic wil need to be implemented at the application level |
| amqp:not-implemented | NotSupportedException | InnerException: AmqpException.Error.Condition = AmqpSymbol.NotImplemented | Inspect the exception details, collect logs and contact service |
| amqp:precondition-failed | IotHubException | InnerException: AmqpException.Error.Condition = AmqpSymbol.PreconditionFailed | Inspect the exception details, collect logs and contact service |
| amqp:resource-deleted | IotHubException | InnerException: AmqpException.Error.Condition = AmqpSymbol.ResourceDeleted | Inspect the exception details, collect logs and contact service |
| amqp:resource-limit-exceeded | DeviceMaximumQueueDepthExceededException | The correct exception type for this error code is `QuotaExceededException` but it was incorrectly mapped to `DeviceMaximumQueueDepthExceededException`. In order to avoid a breaking change, we now return the correct exception details as an inner exception within the `DeviceMaximumQueueDepthExceededException` thrown. | Upgrade or increase the number of units on your IoT Hub or wait until the next UTC day for the daily quota to refresh and then retry the operation. |
| amqp:unauthorized-access | UnauthorizedException | InnerException: AmqpException.Error.Condition = AmqpSymbol.UnauthorizedAccess | Inspect your credentials |
| com.microsoft:message-lock-lost | DeviceMessageLockLostException | The device client attempted to complete/reject/abandon a received cloud-to-device message, but the lock token was expired (took > 1min after receiving the message to complete/reject/abandon it) | Call `ReceiveAsync()` again to retrieve an updated lock token, and then complete/reject/abandon the message. De-duplication logic wil need to be implemented at the application level |
| amqp:transaction :unknown-id | IotHubException | InnerException: AmqpException.Error.Condition = AmqpSymbol.TransactionUnknownId | Inspect the exception details, collect logs and contact service |
| com.microsoft:argument-error | ArgumentException | | Inspect the exception details, collect logs and contact service |
| com.microsoft:argument-out-of-range | ArgumentOutOfRangeException | | Inspect the exception details, collect logs and contact service |
| com.microsoft:iot-hub-suspended | IotHubSuspendedException | | Inspect the exception details, collect logs and contact service |
| | IotHubException | An exception not mapped to a different type will default to IotHubException and will be non-retryable | Inspect the exception details, collect logs and contact service |

* NOTE - For exceptions marked as retryable, the SDK will implement its retry policy internally, and you do not need to take any action. For non-retryable exceptions, or after the SDK retry policy has expired, you should inspect both the connection status change handler and the returned exception details to determine the next step.

* If the device is in `Connected` state, you can perform subsequent operations on the same client instance.
* If the device is in `Disconnected_Retrying` state, then the SDK is trying to recover its connection. Wait until device recovers and reports a `Connected` state, and then perform subsequent operations.
* If the device is in `Disconnected` or `Disabled` state, then the underlying transport layer has been disposed. You should dispose the existing `DeviceClient` instance and initialize a new client (initializing a new `DeviceClient` instance without disposing the previously used instance will cause contention for the same connection resources).