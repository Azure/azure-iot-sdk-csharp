---
page_type: sample
description: "A set of samples that show how a device that uses the IoT Plug and Play conventions interacts with either IoT Hub or IoT Central."
languages:
- csharp
products:
- azure
- azure-iot-hub
- azure-iot-central
- azure-iot-pnp
- dotnet
urlFragment: azure-iot-pnp-device-samples-for-csharp-net
---

# IoT Plug And Play (PnP) device/ module APIs

Devices/ modules connecting to IoT Hub that announce their DTDL model Id during initialization can now perform convention-based operations. One such convention supported is [IoT Plug and Play][pnp-convention].

These devices/ modules can now use the native PnP APIs in the Azure IoT device SDKs to directly exchange messages with an IoT Hub, without having to specify any metadata information that needs to accompany these messages.

## Client initialization

### Announce model ID during client initialization (same as before)

```csharp
var options = new ClientOptions
{
    ModelId = ModelId,
};

DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt, options);
```

### Define the convention that the client follows (newly introduced)

```csharp
// Specify a custom System.Text.Json based PayloadConvention to be used.
// If not specified the library defaults to a convention that uses Newtonsoft.Json-based serializer and Utf8-based encoder.
var options = new ClientOptions(SystemTextJsonPayloadConvention.Instance)
{
    ModelId = ModelId,
};

DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt, options);
```

## Comparision of API calls (non-convention aware APIs vs convention-aware APIs):

## Telemetry

### Send no-component telemetry:

#### Using non-convention aware API:

```csharp
// Send telemetry "workingSet".
long workingSet = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;
var telemetry = new Dictionary<string, object>
{
    ["workingSet"] = workingSet,
};

using var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemetry)))
{
    MessageId = s_random.Next().ToString(),
    ContentEncoding = "utf-8",
    ContentType = "application/json",
};
await _deviceClient.SendEventAsync(message, cancellationToken);
```

#### Using convention aware API:

```csharp
// Send telemetry "workingSet".
long workingSet = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;
using var telemetryMessage = new TelemetryMessage
{
    MessageId = Guid.NewGuid().ToString(),
    Telemetry = { ["workingSet"] = workingSet },
};

await _deviceClient.SendTelemetryAsync(telemetryMessage, cancellationToken);
```

### Send component-level telemetry:

#### Using non-convention aware API:

```csharp
// Send telemetry "workingSet" under component "thermostat1".
long workingSet = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;
var telemetry = new Dictionary<string, object>()
{
    ["workingSet"] = workingSet,
};

using var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemetry)))
{
    MessageId = s_random.Next().ToString(),
    ContentEncoding = "utf-8",
    ContentType = "application/json",
    ComponentName = "thermostat1",
};
await _deviceClient.SendEventAsync(message, cancellationToken);
```

#### Using convention aware API:

```csharp
// Send telemetry "workingSet" under component "thermostat1".
long workingSet = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;
using var telemtryMessage = new TelemetryMessage("thermostat1")
{
    MessageId = Guid.NewGuid().ToString(),
    Telemetry = { ["workingSet"] = workingSet },
};

await _deviceClient.SendTelemetryAsync(telemtryMessage, cancellationToken);
```

## Commands

### Respond to no-component command:

#### Using non-convention aware API:

```csharp
// Subscribe and respond to command "reboot".
await _deviceClient.SetMethodHandlerAsync(
    "reboot",
    async (methodRequest, userContext) =>
    {
        try
        {
            int delay = JsonConvert.DeserializeObject<int>(methodRequest.DataAsJson);
            await Task.Delay(TimeSpan.FromSeconds(delay));
            
            // Application code ...

            return new MethodResponse(CommonClientResponseCodes.OK);
        }
        catch (JsonReaderException ex)
        {
            return new MethodResponse(CommonClientResponseCodes.BadRequest);
        }
    },
    null,
    cancellationToken);
```

#### Using convention aware API:

```csharp
// Subscribe and respond to command "reboot".
await _deviceClient.SubscribeToCommandsAsync(
    async (commandRequest, userContext) =>
    {
        // This API does not support setting command-level callbacks.
        // For this reason we'll switch through the command name returned and handle each root-level command.
        switch (commandRequest.CommandName)
        {
            case "reboot":
                try
                {
                    int delay = commandRequest.GetData<int>();
                    await Task.Delay(delay * 1000);

                    // Application code ...

                    return new CommandResponse(CommonClientResponseCodes.OK);
                }
                catch (JsonReaderException ex)
                {
                    return new CommandResponse(CommonClientResponseCodes.BadRequest);
                }

            default:
                _logger.LogWarning($"Received a command request that isn't" +
                    $" implemented - command name = {commandRequest.CommandName}");

                return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
        }
    },
    null,
    cancellationToken);
```

