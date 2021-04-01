## Plug and Play convention compatible APIs

### Properties

```csharp
/// <summary>
/// Retrieve the device properties.
/// </summary>
/// <returns>The device properties.</returns>
public Task<Properties> GetPropertiesAsync(CancellationToken cancellationToken = default);

/// <summary>
/// Update a single property.
/// </summary>
/// <param name="propertyName">Property name.</param>
/// <param name="propertyValue">Property value.</param>
/// <param name="componentName">The component name this property belongs to.</param>
/// <param name="conventionHandler">A convention handler that defines the content encoding and serializer to use for the properties.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task UpdatePropertyAsync(string propertyName, object propertyValue, string componentName = default, IConventionHandler conventionHandler = default, CancellationToken cancellationToken = default);

/// <summary>
/// Update a collection of properties.
/// </summary>
/// <param name="properties">Reported properties to push.</param>
/// <param name="componentName">The component name these properties belong to.</param>
/// <param name="conventionHandler">A convention handler that defines the content encoding and serializer to use for the properties.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task UpdatePropertiesAsync(IDictionary<string, object> properties, string componentName = default, IConventionHandler conventionHandler = default, CancellationToken cancellationToken = default);

/// <summary>
/// Sets the global listener for Writable properties
/// </summary>
/// <param name="callback">The global call back to handle all writable property updates.</param>
/// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
/// <param name="cancellationToken">A cancellation token.</param>
public Task SubscribeToWritablePropertyEvent(Action<Properties, object> callback, object userContext, CancellationToken cancellationToken = default);

/// <summary>
/// Sets the global listener for Writable properties
/// </summary>
/// <param name="callback">The global call back to handle all writable property updates.</param>
/// <param name="componentName">The component name this writable property belongs to.</param>
/// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
/// <param name="cancellationToken">A cancellation token.</param>
public Task SubscribeToWritablePropertyEvent(Action<Properties, object> callback, string componentName = default, object userContext = default, CancellationToken cancellationToken = default);
```

<details>
<summary>Properties</summary>

```csharp
/// <summary>
/// A container for properties for your device
/// </summary>
public class Properties
{
    private PropertyCollection _readOnlyProperties;
    /// <summary>
    /// Initializes a new instance of <see cref="Properties"/>
    /// </summary>
    public Properties()
    {
        Writable = new PropertyCollection();
        _readOnlyProperties = new PropertyCollection();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Properties"/> with the specified collections
    /// </summary>
    /// <param name="writablePropertyCollection">A collection of writable properties returned from IoT Hub</param>
    /// <param name="readOnlyPropertyCollection">A collection of read-only properties returned from IoT Hub</param>
    public Properties(PropertyCollection writablePropertyCollection, PropertyCollection readOnlyPropertyCollection)
    {
        Writable = writablePropertyCollection;
        _readOnlyProperties = readOnlyPropertyCollection;
    }

    /// <summary>
    /// Gets and sets the writable properties.
    /// </summary>
    [JsonProperty(PropertyName = "desired", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public PropertyCollection Writable { get; private set; }

    /// <summary>
    /// Get the property from the propeties collection
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public dynamic this[string propertyName]
    {
        get
        {
            return _readOnlyProperties[propertyName];
        }
    }

    /// <summary>
    /// Converts a <see cref="TwinProperties"/> collection to a properties collection
    /// </summary>
    /// <param name="twinProperties">The TwinProperties object to convert</param>
    /// <returns></returns>
    public static Properties FromTwinProperties(TwinProperties twinProperties)
    {
        if (twinProperties == null)
        {
            throw new ArgumentNullException(nameof(twinProperties));
        }

        return new Properties()
        {
            _readOnlyProperties = (PropertyCollection)twinProperties.Reported,
            Writable = (PropertyCollection)twinProperties.Desired
        };
    }
}
```
</details>

<details>
<summary>PropertyCollection</summary>

