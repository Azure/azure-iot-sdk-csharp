## Plug and Play convention compatible APIs

#### Common

```csharp

public abstract class IPayloadConvention {
    protected IPayloadConvention();
    public abstract IContentEncoder PayloadEncoder { get; }
    public abstract ISerializer PayloadSerializer { get; }
    public virtual byte[] GetObjectBytes(object objectToSendWithConvention);
}

public abstract class IContentEncoder {
    protected IContentEncoder();
    public abstract Encoding ContentEncoding { get; }
    public abstract byte[] EncodeStringToByteArray(string contentPayload);
}

public abstract class ISerializer {
    protected ISerializer();
    public abstract string ContentType { get; }
    public abstract bool CheckWritablePropertyResponseType(object typeToCheck);
    public abstract T ConvertFromObject<T>(object objectToConvert);
    public abstract T DeserializeToType<T>(string stringToDeserialize);
    public abstract string SerializeToString(object objectToSerialize);
}

public sealed class DefaultPayloadConvention : IPayloadConvention {
    public static readonly DefaultPayloadConvention Instance;
    public DefaultPayloadConvention();
    public override IContentEncoder PayloadEncoder { get; }
    public override ISerializer PayloadSerializer { get; }
}

public class Utf8ContentEncoder : IContentEncoder {
    public static readonly Utf8ContentEncoder Instance;
    public Utf8ContentEncoder();
    public override Encoding ContentEncoding { get; }
    public override byte[] EncodeStringToByteArray(string contentPayload);
}

public class JsonContentSerializer : ISerializer {
    public static readonly JsonContentSerializer Instance;
    public JsonContentSerializer();
    public override string ContentType { get; }
    public override bool CheckWritablePropertyResponseType(object typeToCheck);
    public override T ConvertFromObject<T>(object objectToConvert);
    public override T DeserializeToType<T>(string stringToDeserialize);
    public override string SerializeToString(object objectToSerialize);
}

public abstract class PayloadCollection : IEnumerable, IEnumerable<object> {
    protected PayloadCollection(IPayloadConvention payloadConvention = null);
    public IDictionary<string, object> Collection { get; private set; }
    public IPayloadConvention Convention { get; private set; }
    public object this[string key] { get; set; }
    public IEnumerator<object> GetEnumerator();
    public virtual byte[] GetPayloadObjectBytes();
    public virtual string GetSerailizedString();
    public virtual T GetValue<T>(string key);
    IEnumerator System.Collections.IEnumerable.GetEnumerator();
}
```

### Properties

```csharp
/// <summary>
/// Retrieve the device properties.
/// </summary>
/// <param name="payloadConvention">A convention handler that defines the content encoding and serializer to use for commands.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
/// <returns>The device properties.</returns>
public Task<Properties> GetPropertiesAsync(IPayloadConvention payloadConvention = null, CancellationToken cancellationToken = default(CancellationToken));

/// <summary>
/// Update properties.
/// </summary>
/// <param name="propertyCollection">Reported properties to push.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task UpdatePropertiesAsync(PropertyCollection propertyCollection, CancellationToken cancellationToken = default(CancellationToken));

/// <summary>
/// Sets the global listener for Writable properties
/// </summary>
/// <param name="callback">The global call back to handle all writable property updates.</param>
/// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
/// <param name="payloadConvention">A convention handler that defines the content encoding and serializer to use for commands.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task SubscribeToWritablePropertyEventAsync(Func<PropertyCollection, object, Task> callback, object userContext, IPayloadConvention payloadConvention = null, CancellationToken cancellationToken = default(CancellationToken));
```

#### All related types

