# Plug and Play convention compatible APIs

## Common

```diff
public class ClientOptions {
+    public ClientOptions(PayloadConvention payloadConvention = null);
+    public PayloadConvention PayloadConvention { get; }
}

public class DeviceClient : IDisposable {
+    public PayloadConvention PayloadConvention { get; }
}

public class ModuleClient : IDisposable {
+    public PayloadConvention PayloadConvention { get; }
}
```

```csharp
public abstract class PayloadConvention {
    protected PayloadConvention();
    public abstract PayloadEncoder PayloadEncoder { get; }
    public abstract PayloadSerializer PayloadSerializer { get; }
    public virtual byte[] GetObjectBytes(object objectToSendWithConvention);
}

public abstract class PayloadEncoder {
    protected PayloadEncoder();
    public abstract Encoding ContentEncoding { get; }
    public abstract byte[] EncodeStringToByteArray(string contentPayload);
}

public abstract class PayloadSerializer {
    protected PayloadSerializer();
    public abstract string ContentType { get; }
    public abstract string SerializeToString(object objectToSerialize);
    public abstract T DeserializeToType<T>(string stringToDeserialize);
    public abstract T ConvertFromJsonObject<T>(object jsonObjectToConvert);
    public abstract IWritablePropertyAcknowledgementValue CreateWritablePropertyAcknowledgementValue(object value, int statusCode, long version, string description = null);
}

public sealed class DefaultPayloadConvention : PayloadConvention {
    public static DefaultPayloadConvention Instance { get; }
    public override PayloadEncoder PayloadEncoder { get; }
    public override PayloadSerializer PayloadSerializer { get; }
}

public class Utf8PayloadEncoder : PayloadEncoder {
    public static Utf8PayloadEncoder Instance { get; }
    public override Encoding ContentEncoding { get; }
    public override byte[] EncodeStringToByteArray(string contentPayload);
}

public class NewtonsoftJsonPayloadSerializer : PayloadSerializer {
    public static NewtonsoftJsonPayloadSerializer Instance { get; }
    public override string ContentType { get; }
    public override string SerializeToString(object objectToSerialize);
    public override T DeserializeToType<T>(string stringToDeserialize);
    public override T ConvertFromJsonObject<T>(object objectToConvert);
    public override IWritablePropertyAcknowledgementValue CreateWritablePropertyAcknowledgementValue(object value, int statusCode, long version, string description = null);
}

public abstract class PayloadCollection : IEnumerable, IEnumerable<KeyValuePair<string, object>> {
    protected PayloadCollection();
    public PayloadConvention Convention { get; internal set; }
    public virtual object this[string key] { get; set; }
    public virtual void Add(string key, object value);
    public void ClearCollection();
    public bool Contains(string key);
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator();
    public virtual byte[] GetPayloadObjectBytes();
    public virtual string GetSerializedString();
    IEnumerator System.Collections.IEnumerable.GetEnumerator();
    public bool TryGetValue<T>(string key, out T value);
}

public static class ConventionBasedConstants {
    public const char ComponentLevelCommandSeparator = '*';
    public const string AckCodePropertyName = "ac";
    public const string AckDescriptionPropertyName = "ad";
    public const string AckVersionPropertyName = "av";
    public const string ComponentIdentifierKey = "__t";
    public const string ComponentIdentifierValue = "c";
    public const string ValuePropertyName = "value";
}

public class CommonClientResponseCodes {
    public const int Accepted = 202;
    public const int BadRequest = 400;
    public const int NotFound = 404;
    public const int OK = 200;
    public CommonClientResponseCodes();
}
```

## Properties

```csharp
/// <summary>
/// Retrieve the device properties.
/// </summary>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
/// <returns>The device properties.</returns>
public Task<ClientProperties> GetClientPropertiesAsync(CancellationToken cancellationToken = default);

/// <summary>
/// Update properties.
/// </summary>
/// <param name="propertyCollection">Reported properties to push.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
/// <returns>The response containing the operation request Id and updated version no.</returns>
public Task<ClientPropertiesUpdateResponse> UpdateClientPropertiesAsync(ClientPropertyCollection propertyCollection, CancellationToken cancellationToken = default);

/// <summary>
/// Sets the listener for writable property update events.
/// </summary>
/// <param name="callback">The callback to handle all writable property updates for the client.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task SubscribeToWritablePropertyUpdateRequestsAsync(Func<WritableClientPropertyCollection, Task> callback, CancellationToken cancellationToken = default);
```