### Respond to component-level commands:

#### Using non-convention aware API:

```csharp
// Subscribe and respond to command "getMaxMinReport" under component "thermostat1".
// The method that the application subscribes to is in the format {componentName}*{commandName}.
await _deviceClient.SetMethodHandlerAsync(
    "thermostat1*getMaxMinReport",
    async (commandRequest, userContext) =>
    {
        try
        {
            DateTimeOffset sinceInUtc = JsonConvert.DeserializeObject<DateTimeOffset>(request.DataAsJson);
            
            // Application code ...
            Report report = GetMaxMinReport(sinceInUtc);

            return new MethodResponse(
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(report)),
                CommonClientResponseCodes.OK);
        }
        catch (JsonReaderException ex)
        {
            return new MethodResponse(CommonClientResponseCodes.BadRequest);
        }
    },
    null,
    cancellationToken);
```

#### Using convention aware API:

```csharp
// Subscribe and respond to command "getMaxMinReport" under component "thermostat1".
await _deviceClient.(
    async () =>
    {
        // This API does not support setting command-level callbacks.
        // For this reason we'll first switch through the component name returned and handle each component-level command.
        // For the "default" case, we'll first check if the component name is null.
        // If null, then this would be a root-level command request, so we'll switch through each root-level command.
        // If not null, then this is a component-level command that has not been implemented.

        // Switch through CommandRequest.ComponentName to handle all component-level commands.
        switch (commandRequest.ComponentName)
        {
            case "thermostat1":
                // For each component, switch through CommandRequest.CommandName to handle the specific component-level command.
                switch (commandRequest.CommandName)
                {
                    case "getMaxMinReport":
                        try
                        {
                            DateTimeOffset sinceInUtc = commandRequest.GetData<DateTimeOffset>();
                            
                            // Application code ...
                            Report report = GetMaxMinReport(sinceInUtc);

                            return Task.FromResult(new CommandResponse(report, CommonClientResponseCodes.OK));
                        }
                        catch (JsonReaderException ex)
                        {
                            _logger.LogError($"Command input for {commandRequest.CommandName} is invalid: {ex.Message}.");

                            return Task.FromResult(new CommandResponse(CommonClientResponseCodes.BadRequest));
                        }

                    default:
                        _logger.LogWarning($"Received a command request that isn't" +
                            $" implemented - component name = {commandRequest.ComponentName}, command name = {commandRequest.CommandName}");

                        return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
                }

            // For the default case, first check if CommandRequest.ComponentName is null.
            default:
                // If CommandRequest.ComponentName is null, then this is a root-level command request.
                if (commandRequest.ComponentName == null)
                {
                    // Switch through CommandRequest.CommandName to handle all root-level commands.
                    switch (commandRequest.CommandName)
                    {
                        case "reboot":
                            // Application code ...

                        default:
                            _logger.LogWarning($"Received a command request that isn't" +
                                $" implemented - command name = {commandRequest.CommandName}");

                            return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
                    }
                }
                else
                {
                    _logger.LogWarning($"Received a command request that isn't" +
                        $" implemented - component name = {commandRequest.ComponentName}, command name = {commandRequest.CommandName}");

                    return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
                }
        }

    }
);
```

## Properties

### Retrive no-component client properties:

#### Using non-convention aware API:

```csharp
// Retrieve the client's properties.
Twin properties = await _deviceClient.GetTwinAsync(cancellationToken);

// To fetch the value of client reported property "serialNumber"
bool isSerialNumberReported = properties.Properties.Reported.Contains("serialNumber");
if (isSerialNumberReported)
{
    string serialNumberReported = properties.Properties.Reported["serialNumber"];
}

// To fetch the value of service requested "targetTemperature" value
bool isTargetTemperatureUpdateRequested = properties.Properties.Desired.Contains("targetTemperature");
if (isTargetTemperatureUpdateRequested)
{
    double targetTemperatureUpdateRequest = properties.Properties.Desired["targetTemperature"];
}
```

#### Using convention aware API:

