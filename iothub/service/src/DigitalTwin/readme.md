  > The `DigitalTwinClient` is available only on .NET framework 4.7.2 and .NET Standard 2.0+.

### Protocol layer generation:

- Run the powershell script [generateCode.ps](./generateCode.ps1). It will pick up the [autorest config](./autorest.md) and output the results into this folder. It will also make a few automated changes to the generated protocol layer, that are required for this client library.

### Examples

You can familiarize yourself with different APIs using [samples for DigitalTwinClient](https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Samples/service/DigitalTwinClientSamples).

## Source code folder structure

### /Authentication

The code for generating shared access signature tokens, used for authentication Http requests against Azure IoT Hub service.

### /Generated

The code generated by autorest using the swagger file defined under [DigitalTwin.json](./DigitalTwin.json).

### /src/Customized

The customzied code written to override the following behavior of auto-generated code:

- Rename some of the generated types, eg. [DigitalTwinInvokeCommandHeaders](./Customized/DigitalTwinInvokeCommandHeaders.cs)
- Declare some of the generated types as **internal**, instead of the autorest default of **public**.
- Declare some methods to accept input parameters as **strings** instead of **objects**.
- Declare some methods to return the response as **strings** instead of **objects**.

### /src/Models

Model classes useful for use with the Digital Twin client operations.

### /src/Serialization

Serialization helpers provided to help serialize/deserialize commonly used types when working with digital twins.

## Troubleshooting

All service operations will throw HttpOperationException on failure reported by the service, with helpful error codes and other information.

```csharp
try
{
    HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders> invokeCommandResponse = await digitalTwinClient.InvokeCommandAsync(
        digitalTwinId,
        commandName,
        serializedPayload);
}
catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
{
    // Service could not reach the device, make sure the device is online.
}
```

## Using delegating handlers with DigitalTwinClient

The `DigitalTwinClient.CreateFromConnectionString()` factory method takes in an optional list of `System.Net.Http.DelegatingHandler`, which are added to the Http client pipeline. You can use these delegating handler to specify your custom policies for tracing, retry, routing through a proxy, etc.

Sample usage for logging each Http request and response:

```csharp
internal class LoggingHandler : DelegatingHandler
{
    private readonly ILogger _logger;

    public LoggingHandler(ILogger logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogTrace($"Request: {request}");
        try
        {
            // base.SendAsync calls the inner handler
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            _logger.LogTrace($"Response: {response}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get response: {ex}");
            throw;
        }
    }
}

DelegatingHandler[] handlers = { new LoggingHandler(Logger) };
using var digitalTwinClient = DigitalTwinClient.CreateFromConnectionString(connectionString, handlers);
```