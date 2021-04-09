## Plug and Play convention compatible APIs

### Properties

```csharp
/// <summary>
/// Retrieve the device properties.
/// </summary>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
/// <returns>The device properties.</returns>
public Task<Properties> GetPropertiesAsync(CancellationToken cancellationToken = default);

/// <summary>
/// Update a single property.
/// </summary>
/// <param name="propertyName">Property name.</param>
/// <param name="propertyValue">Property value.</param>
/// <param name="propertyConvention">A convention handler that defines serializer to use for the properties.</param>
/// <param name="componentName">The component name this property belongs to.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task UpdatePropertyAsync(string propertyName, object propertyValue, PropertyConvention propertyConvention, string componentName = default, CancellationToken cancellationToken = default);

/// <summary>
/// Update a collection of properties.
/// </summary>
/// <param name="properties">Reported properties to push.</param>
/// <param name="propertyConvention">A convention handler that defines serializer to use for the properties.</param>
/// <param name="componentName">The component name this property belongs to.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task UpdatePropertiesAsync(IDictionary<string, object> properties, PropertyConvention propertyConvention, string componentName = default, CancellationToken cancellationToken = default);

/// <summary>
/// Update a writable property.
/// </summary>
/// <param name="propertyName">Property name.</param>
/// <param name="writablePropertyResponse">The writable properyt response to push.</param>
/// <param name="componentName">The component name this property belongs to.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task UpdateWritablePropertyAsync(string propertyName, WritablePropertyResponse writablePropertyResponse, string componentName = default, CancellationToken cancellationToken = default);

/// <summary>
/// Update properties.
/// </summary>
/// <param name="propertyPatch">Reported properties to push.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task UpdatePropertiesAsync(PropertyCollection propertyPatch, CancellationToken cancellationToken = default);

/// <summary>
/// Sets the global listener for Writable properties
/// </summary>
/// <param name="callback">The global call back to handle all writable property updates.</param>
/// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task SubscribeToWritablePropertyEventAsync(Func<PropertyCollection, object, Task> callback, object userContext, CancellationToken cancellationToken = default)
```

<details>
<summary>Properties</summary>

```csharp
/// <summary>
/// A container for properties.
/// </summary>
public class Properties
{
    private const string VersionName = "$version";
    private readonly IDictionary<string, object> _readOnlyProperties = new Dictionary<string, object>();

    /// <summary>
    /// Initializes a new instance of <see cref="Properties"/>
    /// </summary>
    internal Properties()
    {
        Writable = new PropertyCollection();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Properties"/> with the specified collections
    /// </summary>
    /// <param name="writablePropertyCollection">A collection of writable properties returned from IoT Hub</param>
    /// <param name="readOnlyPropertyCollection">A collection of read-only properties returned from IoT Hub</param>
    internal Properties(PropertyCollection writablePropertyCollection, IDictionary<string, object> readOnlyPropertyCollection)
    {
        Writable = writablePropertyCollection;
        _readOnlyProperties = readOnlyPropertyCollection;
    }

    /// <summary>
    ///
    /// </summary>
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
    /// Determines whether the specified property is present
    /// </summary>
    /// <param name="propertyName">The property to locate</param>
    /// <returns>true if the specified property is present; otherwise, false</returns>
    public bool Contains(string propertyName)
    {
        return _readOnlyProperties.TryGetValue(propertyName, out _);
    }

    /// <summary>
    ///
    /// </summary>
    public long Version => _readOnlyProperties.TryGetValue(VersionName, out object version)
        ? (long)version
        : default;

    /// <summary>
    /// Converts a <see cref="TwinProperties"/> collection to a properties collection
    /// </summary>
    /// <param name="twinProperties">The TwinProperties object to convert</param>
    /// <returns></returns>
    internal static Properties FromTwinProperties(TwinProperties twinProperties)
    {
        if (twinProperties == null)
        {
            throw new ArgumentNullException(nameof(twinProperties));
        }

        var writablePropertyCollection = new PropertyCollection();
        foreach (KeyValuePair<string, object> property in twinProperties.Desired)
        {
            writablePropertyCollection.AddPropertyToCollection(property.Key, property.Value);
        }
        // The version information is not accessible via the enumerator, so assign it separately.
        writablePropertyCollection.AddPropertyToCollection(VersionName, twinProperties.Desired.Version);

        var propertyCollection = new Dictionary<string, object>();
        foreach (KeyValuePair<string, object> property in twinProperties.Reported)
        {
            propertyCollection.Add(property.Key, property.Value);
        }
        // The version information is not accessible via the enumerator, so assign it separately.
        propertyCollection.Add(VersionName, twinProperties.Reported.Version);

        return new Properties(writablePropertyCollection, propertyCollection);
    }
}
```
</details>

