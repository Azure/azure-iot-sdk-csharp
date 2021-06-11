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

## Contents

- TO-DO

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

### Define the serialization and encoding convention that the client follows (newly introduced)

```csharp
// Specify a custom System.Text.Json serialization and Utf8 encoding based PayloadConvention to be used.
// If not specified the library defaults to a convention that uses Newtonsoft.Json-based serializer and Utf8-based encoder.
var options = new ClientOptions(SystemTextJsonPayloadConvention.Instance)
{
    ModelId = ModelId,
};

DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt, options);
```

## Comparision of API calls - non-convention aware APIs (old) vs convention-aware APIs (newly introduced):

### Terms used:
- Top-level telemetry/ commands/ preoperties - TO-DO
- Component-level telemetry/ commands/ properties -  TO-DO

## Telemetry

### Send top-level telemetry:

#### Using non-convention aware API (old):

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

#### Using convention aware API (new):

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

#### Using non-convention aware API (old):

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

#### Using convention aware API (new):

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

### Respond to top-level commands:

#### Using non-convention aware API (old):

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

#### Using convention aware API (new):

```csharp
// Subscribe and respond to command "reboot".
await _deviceClient.SubscribeToCommandsAsync(
    async (commandRequest, userContext) =>
    {
        // This API does not support setting command-level callbacks.
        // For this reason we'll need to inspect the commandRequest.CommandName for the request command and perform the actions accordingly.
        // Refer to the ThermostatSample.cs for a complete sample implementation.

        if (commandRequest.CommandName == "reboot")
        {
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
        }
        else
        {
            return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
        }
    },
    null,
    cancellationToken);
```

### Respond to component-level commands:

#### Using non-convention aware API (old):

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

#### Using convention aware API (new):

```csharp
// Subscribe and respond to command "getMaxMinReport" under component "thermostat1".
await _deviceClient.SubscribeToCommandsAsync(
    async (commandRequest, userContext) =>
    {
        // This API does not support setting command-level callbacks.
        // For this reason we'll need to inspect both commandRequest.ComponentName and commandRequest.CommandName, and perform the actions accordingly.
        // Refer to the TemperatureControllerSample.cs for a complete sample implementation.

        if (commandRequest.ComponentName == "thermostat1"
            && commandRequest.CommandName == "getMaxMinReport")
        {
            try
            {
                DateTimeOffset sinceInUtc = commandRequest.GetData<DateTimeOffset>();
                
                // Application code ...
                Report report = GetMaxMinReport(sinceInUtc);

                return Task.FromResult(new CommandResponse(report, CommonClientResponseCodes.OK));
            }
            catch (JsonReaderException ex)
            {
                return Task.FromResult(new CommandResponse(CommonClientResponseCodes.BadRequest));
            }
        }
        else
        {
            return Task.FromResult(new CommandResponse(CommonClientResponseCodes.NotFound));
        }
    }
);
```

## Properties

### Retrive top-level client properties:

#### Using non-convention aware API (old):

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

#### Using convention aware API (new):

```csharp
// Retrieve the client's properties.
 ClientProperties properties = await _deviceClient.GetClientPropertiesAsync(cancellationToken);

// To fetch the value of client reported property "serialNumber"
bool isSerialNumberReported = properties.TryGetValue("serialNumber", out string serialNumberReported);


// To fetch the value of service requested "targetTemperature" value
bool isTargetTemperatureUpdateRequested = properties.Writable.TryGetValue("targetTemperature", out double targetTemperatureUpdateRequest);
```

### Retrive component-level client properties:

#### Using non-convention aware API (old):

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

#### Using convention aware API (new):

```csharp
// Retrieve the client's properties.
 ClientProperties properties = await _deviceClient.GetClientPropertiesAsync(cancellationToken);

// To fetch the value of client reported property "serialNumber" under component "thermostat1"
bool isSerialNumberReported = properties.TryGetValue("thermostat1", "serialNumber", out string serialNumberReported);


// To fetch the value of service requested "targetTemperature" value under component "thermostat1"
bool isTargetTemperatureUpdateRequested = properties.Writable.TryGetValue("thermostat1", "targetTemperature", out double targetTemperatureUpdateRequest);
```

### Update top-level property:

#### Using non-convention aware API (old):

```csharp
```

#### Using convention aware API (new):

```csharp
```

### Update component-level properties:

#### Using non-convention aware API (old):

```csharp
```

#### Using convention aware API (new):

```csharp
```

### Respond to top-level property update requests:

#### Using non-convention aware API (old):

```csharp
```

#### Using convention aware API (new):

```csharp
```

### Respond to component-level property update requests:

#### Using non-convention aware API (old):

```csharp
```

#### Using convention aware API (new):

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
