## Plug and Play convention compatible APIs

### Properties

```csharp
/// <summary>
/// Retrieve the device properties.
/// </summary>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
/// <returns>The device properties.</returns>
public Task<Properties> GetPropertiesAsync(CancellationToken cancellationToken);

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
public Task SubscribeToWritablePropertyEventAsync(Func<PropertyCollection, object, Task> callback, object userContext, CancellationToken cancellationToken = default);
```
#### All related types

```csharp
public class Properties : IEnumerable, IEnumerable<object> {
    public object this[string propertyName] { get; }
    public long Version { get; }
    public PropertyCollection Writable { get; private set; }
    public bool Contains(string propertyName);
    public IEnumerator<object> GetEnumerator();
    IEnumerator System.Collections.IEnumerable.GetEnumerator();
}

public class PropertyCollection : IEnumerable, IEnumerable<object> {
    public object this[string propertyName] { get; }
    public long Version { get; }
    public bool Contains(string propertyName);
    public IEnumerator<object> GetEnumerator();
    IEnumerator System.Collections.IEnumerable.GetEnumerator();
    public string ToJson();
}

public class WritablePropertyResponse {
    public WritablePropertyResponse(object propertyValue, PropertyConvention propertyConvention=null);
    public WritablePropertyResponse(object propertyValue, int ackCode, long ackVersion, string ackDescription=null, PropertyConvention propertyConvention=null);
    public int AckCode { get; set; }
    public string AckDescription { get; set; }
    public long AckVersion { get; set; }
    public object Value { get; private set; }
    public JRaw ValueAsJson { get; }
}

public class ObjectSerializer {
    public static readonly ObjectSerializer Instance;
    public ObjectSerializer();
    public string ContentType { get; set; }
    public virtual T DeserializeToType<T>(string stringToDeserialize);
    public virtual string SerializeToString(object objectToSerialize);
}

public class PropertyConvention {
    public static readonly PropertyConvention Instance;
    public PropertyConvention();
    public Encoding ContentEncoding { get; }
    public string ContentType { get; }
    public ObjectSerializer PayloadSerializer { get; set; }
    public static PropertyCollection CreatePropertiesPatch(IDictionary<string, object> properties, string componentName=null, PropertyConvention propertyConvention=null);
    public static PropertyCollection CreatePropertyPatch(string propertyName, object propertyValue, string componentName=null, PropertyConvention propertyConvention=null);
    public static PropertyCollection CreateWritablePropertyPatch(string propertyName, WritablePropertyResponse writablePropertyResponse, string componentName=null);
}
```

### Telemetry

```csharp
/// <summary>
/// Send telemetry using the specified message.
/// </summary>
/// <remarks>
/// Use the <see cref="Message(object, TelemetryConvention)"/> constructor to pass in the formatted telemetry payload and an optional
/// <see cref="TelemetryConvention"/> that specifies your payload serialization and encoding rules.
/// If the telemetry is originating from a component, set the component name to <see cref="Message.ComponentName"/>.
/// </remarks>
/// <param name="telemetryMessage">The telemetry message.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
/// <returns></returns>
public Task SendTelemetryAsync(Message telemetryMessage, CancellationToken cancellationToken = default);
```
#### All related types

```csharp
public Message(object messagePayload, TelemetryConvention telemetryConvention=null);

public class TelemetryConvention {
    public static readonly TelemetryConvention Instance;
    public TelemetryConvention();
    public Encoding ContentEncoding { get; set; }
    public ObjectSerializer PayloadSerializer { get; set; }
    public virtual byte[] EncodeStringToByteArray(string contentPayload);
    public static IDictionary<string, object> FormatTelemetryPayload(string telemetryName, object telemetryValue);
    public virtual byte[] GetObjectBytes(object objectToSendWithConvention);
}
```

### Commands

```csharp
/// <summary>
/// Set the global command callback handler.
/// </summary>
/// <param name="callback">A method implementation that will handle the incoming command.</param>
/// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
/// <param name="commandConvention">A convention handler that defines the content encoding and serializer to use for commands.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task SubscribeToCommandsAsync(Func<CommandRequest, object, Task<CommandResponse>> callback, object userContext, CommandConvention commandConvention = default, CancellationToken cancellationToken = default);
```
#### All related types

```csharp
public sealed class CommandRequest {
    public string ComponentName { get; private set; }
    public string DataAsJson { get; }
    public string Name { get; private set; }
    public T GetData<T>();
}

public sealed class CommandResponse {
    public CommandResponse(int status);
    public CommandResponse(object result, int status, CommandConvention commandConvention=null);
    public string ResultAsJson { get; }
    public int Status { get; private set; }
}

public class CommandConvention {
    public static readonly CommandConvention Instance;
    public CommandConvention();
    public Encoding ContentEncoding { get; }
    public string ContentType { get; }
    public ObjectSerializer PayloadSerializer { get; set; }
}
```