<details>
<summary>PropertyCollection</summary>

```csharp
/// <summary>
///
/// </summary>
public class PropertyCollection : IEnumerable<object>
{
    private const string VersionName = "$version";

    private readonly string _propertyJson;
    private readonly IDictionary<string, object> _propertiesList = new Dictionary<string, object>();

    /// <summary>
    ///
    /// </summary>
    /// <param name="propertyJson"></param>
    public PropertyCollection(string propertyJson)
    {
        _propertyJson = propertyJson;
    }

    internal PropertyCollection()
    {
    }

    internal PropertyCollection(IDictionary<string, object> propertiesList)
    {
        _propertiesList = propertiesList;
    }

    /// <summary>
    /// Determines whether the specified property is present
    /// </summary>
    /// <param name="propertyName">The property to locate</param>
    /// <returns>true if the specified property is present; otherwise, false</returns>
    public bool Contains(string propertyName)
    {
        return _propertiesList.TryGetValue(propertyName, out _);
    }

    /// <summary>
    ///
    /// </summary>
    public long Version => _propertiesList.TryGetValue(VersionName, out object version)
        ? (long)version
        : default;

    /// <summary>
    ///
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public dynamic this[string propertyName]
    {
        get
        {
            return _propertiesList[propertyName];
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public string ToJson()
    {
        return _propertiesList.ToString();
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public IEnumerator<object> GetEnumerator()
    {
        foreach (object property in _propertiesList)
        {
            yield return property;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    internal string GetPropertyJson()
    {
        return _propertyJson;
    }

    internal void AddPropertyToCollection(string propertyKey, object propertyValue)
    {
        _propertiesList.Add(propertyKey, propertyValue);
    }

    /// <summary>
    /// Converts a <see cref="TwinCollection"/> collection to a properties collection
    /// </summary>
    /// <param name="twinCollection">The TwinCollection object to convert</param>
    /// <returns></returns>
    internal static PropertyCollection FromTwinCollection(TwinCollection twinCollection)
    {
        if (twinCollection == null)
        {
            throw new ArgumentNullException(nameof(twinCollection));
        }

        var writablePropertyCollection = new PropertyCollection();
        foreach (KeyValuePair<string, object> property in twinCollection)
        {
            writablePropertyCollection.AddPropertyToCollection(property.Key, property.Value);
        }
        // The version information is not accessible via the enumerator, so assign it separately.
        writablePropertyCollection.AddPropertyToCollection(VersionName, twinCollection.Version);

        return writablePropertyCollection;
    }
}
```
</details>


<details>
<summary>WritablePropertyResponse</summary>

```csharp
public class WritablePropertyResponse
{
    private readonly PropertyConvention _propertyConvention;

    /// <summary>
    /// Convenience constructor for specifying only the property value.
    /// </summary>
    /// <param name="propertyValue">The unserialized property value.</param>
    /// <param name="propertyConvention"></param>
    public WritablePropertyResponse(object propertyValue, PropertyConvention propertyConvention)
    {
        // null checks

        Value = propertyValue;
        _propertyConvention = propertyConvention;
    }

    /// <summary>
    /// Convenience constructor for specifying the properties.
    /// </summary>
    /// <param name="propertyValue">The unserialized property value.</param>
    /// <param name="ackCode">The acknowledgement code, usually an HTTP Status Code e.g. 200, 400.</param>
    /// <param name="ackVersion">The acknowledgement version, as supplied in the property update request.</param>
    /// <param name="ackDescription">The acknowledgement description, an optional, human-readable message about the result of the property update.</param>
    /// <param name="propertyConvention"></param>
    public WritablePropertyResponse(object propertyValue, int ackCode, long ackVersion, string ackDescription = default, PropertyConvention propertyConvention = default)
    {
        // null checks

        Value = propertyValue;
        AckCode = ackCode;
        AckVersion = ackVersion;
        AckDescription = ackDescription;

        _propertyConvention = propertyConvention;
    }

    /// <summary>
    /// The unserialized property value.
    /// </summary>
    [JsonIgnore]
    public object Value { get; private set; }

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

    /// <summary>
    /// The serialized property value.
    /// </summary>
    [JsonProperty("value")]
    public JRaw ValueAsJson => new JRaw(_propertyConvention.SerializeToString(Value));
}
```
</details>