```csharp
/// <summary>
/// A collection of properties for the device
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1010:Generic interface should also be implemented", Justification = "<Pending>")]
public class PropertyCollection : TwinCollection
{
}
```
</details>


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
/// Sends a single instance of telemetry.
/// </summary>
/// <param name="telemetryName">The name of the telemetry to send.</param>
/// <param name="telemetryValue">The value of the telemetry to send.</param>
/// <param name="conventionHandler">A convention handler that defines the content encoding and serializer to use for the telemetry message.</param>
/// <param name="componentName">The component name this telemetry belongs to.</param>
/// <param name="cancellationToken">A cancellation token.</param>
/// <remarks>
/// This will create a single telemetry message and will not combine multiple calls into one message. Use <seealso cref="SendTelemetryAsync(IDictionary{string, dynamic}, string, IConventionHandler, CancellationToken)"/>. Refer to the documentation for <see cref="IConventionHandler"/> if you want to use a custom serializer.
/// </remarks>
public Task SendTelemetryAsync(string telemetryName, object telemetryValue, string componentName = default, IConventionHandler conventionHandler = default, CancellationToken cancellationToken = default);

/// <summary>
/// Sends a collection of telemetry.
/// </summary>
/// <param name="telemetryDictionary">A dictionary of dynamic objects </param>
/// <param name="componentName">The component name this telemetry belongs to.</param>
/// <param name="conventionHandler">A convention handler that defines the content encoding and serializer to use for the telemetry message.</param>
/// <param name="cancellationToken">A cancellation token.</param>
/// /// <remarks>
/// This will either use the <see cref="DefaultConvention"/> to define the encoding and use the default Json serailzier. Refer to the documentation for <see cref="IConventionHandler"/> if you want to use a custom serializer.
/// </remarks>
public Task SendTelemetryAsync(IDictionary<string, object> telemetryDictionary, string componentName = default, IConventionHandler conventionHandler = default, CancellationToken cancellationToken = default);

/// <summary>
/// Send telemetry using the specified message.
/// </summary>
/// <remarks>
/// Use this method when you need to define custom properties for the message.
/// </remarks>
/// <param name="telemetryMessage">The custom implemented telemetry message</param>
/// <param name="componentName">The component name this telemetry belongs to.</param>
/// <param name="cancellationToken">A cancellation token.</param>
public Task SendTelemetryAsync(Message telemetryMessage, string componentName = default, CancellationToken cancellationToken = default);
```

### Commands

```csharp
/// <summary>
/// Set command callback handler.
/// </summary>
/// <param name="commandName">The name of the command this handler will be used for.</param>
/// <param name="commandCallback">The callback for this command.</param>
/// <param name="componentName">The component name this command belongs to.</param>
/// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
/// <param name="cancellationToken"></param>
/// <remarks>
/// The .NET SDK has a built in dispatcher that handles per command name routing. The SDK will first attempt to find the command by name. If it is found it will execute the callback for that specific command. If there is no entry found in the dispatcher the SDK will fall back to the global command handler <see cref="SetCommandCallbackHandlerAsync(Func{CommandRequest, object, Task{CommandResponse}}, object, CancellationToken)"/>.
/// </remarks>
public Task SetCommandCallbackHandlerAsync(string commandName, Func<CommandRequest, object, Task<CommandResponse>> commandCallback, string componentName = default, object userContext = default, CancellationToken cancellationToken = default);

/// <summary>
/// Set the global command callback handler. This handler will be called when no named handler was found for the command.
/// </summary>
/// <param name="commandCallback">A method implementation that will handle the incoming command.</param>
/// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
/// <remarks>
/// The global command handler will be the fallback handler in the event there is nothing specified in the dispatcher. See the remarks in <see cref="SetCommandCallbackHandlerAsync(Func{CommandRequest, object, Task{CommandResponse}}, object, CancellationToken)"/> for more information.
/// </remarks>
public Task SetCommandCallbackHandlerAsync(Func<CommandRequest, object, Task<CommandResponse>> commandCallback, object userContext = default, CancellationToken cancellationToken = default);
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