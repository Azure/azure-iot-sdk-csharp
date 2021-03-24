## Plug and Play convention compatible APIs

### Properties

```csharp
/// <summary>
/// Update a single property.
/// 
/// Property under the root component:
/// "reported": {
///     "temperature": 21.3
/// }
/// 
/// Property under a specified component:
/// "reported": {
///     "thermostat1": {
///         "__t": "c",
///         "temperature": 21.3
///     }
/// }
/// </summary>
/// <param name="propertyName">The name of the twin property to update.</param>
/// <param name="propertyValue">The value of the twin property to update.</param>
/// <param name="componentName">The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task UpdatePropertyAsync(string propertyName, dynamic propertyValue, string componentName = default, CancellationToken cancellationToken = default);

/// <summary>
/// Update a batch of properties.
/// 
/// Properties under the root component:
/// "reported": {
///     "temperature": 21.3,
///     "humudity": 60
/// }
/// 
/// Properties under a specified component:
/// "reported": {
///     "thermostat1": {
///         "__t": "c",
///         "temperature": 21.3,
///         "humudity": 60
///     }
/// }
/// </summary>
/// <param name="properties">The twin properties and values to update.</param>
/// <param name="componentName">The name of the component in which the properties are defined. Can be null for properties defined under the root interface.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task UpdatePropertiesAsync(IDictionary<string, dynamic> properties, string componentName = default, CancellationToken cancellationToken = default);

/// <summary>
/// Respond to a writable property request received from the service.
/// 
/// Property under the root component:
/// "reported": {
///     "targetTemperature": {
///         "value": 21.3,
///         "ac": 200,
///         "av": 3,
///         "ad": "complete"
///     }
/// }
/// 
/// Property under a specified component:
/// "reported": {
///   "thermostat1": {
///     "__t": "c",
///     "targetTemperature": {
///       "value": 23,
///       "ac": 200,
///       "av": 3,
///       "ad": "complete"
///     }
///   }
/// }
/// </summary>
/// <param name="propertyName">The name of the twin property to update.</param>
/// <param name="propertyValue">The value of the twin property to update.</param>
/// <param name="componentName">The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task UpdatePropertyAsync(string propertyName, WritableProperty propertyValue, string componentName = default, CancellationToken cancellationToken = default);

/// <summary>
/// Subscribe to writable property event for all properties under any component.
///
/// Payload received for the root component:
/// "desired" :
/// {
///   "targetTemperature" : 21.3,
///   "targetHumidity" : 80
/// },
/// "$version" : 3
///
/// Payload received for a specific component:
/// "desired": {
///   "thermostat1": {
///     "__t": "c",
///     "targetTemperature": 21.3,
///     "targetHumidity": 80
///   }
/// },
/// "$version" : 3
/// 
/// </summary>
/// <param name="propertyActionAsTwinCollection">The action to be taken when a writeable property event is received.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public void SubscribeToWritablePropertyEvent(Action<TwinCollection> propertyActionAsTwinCollection, CancellationToken cancellationToken = default);

/// <summary>
/// Retrieve the device properties.
/// </summary>
/// <returns>The device properties.</returns>
public Task<Twin> GetPropertiesAsync(CancellationToken cancellationToken = default);
```

<details>
<summary>WritableProperty</summary>