<details>
<summary>PropertyConvention</summary>

```csharp
public class PropertyConvention
{
/// <summary>
///
/// </summary>
public static readonly PropertyConvention Instance = new PropertyConvention();

/// <summary>
///
/// </summary>
public static string ComponentIdentifierKey => "__t";

/// <summary>
///
/// </summary>
public static string ComponentIdentifierValue => "c";

/// <summary>
///
/// </summary>
/// <param name="objectToSerialize"></param>
/// <returns></returns>
public virtual string SerializeToString(object objectToSerialize)
{
    return JsonConvert.SerializeObject(objectToSerialize);
}

/// <summary>
///
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="stringToDeserialize"></param>
/// <returns></returns>
public virtual T DeserializeToType<T>(string stringToDeserialize)
{
    return JsonConvert.DeserializeObject<T>(stringToDeserialize);
}
```
</details>


### Telemetry

```csharp
/// <summary>
/// Send telemetry using the specified message.
/// </summary>
/// <remarks>
/// Use the <see cref="Message.Message(object, TelemetryConvention)"/> constructor to pass in the formatted telemetry payload and the <see cref="TelemetryConvention"/>.
/// If your telemetry payload does not have any specific serialization requirements you can pass in <see cref="TelemetryConvention.Instance"/>.
/// If the telemetry is originating from a component, set the component name to <see cref="Message.ComponentName"/>.
/// </remarks>
/// <param name="telemetryMessage">The telemetry message.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
/// <returns></returns>
public Task SendTelemetryAsync(Message telemetryMessage, CancellationToken cancellationToken = default)
```

<details>
<summary>TelemetryConvention</summary>

```csharp
public class TelemetryConvention
{
    /// <summary>
    /// The content type for a plug and play compatible telemetry message.
    /// </summary>
    private const string ApplicationJson = "application/json";

    /// <summary>
    ///
    /// </summary>
    public static readonly TelemetryConvention Instance = new TelemetryConvention();

    /// <summary>
    ///
    /// </summary>
    public Encoding ContentEncoding { get; set; } = Encoding.UTF8;

    /// <summary>
    ///
    /// </summary>
    public string ContentType { get; set; } = ApplicationJson;

    /// <summary>
    /// Format a plug and play compatible telemetry message payload.
    /// </summary>
    /// <param name="telemetryName">The name of the telemetry, as defined in the DTDL interface. Must be 64 characters or less. For more details see
    /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#telemetry"/>.</param>
    /// <param name="telemetryValue">The unserialized telemetry payload, in the format defined in the DTDL interface.</param>
    /// <returns>A plug and play compatible telemetry message payload, which can be sent to IoT Hub.</returns>
    public static IDictionary<string, object> FormatTelemetryPayload(string telemetryName, object telemetryValue)
    {
        return new Dictionary<string, object> { { telemetryName, telemetryValue } };
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="objectToSerialize"></param>
    /// <returns></returns>
    public virtual string SerializeToString(object objectToSerialize)
    {
        return JsonConvert.SerializeObject(objectToSerialize);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="contentPayload"></param>
    /// <returns></returns>
    public virtual byte[] EncodeStringToByteArray(string contentPayload)
    {
        return ContentEncoding.GetBytes(contentPayload);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="objectToSendWithConvention"></param>
    /// <returns></returns>
    public virtual byte[] GetObjectBytes(object objectToSendWithConvention)
    {
        return EncodeStringToByteArray(SerializeToString(objectToSendWithConvention));
    }
}
```
</details>

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