### All related types

```csharp
public class ClientProperties {
    public ClientPropertyCollection ReportedFromClient { get; }
    public WritableClientPropertyCollection WritablePropertyRequests { get; }
}

public class ClientPropertyCollection : IEnumerable, IEnumerable<KeyValuePair<string, object>> {
    public ClientPropertyCollection();
    public long Version { get; private set; }
    public virtual object this[string key] { get; set; }
    public virtual void Add(string key, object value);
    public void AddRootProperty(string propertyName, object propertyValue);
    public void AddComponentProperty(string componentName, string propertyName, object propertyValue);
    public bool Contains(string propertyName);
    public bool Contains(string componentName, string propertyName);
    public bool TryGetValue<T>(string propertyName, out T propertyValue);
    public bool TryGetValue<T>(string componentName, string propertyName, out T propertyValue);
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator();
    IEnumerator System.Collections.IEnumerable.GetEnumerator();
}

public class WritableClientPropertyCollection : IEnumerable, IEnumerable<KeyValuePair<string, object>> {
    public long Version { get; private set; }
    public bool Contains(string propertyName);
    public bool Contains(string componentName, string propertyName);
    public bool TryGetValue<T>(string propertyName, out T propertyValue);
    public bool TryGetValue<T>(string componentName, string propertyName, out T propertyValue);
    public bool TryGetWritableClientProperty(string propertyName, out WritableClientProperty propertyValue);
    public bool TryGetWritableClientProperty(string componentName, string propertyName, out WritableClientProperty propertyValue);
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator();
    IEnumerator System.Collections.IEnumerable.GetEnumerator();
}

public class WritableClientProperty {
    public object Value { get; internal set; }
    public long Version { get; internal set; }
    public IWritablePropertyAcknowledgementValue AcknowledgeWith(int statusCode, string description = null);
    public bool TryGetValue<T>(out T propertyValue);
}

public interface IWritablePropertyAcknowledgementValue {
    object Value { get; set; }
    int AckCode { get; set; }
    long AckVersion { get; set; }
    string AckDescription { get; set; }
}

public sealed class NewtonsoftJsonWritablePropertyAcknowledgementValue : IWritablePropertyAcknowledgementValue {
    public NewtonsoftJsonWritablePropertyAcknowledgementValue(object propertyValue, int ackCode, long ackVersion, string ackDescription = null);
    public object Value { get; set; }
    public int AckCode { get; set; }
    public long AckVersion { get; set; }
    public string AckDescription { get; set; }
}

public class ClientPropertiesUpdateResponse {
    public string RequestId { get; internal set; }
    public long Version { get; internal set; }
}
```

## Telemetry

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
public Task SendTelemetryAsync(TelemetryMessage telemetryMessage, CancellationToken cancellationToken = default);
```

### All related types

```csharp
public class TelemetryCollection : IEnumerable, IEnumerable<KeyValuePair<string, object>> {
    public TelemetryCollection();
    public object this[string telemetryKey] { get; set; }
    public void Add(IDictionary<string, object> telemetryValues);
    public void Add(string telemetryName, object telemetryValue);
    public bool Contains(string key);
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator();
    IEnumerator System.Collections.IEnumerable.GetEnumerator();
    public bool TryGetValue<T>(string telemetryKey, out T telemetryValue);
    public void ClearCollection();
}

public sealed class TelemetryMessage : MessageBase {
    public TelemetryMessage(string componentName = null);
    public TelemetryCollection Telemetry { get; set; }
    public override Stream GetBodyStream();
}
```

## Commands

```csharp
/// <summary>
/// Sets the listener for command invocation requests.
/// </summary>
/// <param name="callback">The callback to handle all incoming commands for the client.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task SubscribeToCommandsAsync(Func<CommandRequest, Task<CommandResponse>> callback, CancellationToken cancellationToken = default);
```

### All related types

```csharp
public class CommandRequest {
    public string CommandName { get; }
    public string ComponentName { get; }
    public T GetPayload<T>();
    public ReadOnlyCollection<byte> GetPayloadAsBytes();
    public string GetPayloadAsString();
}

public sealed class CommandResponse {
    public CommandResponse(int status);
    public CommandResponse(int status, object payload);
    public object Payload { get; set; }
    public int Status { get; set; }
}
```