```csharp
/// <summary>
/// Empty constructor.
/// </summary>
public WritablePropertyResponse() { }

/// <summary>
/// Convenience constructor for specifying the properties.
/// </summary>
/// <param name="propertyValue">The unserialized property value.</param>
/// <param name="ackCode">The acknowledgement code, usually an HTTP Status Code e.g. 200, 400.</param>
/// <param name="ackVersion">The acknowledgement version, as supplied in the property update request.</param>
/// <param name="ackDescription">The acknowledgement description, an optional, human-readable message about the result of the property update.</param>
public WritablePropertyResponse(object propertyValue, int ackCode, long ackVersion, string ackDescription = null)
{
    PropertyValue = propertyValue;
    AckCode = ackCode;
    AckVersion = ackVersion;
    AckDescription = ackDescription;
}

/// <summary>
/// The unserialized property value.
/// </summary>
[JsonProperty("value")]
public object PropertyValue { get; set; }

/// <summary>
/// The acknowledgement code, usually an HTTP Status Code e.g. 200, 400.
/// </summary>
[JsonProperty("ac")]
public int AckCode { get; set; }

/// <summary>
/// The acknowledgement version, as supplied in the property update request.
/// </summary>
[JsonProperty("av")]
public long AckVersion { get; set; }

/// <summary>
/// The acknowledgement description, an optional, human-readable message about the result of the property update.
/// </summary>
[JsonProperty("ad", DefaultValueHandling = DefaultValueHandling.Ignore)]
public string AckDescription { get; set; }
```
</details>


### Telemetry

```csharp
/// <summary>
/// Send a single instance of telemetry.
/// </summary>
/// <param name="telemetryName">The name of the telemetry, as defined in the DTDL interface.</param>
/// <param name="telemetryValue">The telemetry payload, in the format defined in the DTDL interface.</param>
/// <param name="componentName">The name of the component in which the telemetry is defined. Can be null for telemetry defined under the root interface.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task SendTelemetryAsync(string telemetryName, dynamic telemetryValue, string componentName = default, CancellationToken cancellationToken = default);

/// <summary>
/// Send a batched instance of telemetry.
/// </summary>
/// <param name="telemetryPairs">The name and value telemetry pairs, as defined in the DTDL interface.</param>
/// <param name="componentName">The name of the component in which the telemetry is defined. Can be null for telemetry defined under the root interface.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task SendTelemetryAsync(IDictionary<string, dynamic> telemetryPairs, string componentName = default, CancellationToken cancellationToken = default);

/// <summary>
/// Send a single instance of telemetry, formatted as per DTDL specifications:
/// - the payload should be in the format: { "<telemetry_name>" : "<telemetry_value>" }
/// - the property Message.ContentEncoding should be set to "utf-8".
/// - the property Message.ContentType should be set to "application/json".
/// - if the telemetry is defined under a component, the property Message.ComponentName should be set to "<component_name>".
/// </summary>
/// <param name="telemetryMessage">The telemetry message to be sent.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task SendTelemetryAsync(Message telemetryMessage, CancellationToken cancellationToken = default);
```

### Commands

```csharp
/// <summary>
/// Subscribe to command event.
/// </summary>
/// <param name="commandName">The name of the command, as defined in the DTDL interface.</param>
/// <param name="commandCallback">The action to be taken when a command request is received.</param>
/// <param name="userContext">Context object that is passed to the event callback.</param>
/// <param name="componentName">The name of the component in which the command is defined. Can be null for command defined under the root interface.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task SubscribeToCommandEvent(string commandName, Func<CommandRequest, object, Task<CommandResponse>> commandCallback, object userContext = default, string componentName = default, CancellationToken cancellationToken = default);
```

<details>
<summary>CommandRequest</summary>

```csharp
public sealed class CommandRequest : MethodRequest
{
    internal CommandRequest(string commandName) : this (commandName, null)
    {
    }

    internal CommandRequest(string commandName, string componentName) : this (commandName, componentName, null)
    {

    }

    internal CommandRequest(string commandName, string componentName, object data) : base (commandName, ConvertToByteArray(data))
    {
        ComponentName = componentName;
    }

    public readonly string ComponentName { get; private set; }

    private static byte[] ConvertToByteArray(object result)
    {
    }
}
```
</details>

<details>
<summary>CommandResponse</summary>

```c#
public class CommandResponse : MethodResponse
{
    public CommandResponse(object result, int status) : base (ConvertToByteArray(result), status)
    {
    }

    public CommandResponse(int status) : base (status)
    {
    }

    private static byte[] ConvertToByteArray(object result)
    {
    }
}
```
</details>