```csharp
public class Properties : IEnumerable, IEnumerable<object> {
    public object this[string key] { get; }
    public long Version { get; }
    public PropertyCollection Writable { get; private set; }
    public bool Contains(string propertyName);
    public T Get<T>(string propertyKey);
    public IEnumerator<object> GetEnumerator();
    IEnumerator System.Collections.IEnumerable.GetEnumerator();
}

public class PropertyCollection : PayloadCollection {
    public PropertyCollection(IPayloadConvention payloadConvention = null);
    public long Version { get; }
    public void Add(IDictionary<string, object> properties, string componentName = null);
    public void Add(string propertyName, object propertyValue, string componentName = null);
    public void AddOrUpdate(IDictionary<string, object> properties, string componentName = null);
    public void AddOrUpdate(string propertyName, object propertyValue, string componentName = null);
    public bool Contains(string propertyName);
}

public abstract class WritablePropertyBase {
    protected const string AckCodePropertyName = "ac";
    protected const string AckDescriptionPropertyName = "ad";
    protected const string AckVersionPropertyName = "av";
    protected const string ValuePropertyName = "value";
    public WritablePropertyBase(object propertyValue, int ackCode, long ackVersion, string ackDescription = null);
    public abstract int AckCode { get; set; }
    public abstract string AckDescription { get; set; }
    public abstract long AckVersion { get; set; }
    public abstract object Value { get; set; }
}

public sealed class WritablePropertyResponse : WritablePropertyBase {
    public WritablePropertyResponse(object propertyValue, int ackCode, long ackVersion, string ackDescription = null);
    public override int AckCode { get; set; }
    public override string AckDescription { get; set; }
    public override long AckVersion { get; set; }
    public override object Value { get; set; }
}
```

### Telemetry

```csharp
/// <summary>
/// Send telemetry using the specified message.
/// </summary>
/// <remarks>
/// Use the <see cref="TelemetryMessage(string, TelemetryCollection)"/> constructor to pass in the optional
/// <see cref="TelemetryCollection"/> that specifies your payload and serialization and encoding rules.
/// </remarks>
/// <param name="telemetryMessage">The telemetry message.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
/// <returns></returns>
public Task SendTelemetryAsync(TelemetryMessage telemetryMessage, CancellationToken cancellationToken = default(CancellationToken));
```
#### All related types

```csharp
public class TelemetryCollection : PayloadCollection {
    public TelemetryCollection(IPayloadConvention payloadConvention = null);
    public void Add(string telemetryName, object telemetryValue);
    public void AddOrUpdate(string telemetryName, object telemetryValue);
}

public class TelemetryMessage : Message {
    public TelemetryMessage(string componentName = null, TelemetryCollection telemetryCollection = null);
    public new string ContentEncoding { get; }
    public new string ContentType { get; }
    public TelemetryCollection Telemetry { get; set; }
    public override Stream GetBodyStream();
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
public Task SubscribeToCommandsAsync(Func<CommandRequest, object, Task<CommandResponse>> callback, object userContext, IPayloadConvention payloadConvention = null, CancellationToken cancellationToken = default(CancellationToken));
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
    public CommandResponse(object result, int status, IPayloadConvention payloadConvention = null);
    public string ResultAsJson { get; }
    public int Status { get; private set; }
}
```


### Other changes to Microsoft.Azure.Devices.Client

API listing follows standard diff formatting. Lines preceded by a '+' are additions and a '-' indicates removal.

``` diff
 {
     namespace Microsoft.Azure.Devices.Client {
-        public sealed class Message : IDisposable, IReadOnlyIndicator {
+        public class Message : IDisposable, IReadOnlyIndicator {
-            public Stream BodyStream { get; }
+            public Stream BodyStream { get; protected set; }
-            public string ComponentName { get; set; }
+            public virtual string ComponentName { get; set; }
-            public string ContentEncoding { get; set; }
+            public virtual string ContentEncoding { get; set; }
-            public string ContentType { get; set; }
+            public virtual string ContentType { get; set; }
+            protected virtual void Dispose(bool disposing);
+            protected void DisposeBodyStream();
-            public Stream GetBodyStream();
+            public virtual Stream GetBodyStream();
         }
     }
 }
```