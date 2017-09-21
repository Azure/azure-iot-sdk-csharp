# ServiceClient Requirements

## Overview

ServiceClient class is used by the Service client callback delegate to prepare the callback results.

### References

### Exposed API

```csharp

public sealed class AmqpServiceClient : ServiceClient
{
    ...
    public async override Task SendAsync(string deviceId, Message message, TimeSpan? timeout = null)
}
```

### SendAsync

```csharp
public async override Task SendAsync(string deviceId, Message message, TimeSpan? timeout = null)
```

**SRS_ServiceClient_36_1480581: [** Method should throw an error if timeout reached before response is recieved **]**

### Exceptions

TimeoutException - Response not recieved before timeout was reached.
