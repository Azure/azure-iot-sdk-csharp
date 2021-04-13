## Plug and Play convention compatible APIs

#### Common

```csharp
public interface ISerializer {
    string ContentType { get; }
    T DeserializeToType<T>(string stringToDeserialize);
    string SerializeToString(object objectToSerialize);
}

public interface IContentEncoder {
    Encoding ContentEncoding { get; }
    byte[] EncodeStringToByteArray(string contentPayload);
}

public interface IPayloadConvention {
    IContentEncoder PayloadEncoder { get; }
    ISerializer PayloadSerializer { get; }
    byte[] GetObjectBytes(object objectToSendWithConvention);
}

public class JsonContentSerializer : ISerializer {
    public static readonly JsonContentSerializer Instance;
    public JsonContentSerializer();
    public string ContentType { get; }
    public T DeserializeToType<T>(string stringToDeserialize);
    public string SerializeToString(object objectToSerialize);
}

public class Utf8ContentEncoder : IContentEncoder {
    public static readonly Utf8ContentEncoder Instance;
    public Utf8ContentEncoder();
    public Encoding ContentEncoding { get; }
    public byte[] EncodeStringToByteArray(string contentPayload);
}

public class DefaultPayloadConvention : IPayloadConvention {
    public static readonly DefaultPayloadConvention Instance;
    public DefaultPayloadConvention();
    public IContentEncoder PayloadEncoder { get; }
    public ISerializer PayloadSerializer { get; }
    public byte[] GetObjectBytes(object objectToSendWithConvention);
}
```

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
    public WritablePropertyResponse(object propertyValue, IPayloadConvention payloadConvention=null);
    public WritablePropertyResponse(object propertyValue, int ackCode, long ackVersion, string ackDescription=null, IPayloadConvention payloadConvention=null);
    public int AckCode { get; set; }
    public string AckDescription { get; set; }
    public long AckVersion { get; set; }
    public object Value { get; private set; }
    public JRaw ValueAsJson { get; }
}

public class PropertyConvention : DefaultPayloadConvention {
    public static readonly new PropertyConvention Instance;
    public PropertyConvention();
}

public static class PropertyConventionHelper {
    public static PropertyCollection CreatePropertiesPatch(IDictionary<string, object> properties, string componentName=null, IPayloadConvention payloadConvention=null);
    public static PropertyCollection CreatePropertyPatch(string propertyName, object propertyValue, string componentName=null, IPayloadConvention payloadConvention=null);
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
public Message(object messagePayload, IPayloadConvention payloadConvention=null);

public static class TelemetryConventionHelper {
    public static IDictionary<string, object> FormatTelemetryPayload(string telemetryName, object telemetryValue);
}
```

### Commands

```csharp
/// <summary>
/// Set the global command callback handler.
/// </summary>
/// <param name="callback">A method implementation that will handle the incoming command.</param>
/// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
/// <param name="payloadConvention">A convention handler that defines the content encoding and serializer to use for commands.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task SubscribeToCommandsAsync(Func<CommandRequest, object, Task<CommandResponse>> callback, object userContext, IPayloadConvention payloadConvention = default, CancellationToken cancellationToken = default);
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
    public CommandResponse(object result, int status, IPayloadConvention payloadConvention=null);
    public string ResultAsJson { get; }
    public int Status { get; private set; }
}
```