```csharp
// Retrieve the client's properties.
 ClientProperties properties = await _deviceClient.GetClientPropertiesAsync(cancellationToken);

// To fetch the value of client reported property "serialNumber"
bool isSerialNumberReported = properties.TryGetValue("serialNumber", out string serialNumberReported)


// To fetch the value of service requested "targetTemperature" value
bool isTargetTemperatureUpdateRequested = properties.Writable.TryGetValue("targetTemperature", out double targetTemperatureUpdateRequest);
```

### Retrive component-level client properties:

#### Using non-convention aware API:

```csharp
// Retrieve the client's properties.
Twin properties = await _deviceClient.GetTwinAsync(cancellationToken);

// To fetch the value of client reported property "serialNumber" under component "thermostat1"
bool isSerialNumberReported = properties.Properties.Reported.Contains("thermostat1")
    && ((JObject)properties.Properties.Reported["thermostat1"]).TryGetValue("serialNumber", out JToken serialNumberJToken);

if (isSerialNumberReported)
{
    string serialNumberReported = serialNumberJToken.ToObject<string>()
}

// To fetch the value of service requested "targetTemperature" value under component "thermostat1"
bool isTargetTemperatureUpdateRequested = properties.Properties.Desired.Contains("thermostat1")
    && ((JObject)properties.Properties.Desired["thermostat1"]).TryGetValue("targetTemperature", out JToken targetTemperatureUpdateRequestJToken);
    
if (isTargetTemperatureUpdateRequested)
{
    double targetTemperatureUpdateRequest = targetTemperatureUpdateRequestJToken.ToObject<double>()
}
```

#### Using convention aware API:

```csharp
// Retrieve the client's properties.
 ClientProperties properties = await _deviceClient.GetClientPropertiesAsync(cancellationToken);

// To fetch the value of client reported property "serialNumber" under component "thermostat1"
bool isSerialNumberReported = properties.TryGetValue("thermostat1", "serialNumber", out string serialNumberReported)


// To fetch the value of service requested "targetTemperature" value under component "thermostat1"
bool isTargetTemperatureUpdateRequested = properties.Writable.TryGetValue("thermostat1", "targetTemperature", out double targetTemperatureUpdateRequest);
```

### Update no-component property:

#### Using non-convention aware API:

```csharp
```

#### Using convention aware API:

```csharp
```

### Update component-level properties:

#### Using non-convention aware API:

```csharp
```

#### Using convention aware API:

```csharp
```

### Respond to no-component property update requests:

#### Using non-convention aware API:

```csharp
```

#### Using convention aware API:

```csharp
```

### Respond to component-level property update requests:

#### Using non-convention aware API:

```csharp
```

#### Using convention aware API:

```csharp
```

# IoT Plug And Play device samples

These samples demonstrate how a device that follows the [IoT Plug and Play conventions][pnp-convention] interacts with IoT Hub or IoT Central, to:

- Send telemetry.
- Update client properties and be notified of service requested property update requests.
- Respond to command invocation.

The samples demonstrate two scenarios:

- An IoT Plug and Play device that implements the [Thermostat][d-thermostat] model. This model has a single interface that defines telemetry, properties and commands.
- An IoT Plug and Play device that implements the [Temperature controller][d-temperature-controller] model. This model uses multiple components:
  - The top-level interface defines telemetry, properties and commands.
  - The model includes two [Thermostat][thermostat-model] components, and a [device information][d-device-info] component.

> NOTE: These samples are only meant to demonstrate the usage of Plug and Play APIs. If you are looking for a good device sample to get started with, please see the [device reconnection sample][device-reconnection-sample]. It shows how to connect a device, handle disconnect events, cases to handle when making calls, and when to re-initialize the `DeviceClient`.

[pnp-convention]: https://docs.microsoft.com/azure/iot-pnp/concepts-convention
[d-thermostat]: ./Thermostat
[d-temperature-controller]: ./TemperatureController
[thermostat-model]: /iot-hub/Samples/device/convention-based-samples/Thermostat/Models/Thermostat.json
[d-device-info]: https://devicemodels.azure.com/dtmi/azure/devicemanagement/deviceinformation-1.json
[thermostat-hub-qs]: https://docs.microsoft.com/azure/iot-pnp/quickstart-connect-device?pivots=programming-language-csharp
[temp-controller-hub-tutorial]: https://docs.microsoft.com/azure/iot-pnp/tutorial-multiple-components?pivots=programming-language-csharp
[temp-controller-central-tutorial]: https://docs.microsoft.com/azure/iot-central/core/tutorial-connect-device?pivots=programming-language-csharp
[device-reconnection-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/DeviceReconnectionSample
