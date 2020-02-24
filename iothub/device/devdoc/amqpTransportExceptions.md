Below is the behavior of the SDK on receiving an exception from the AMQP library being used underneath. If the exception is marked as retryable, the SDK will implement the default retry-policy and attempt to reconnect. For exceptions not marked as retryable, it is advised to inspect the exception details and perform the necessary action as indicated below.

* NOTE - The SDK default [retry policy](./retrypolicy.md) is `ExponentialBackoff`. 

|Exception Name |Error code (if available) |isRetryable  |Action                 |
|------|------|------|------|
| | | | 
| AmqpConnectionForcedException | amqp:connection:forced error | Yes | SDK will retry |
| AmqpConnectionFramingErrorException | amqp:connection:framing-error | Yes | SDK will retry |
| AmqpConnectionRedirectException | amqp:connection:redirect | Yes | SDK will retry |
| AmqpConnectionThrottledException | com.microsoft:device-container-throttled | Yes | SDK will retry, with backoff |
| AmqpDecodeErrorException | amqp:decode-error | No | Mismatch between AMQP message sent by client and received by service; collect logs and contact service |
| AmqpFrameSizeTooSmallException | amqp:frame-size-too-small | No | The AMQP message is not being formed correctly by the SDK, collect logs and contact SDK team |
| AmqpIllegalStateException | amqp:illegal-state | No | Inspect the exception details, collect logs and contact service |
| AmqpInternalErrorException | amqp:internal-error | Yes | SDK will retry |
| AmqpInvalidFieldException | amqp:invalid-field | No | Inspect the exception details, collect logs and contact service |
| AmqpLinkDetachForcedException | amqp:link :detach-forced | Yes | SDK will retry |
| AmqpLinkMessageSizeExceededException | amqp:link :message-size-exceeded | No | The AMQP message size exceeded the value supported by the link, collect logs and contact service |
| AmqpLinkRedirectException	| amqp:link :redirect | Yes | SDK will retry | 
| AmqpLinkStolenException | amqp:link stolen | Yes | SDK will retry |
| AmqpLinkTransferLimitExceededException | amqp:link :transfer-limit-exceeded | Yes | SDK will retry |
| AmqpNotAllowedException	| amqp:not-allowed | No | Inspect the exception details, collect logs and contact service |
| AmqpNotFoundException | amqp:not-found | No | Inspect the exception details, collect logs and contact service |
| AmqpNotImplementedException | amqp:not-implemented | No | Inspect the exception details, collect logs and contact service |
| AmqpPreconditionFailedException | amqp:precondition-failed | No | Inspect the exception details, collect logs and contact service |
| AmqpResourceDeletedException | amqp:resource-deleted | No | Inspect the exception details, collect logs and contact service |
| AmqpResourceLimitExceededException | amqp:resource-limit-exceeded | No | Inspect the exception details, collect logs and contact service |
| AmqpResourceLockedException | amqp:resource-locked | Yes | SDK will retry |
| AmqpSessionErrantLinkException | amqp:session:errant-link | Yes | SDK will retry |
| AmqpSessionHandleInUseException | amqp:session:handle-in-use | Yes | SDK will retry |
| AmqpSessionUnattachedHandleException | amqp:session:unattached-handle | Yes | SDK will retry |
| AmqpSessionWindowViolationException | amqp:session:window-violation | Yes | SDK will retry |
| AmqpUnauthorizedAccessException | amqp:unauthorized-access | No | SDK will throw `UnauthorizedException` with Connection status reason `BAD_CREDENTIAL` |

* NOTE - For exceptions marked as retryable, the SDK will implement its retry policy internally, and you do not need to take any action. For non-retryable exceptions, you can inspect the exception details, and if a reconnection makes sense, you should dispose of the existing `DeviceClient` instance and then initialize a new client (initializing a new `DeviceClient` instance without disposing the previously used instance will cause them to fight for the same connection resources).