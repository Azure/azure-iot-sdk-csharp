# SendAsyncTimeout Requirements

## Overview

DeviceClient class is used by the device client callback delegate to prepare the callback results.

## References

##Exposed API

```csharp

sealed class AmqpServiceClient : ServiceClient
{
    ...
    public async override Task SendAsync(string deviceId, Message message, TimeSpan timeout)
}
```

### SendAsyncTimeout Thumbprint

```csharp
public async override Task SendAsync(string deviceId, Message message, TimeSpan timeout)
```

### Result

```csharp

```

### Excepetions

TimeoutException - Response not recieved before timeout was reached.

**SRS_SENDASYNCTIMEOUT_36_1480581: [** Method should throw an error if timeout reached before response is recieved **]**
