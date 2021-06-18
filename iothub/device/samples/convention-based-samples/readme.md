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

# IoT Plug And Play (PnP) device/module APIs

Device(s)/module(s) connecting to IoT Hub that announce their DTDL model ID during initialization can now perform convention-based operations. One such convention supported is [IoT Plug and Play][pnp-convention].

These devices/modules can now use the native PnP APIs in the Azure IoT device SDKs to directly exchange messages with an IoT Hub, without having to manually format these messages to follow the PnP convention.

## Table of Contents

- [Client initialization](#client-initialization)
  - [Announce model ID during client initialization (same as in latest `master` release)](#announce-model-ID-during-client-initialization-same-as-in-latest-master-release)
  - [Define the serialization and encoding convention that the client follows (newly introduced in `preview`)](#define-the-serialization-and-encoding-convention-that-the-client-follows-newly-introduced-in-preview)
- [Terms used](#terms-used)
- [Comparison of API calls - non-convention-aware APIs (old) vs convention-aware APIs (new):](#comparison-of-api-calls---non-convention-aware-apis-old-vs-convention-aware-apis-new)
  - [Telemetry](#telemetry)
  - [Commands](#commands)
  - [Properties](#properties)
- [IoT Plug And Play device samples](#iot-plug-and-play-device-samples)

## Client initialization

### Announce model ID during client initialization (same as in latest [`master`][latest-master-release] release)

```csharp
var options = new ClientOptions
{
    ModelId = ModelId,
};

DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt, options);
```

### Define the serialization and encoding convention that the client follows (newly introduced in [`preview`][latest-preview-release])

```csharp
// Specify a custom System.Text.Json serialization and Utf8 encoding based PayloadConvention to be used.
// If not specified, the library defaults to a convention that uses Newtonsoft.Json-based serializer and Utf8-based encoder.
var options = new ClientOptions(SystemTextJsonPayloadConvention.Instance)
{
    ModelId = ModelId,
};

DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt, options);
```

## Terms used:
Telemetry, commands, properties and components can all be defined in the contents section of the main interface of a DTDL v2 model. Components enable interfaces to be composed of other interfaces.

In DTDL v2, a component cannot contain another component. The maximum depth of components is 1.

- Top-level telemetry/commands/properties
  - These refer to the telemetry, commands and properties that are defined directly in the contents section of the main interface of a DTDL v2 model. In case of a model with no components, the main interface refers to the default component.
  - When working with this category of telemetry, commands and properties, you do not need to specify any component name.
- Component-level telemetry/commands/properties
  - These refer to the telemetry, commands and properties that are defined in the contents section of an interface, which itself is defined as a component within the main interface.
  - When working with this category of telemetry, commands and properties, you need to specify the name of the component that these contents belong to.

## Comparison of API calls - non-convention-aware APIs (old) vs convention-aware APIs (new):

The following section provides a comparison between the older non-convention-aware APIs (as per latest [`master`][latest-master-release] release) and the newly introduced convention-aware APIs (as per latest [`preview`][latest-preview-release] release).

## Telemetry

### Send top-level telemetry:

#### Using non-convention-aware API (old):

```csharp
// Send telemetry "temperature".
double temperature = 70.0D;
var telemetry = new Dictionary<string, object>
{
    ["temperature"] = temperature,
};

using var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemetry)))
{
    MessageId = s_random.Next().ToString(),
    ContentEncoding = "utf-8",
    ContentType = "application/json",
};
await _deviceClient.SendEventAsync(message, cancellationToken);
```

#### Using convention-aware API (new):

```csharp
// Send telemetry "temperature".
double temperature = 70.0D;
using var telemetryMessage = new TelemetryMessage
{
    MessageId = Guid.NewGuid().ToString(),
    Telemetry = { ["temperature"] = temperature },
};

await _deviceClient.SendTelemetryAsync(telemetryMessage, cancellationToken);
```

### Send component-level telemetry:

#### Using non-convention-aware API (old):

```csharp
// Send telemetry "temperature" under component "thermostat1".
double temperature = 70.0D;
var telemetry = new Dictionary<string, object>()
{
    ["temperature"] = temperature,
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

#### Using convention-aware API (new):

```csharp
// Send telemetry "temperature" under component "thermostat1".
double temperature = 70.0D;
using var telemtryMessage = new TelemetryMessage("thermostat1")
{
    MessageId = Guid.NewGuid().ToString(),
    Telemetry = { ["temperature"] = temperature },
};

await _deviceClient.SendTelemetryAsync(telemtryMessage, cancellationToken);
```

## Commands

### Respond to top-level commands:

#### Using non-convention-aware API (old):

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
        catch (JsonReaderException)
        {
            return new MethodResponse(CommonClientResponseCodes.BadRequest);
        }
    },
    null,
    cancellationToken);
```

#### Using convention-aware API (new):

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
                catch (JsonReaderException)
                {
                    return new CommandResponse(CommonClientResponseCodes.BadRequest);
                }
        }
        else
        {
            return new CommandResponse(CommonClientResponseCodes.NotFound);
        }
    },
    null,
    cancellationToken);
```

### Respond to component-level commands:

#### Using non-convention-aware API (old):

```csharp
// Subscribe and respond to command "getMaxMinReport" under component "thermostat1".
// The method that the application subscribes to is in the format {componentName}*{commandName}.
await _deviceClient.SetMethodHandlerAsync(
    "thermostat1*getMaxMinReport",
    (commandRequest, userContext) =>
    {
        try
        {
            DateTimeOffset sinceInUtc = JsonConvert.DeserializeObject<DateTimeOffset>(commandRequest.DataAsJson);

            // Application code ...
            Report report = GetMaxMinReport(sinceInUtc);

            return Task.FromResult(
                new MethodResponse(
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(report)),
                    CommonClientResponseCodes.OK));
        }
        catch (JsonReaderException)
        {
            return Task.FromResult(new MethodResponse(CommonClientResponseCodes.BadRequest));
        }
    },
    null,
    cancellationToken);
```

#### Using convention-aware API (new):

```csharp
// Subscribe and respond to command "getMaxMinReport" under component "thermostat1".
await _deviceClient.SubscribeToCommandsAsync(
    (commandRequest, userContext) =>
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
            catch (JsonReaderException)
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

#### Using non-convention-aware API (old):

```csharp
// Retrieve the client's properties.
Twin properties = await _deviceClient.GetTwinAsync(cancellationToken);

// To fetch the value of client reported property "serialNumber".
bool isSerialNumberReported = properties.Properties.Reported.Contains("serialNumber");
if (isSerialNumberReported)
{
    string serialNumberReported = properties.Properties.Reported["serialNumber"];
}

// To fetch the value of service requested "targetTemperature" value.
bool isTargetTemperatureUpdateRequested = properties.Properties.Desired.Contains("targetTemperature");
if (isTargetTemperatureUpdateRequested)
{
    double targetTemperatureUpdateRequest = properties.Properties.Desired["targetTemperature"];
}
```

#### Using convention-aware API (new):

```csharp
// Retrieve the client's properties.
 ClientProperties properties = await _deviceClient.GetClientPropertiesAsync(cancellationToken);

// To fetch the value of client reported property "serialNumber".
bool isSerialNumberReported = properties.TryGetValue("serialNumber", out string serialNumberReported);


// To fetch the value of service requested "targetTemperature" value.
bool isTargetTemperatureUpdateRequested = properties.Writable.TryGetValue("targetTemperature", out double targetTemperatureUpdateRequest);
```

### Retrive component-level client properties:

#### Using non-convention-aware API (old):

```csharp
// Retrieve the client's properties.
Twin properties = await _deviceClient.GetTwinAsync(cancellationToken);

// To fetch the value of client reported property "serialNumber" under component "thermostat1".
JToken serialNumberJToken = null;
bool isSerialNumberReported = properties.Properties.Reported.Contains("thermostat1")
    && ((JObject)properties.Properties.Reported["thermostat1"]).TryGetValue("serialNumber", out serialNumberJToken);

if (isSerialNumberReported)
{
    string serialNumberReported = serialNumberJToken?.ToObject<string>();
}

// To fetch the value of service requested "targetTemperature" value under component "thermostat1".
JToken targetTemperatureUpdateRequestJToken = null;
bool isTargetTemperatureUpdateRequested = properties.Properties.Desired.Contains("thermostat1")
    && ((JObject)properties.Properties.Desired["thermostat1"]).TryGetValue("targetTemperature", out targetTemperatureUpdateRequestJToken);

if (isTargetTemperatureUpdateRequested)
{
    double targetTemperatureUpdateRequest = (double)(targetTemperatureUpdateRequestJToken?.ToObject<double>());
}
```

#### Using convention-aware API (new):

```csharp
// Retrieve the client's properties.
 ClientProperties properties = await _deviceClient.GetClientPropertiesAsync(cancellationToken);

// To fetch the value of client reported property "serialNumber" under component "thermostat1".
bool isSerialNumberReported = properties.TryGetValue("thermostat1", "serialNumber", out string serialNumberReported);


// To fetch the value of service requested "targetTemperature" value under component "thermostat1".
bool isTargetTemperatureUpdateRequested = properties.Writable.TryGetValue("thermostat1", "targetTemperature", out double targetTemperatureUpdateRequest);
```

### Update top-level property:

#### Using non-convention-aware API (old):

```csharp
// Update the property "serialNumber".
var propertiesToBeUpdated = new TwinCollection
{
    ["serialNumber"] = "SR-1234",
};
await _deviceClient.UpdateReportedPropertiesAsync(propertiesToBeUpdated, cancellationToken);
```

#### Using convention-aware API (new):

```csharp
// Update the property "serialNumber".
var propertiesToBeUpdated = new ClientPropertyCollection
{
    ["serialNumber"] = "SR-1234",
};
ClientPropertiesUpdateResponse updateResponse = await _deviceClient
    .UpdateClientPropertiesAsync(propertiesToBeUpdated, cancellationToken);
long updatedVersion = updateResponse.Version;
```

### Update component-level properties:

#### Using non-convention-aware API (old):

```csharp
// Update the property "serialNumber" under component "thermostat1".
// When calling the UpdateReportedPropertiesAsync API the component-level property update requests must
// include the {"__t": "c"} marker to indicate that the element refers to a component.
var thermostatProperties = new TwinCollection
{
    ["__t"] = "c",
    ["serialNumber"] = "SR-1234",
};
var propertiesToBeUpdated = new TwinCollection
{
    ["thermostat1"] = thermostatProperties
};
await _deviceClient.UpdateReportedPropertiesAsync(propertiesToBeUpdated, cancellationToken);
```

#### Using convention-aware API (new):

```csharp
// Update the property "serialNumber" under component "thermostat1".
var propertiesToBeUpdated = new ClientPropertyCollection();
propertiesToBeUpdated.AddComponentProperty("thermostat1", "serialNumber", "SR-1234");

ClientPropertiesUpdateResponse updateResponse = await _deviceClient
    .UpdateClientPropertiesAsync(propertiesToBeUpdated, cancellationToken);
long updatedVersion = updateResponse.Version;
```

### Respond to top-level property update requests:

#### Using non-convention-aware API (old):

```csharp
// Subscribe and respond to event for writable property "targetTemperature".
// This writable property update response should follow the format specified here: https://docs.microsoft.com/azure/iot-pnp/concepts-convention#writable-properties.
await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(
    async (desired, userContext) =>
    {
        if (desired.Contains("targetTemperature"))
        {
            double targetTemperature = desired["targetTemperature"];

            var targetTemperatureUpdateResponse = new TwinCollection
            {
                ["value"] = targetTemperature,
                ["ac"] = CommonClientResponseCodes.OK,
                ["av"] = desired.Version,
                ["ad"] = "The operation completed successfully."
            };
            var propertiesToBeUpdated = new TwinCollection()
            {
                ["targetTemperature"] = targetTemperatureUpdateResponse,
            };

            await _deviceClient.UpdateReportedPropertiesAsync(propertiesToBeUpdated, cancellationToken);
        }
    },
    null,
    cancellationToken);
```

#### Using convention-aware API (new):

```csharp
// Subscribe and respond to event for writable property "targetTemperature".
// This writable property update response should follow the format specified here: https://docs.microsoft.com/azure/iot-pnp/concepts-convention#writable-properties.
await _deviceClient.SubscribeToWritablePropertiesEventAsync(
    async (writableProperties, userContext) =>
    {
        if (writableProperties.TryGetValue("targetTemperature", out double targetTemperature))
        {
            IWritablePropertyResponse writableResponse = _deviceClient
                .PayloadConvention
                .PayloadSerializer
                .CreateWritablePropertyResponse(targetTemperature, CommonClientResponseCodes.OK, writableProperties.Version, "The operation completed successfully.");

            var propertiesToBeUpdated = new ClientPropertyCollection();
            propertiesToBeUpdated.AddRootProperty("targetTemperature", writableResponse);

            ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(propertiesToBeUpdated, cancellationToken);
        }
    },
    null,
    cancellationToken);
```

### Respond to component-level property update requests:

#### Using non-convention-aware API (old):

```csharp
// Subscribe and respond to event for writable property "targetTemperature"
// under component "thermostat1".
// This writable property update response should follow the format specified here: https://docs.microsoft.com/azure/iot-pnp/concepts-convention#writable-properties.
// When calling the UpdateReportedPropertiesAsync API the component-level property update requests must
// include the {"__t": "c"} marker to indicate that the element refers to a component.
await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(
    async (desired, userContext) =>
    {
        if (desired.Contains("thermostat1")
            && ((JObject)desired["thermostat1"])
                .TryGetValue("targetTemperature", out JToken targetTemperatureRequested))
        {
            double targetTemperature = targetTemperatureRequested
                .ToObject<double>();
            
            var targetTemperatureUpdateResponse = new TwinCollection
            {
                ["value"] = targetTemperature,
                ["ac"] = CommonClientResponseCodes.OK,
                ["av"] = desired.Version,
                ["ad"] = "The operation completed successfully."
            };
            var thermostatProperties = new TwinCollection()
            {
                ["__t"] = "c",
                ["targetTemperature"] = targetTemperatureUpdateResponse,
            };
            var propertiesToBeUpdated = new TwinCollection()
            {
                ["thermostat1"] = thermostatProperties,
            };

            await _deviceClient.UpdateReportedPropertiesAsync(propertiesToBeUpdated, cancellationToken);
        }
    },
    null,
    cancellationToken);
```

#### Using convention-aware API (new):

```csharp
// Subscribe and respond to event for writable property "targetTemperature"
// under component "thermostat1".
// This writable property update response should follow the format specified here: https://docs.microsoft.com/azure/iot-pnp/concepts-convention#writable-properties.
await _deviceClient.SubscribeToWritablePropertiesEventAsync(
    async (writableProperties, userContext) =>
    {
        if (writableProperties.TryGetValue("thermostat1", "targetTemperature", out double targetTemperature))
        {
            IWritablePropertyResponse writableResponse = _deviceClient
                .PayloadConvention
                .PayloadSerializer
                .CreateWritablePropertyResponse(targetTemperature, CommonClientResponseCodes.OK, writableProperties.Version, "The operation completed successfully.");

            var propertiesToBeUpdated = new ClientPropertyCollection();
            propertiesToBeUpdated.AddComponentProperty("thermostat1", "targetTemperature", writableResponse);

            ClientPropertiesUpdateResponse updateResponse = await _deviceClient.UpdateClientPropertiesAsync(propertiesToBeUpdated, cancellationToken);
        }
    },
    null,
    cancellationToken);
```

# IoT Plug And Play device samples

These samples demonstrate how a device that follows the [IoT Plug and Play conventions][pnp-convention] interacts with IoT Hub or IoT Central, to:

- Send telemetry.
- Update client properties and be notified of service requested property update requests.
- Respond to command invocation.

The samples demonstrate two scenarios:

- An IoT Plug and Play device that implements the [Thermostat][d-thermostat] model. This model has a single interface (the default component) that defines telemetry, properties and commands.
- An IoT Plug and Play device that implements the [Temperature controller][d-temperature-controller] model. This model defines multiple interfaces:
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
[latest-master-release]: https://github.com/Azure/azure-iot-sdk-csharp/tree/2021-05-13
[latest-preview-release]: https://github.com/Azure/azure-iot-sdk-csharp/tree/preview_2021-6-8
