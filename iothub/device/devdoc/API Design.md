## Plug and Play convention compatible APIs

#### Common

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
    public abstract T ConvertFromObject<T>(object objectToConvert);
    public abstract IWritablePropertyResponse CreateWritablePropertyResponse(object value, int statusCode, long version, string description = null);
    public abstract T DeserializeToType<T>(string stringToDeserialize);
    public abstract string SerializeToString(object objectToSerialize);
}

public sealed class DefaultPayloadConvention : PayloadConvention {
    public static readonly DefaultPayloadConvention Instance;
    public DefaultPayloadConvention();
    public override PayloadEncoder PayloadEncoder { get; }
    public override PayloadSerializer PayloadSerializer { get; }
}

public class Utf8PayloadEncoder : PayloadEncoder {
    public static readonly Utf8PayloadEncoder Instance;
    public Utf8PayloadEncoder();
    public override Encoding ContentEncoding { get; }
    public override byte[] EncodeStringToByteArray(string contentPayload);
}

public class NewtonsoftJsonPayloadSerializer : PayloadSerializer {
    public static readonly NewtonsoftJsonPayloadSerializer Instance;
    public NewtonsoftJsonPayloadSerializer();
    public override string ContentType { get; }
    public override T ConvertFromObject<T>(object objectToConvert);
    public override IWritablePropertyResponse CreateWritablePropertyResponse(object value, int statusCode, long version, string description = null);
    public override T DeserializeToType<T>(string stringToDeserialize);
    public override string SerializeToString(object objectToSerialize);
}

public abstract class PayloadCollection : IEnumerable, IEnumerable<object> {
    protected PayloadCollection();
    public IDictionary<string, object> Collection { get; private set; }
    public PayloadConvention Convention { get; internal set; }
    public virtual object this[string key] { get; set; }
    public virtual void Add(string key, object value);
    public virtual void AddOrUpdate(string key, object value);
    public IEnumerator<object> GetEnumerator();
    public virtual byte[] GetPayloadObjectBytes();
    public virtual string GetSerializedString();
    IEnumerator System.Collections.IEnumerable.GetEnumerator();
    public virtual bool TryGetValue<T>(string key, out T value);
}

public static class ConventionBasedConstants {
    public const string AckCodePropertyName = "ac";
    public const string AckDescriptionPropertyName = "ad";
    public const string AckVersionPropertyName = "av";
    public const string ComponentIdentifierKey = "__t";
    public const string ComponentIdentifierValue = "c";
    public const string ValuePropertyName = "value";
}
```

### Properties

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
/// Sets the global listener for Writable properties
/// </summary>
/// <param name="callback">The global call back to handle all writable property updates.</param>
/// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
public Task SubscribeToWritablePropertiesEventAsync(Func<ClientPropertyCollection, object, Task> callback, object userContext, CancellationToken cancellationToken = default);
```

#### All related types

```csharp
public class ClientProperties : IEnumerable, IEnumerable<object> {
    public object this[string key] { get; }
    public long Version { get; }
    public ClientPropertyCollection Writable { get; private set; }
    public bool Contains(string propertyName);
    public IEnumerator<object> GetEnumerator();
    IEnumerator System.Collections.IEnumerable.GetEnumerator();
    public bool TryGetValue<T>(string componentName, string propertyName, out T propertyValue);
    public bool TryGetValue<T>(string propertyKey, out T propertyValue);
}

public class ClientPropertyCollection : PayloadCollection {
    public ClientPropertyCollection();
    public long Version { get; private set; }
    public void Add(IDictionary<string, object> properties, string componentName = null);
    public override void Add(string propertyName, object propertyValue);
    public void Add(string propertyName, object propertyValue, string componentName);
    public void AddOrUpdate(IDictionary<string, object> properties, string componentName = null);
    public override void AddOrUpdate(string propertyName, object propertyValue);
    public void AddOrUpdate(string propertyName, object propertyValue, string componentName);
    public bool Contains(string propertyName);
    public virtual bool TryGetValue<T>(string componentName, string propertyName, out T propertyValue);
}

public interface IWritablePropertyResponse {
    int AckCode { get; set; }
    string AckDescription { get; set; }
    long AckVersion { get; set; }
    object Value { get; set; }
}

public sealed class NewtonsoftJsonWritablePropertyResponse : IWritablePropertyResponse {
    public NewtonsoftJsonWritablePropertyResponse(object propertyValue, int ackCode, long ackVersion, string ackDescription = null);
    public int AckCode { get; set; }
    public string AckDescription { get; set; }
    public long AckVersion { get; set; }
    public object Value { get; set; }
}

public class ClientPropertiesUpdateResponse {
    public ClientPropertiesUpdateResponse();
    public string RequestId { get; internal set; }
    public long Version { get; internal set; }
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
public Task SendTelemetryAsync(TelemetryMessage telemetryMessage, CancellationToken cancellationToken = default);
```
#### All related types

```csharp
public class TelemetryCollection : PayloadCollection {
    public TelemetryCollection();
    public override void Add(string telemetryName, object telemetryValue);
    public override void AddOrUpdate(string telemetryName, object telemetryValue);
}

public class TelemetryMessage : Message {
    public TelemetryMessage(string componentName = null);
    public new string ContentEncoding { get; internal set; }
    public new string ContentType { get; internal set; }
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
public Task SubscribeToCommandsAsync(Func<CommandRequest, object, Task<CommandResponse>> callback, object userContext, CancellationToken cancellationToken = default);
```
#### All related types

```csharp
public sealed class CommandRequest {
    public string CommandName { get; private set; }
    public string ComponentName { get; private set; }
    public string DataAsJson { get; }
    public T GetData<T>();
}

public sealed class CommandResponse {
    public CommandResponse(int status);
    public CommandResponse(object result, int status);
    public string ResultAsJson { get; }
    public int Status { get; private set; }
}
```

### Additional changes to Microsoft.Azure.Devices.Client

API listing follows standard diff formatting. Lines preceded by a '+' are additions and a '-' indicates removal.

Includes all public and non-public changes to existing types.

``` diff
 {
     namespace Microsoft.Azure.Devices.Client {
         
        public class ClientOptions {
+            public PayloadConvention PayloadConvention { get; set; }
        }
        
        public class DeviceClient : IDisposable {
+            public PayloadConvention PayloadConvention { get; }
        }

        internal interface IDelegatingHandler : IContinuationProvider<IDelegatingHandler>, IDisposable {
+            Task SendPropertyPatchAsync(PropertyCollection reportedProperties, CancellationToken cancellationToken);
        }

        internal class InternalClient : IDisposable {
+            internal PayloadConvention PayloadConvention { get; }
+            internal Task<ClientProperties> GetClientPropertiesAsync(CancellationToken cancellationToken);
+            internal Task SubscribeToCommandsAsync(Func<CommandRequest, object, Task<CommandResponse>> callback, object userContext, CancellationToken cancellationToken);
+            internal Task SubscribeToWritablePropertiesEventAsync(Func<ClientPropertyCollection, object, Task> callback, object userContext, CancellationToken cancellationToken);
+            internal Task UpdateClientPropertiesAsync(ClientPropertyCollection clientProperties, CancellationToken cancellationToken);
         }

-        public sealed class Message : IDisposable, IReadOnlyIndicator {
+        public class Message : IDisposable, IReadOnlyIndicator {
+            internal Message(PayloadCollection payloadCollection);
-            public Stream BodyStream { get; }
+            public Stream BodyStream { get; protected set; }
-            public string ComponentName { get; set; }
+            public virtual string ComponentName { get; set; }
-            public string ContentEncoding { get; set; }
+            public virtual string ContentEncoding { get; set; }
-            public string ContentType { get; set; }
+            public virtual string ContentType { get; set; }
-            private void Dispose(bool disposing);
+            protected virtual void Dispose(bool disposing);
+            protected void DisposeBodyStream();
-            public Stream GetBodyStream();
+            public virtual Stream GetBodyStream();
+            private void SetSystemProperty(string key, object value);
        }
    }

     namespace Microsoft.Azure.Devices.Client.Transport {
         internal abstract class DefaultDelegatingHandler : IContinuationProvider<IDelegatingHandler>, IDelegatingHandler, IDisposable {
+            public virtual Task SendPropertyPatchAsync(PropertyCollection reportedProperties, CancellationToken cancellationToken);
         }
         
         internal sealed class ErrorDelegatingHandler : DefaultDelegatingHandler {
+            public override Task SendPropertyPatchAsync(PropertyCollection reportedProperties, CancellationToken cancellationToken);
         }

         internal class RetryDelegatingHandler : DefaultDelegatingHandler {
+            public override Task SendPropertyPatchAsync(PropertyCollection reportedProperties, CancellationToken cancellationToken);
         }
     }

     namespace Microsoft.Azure.Devices.Client.Transport.Mqtt {
         internal sealed class MqttTransportHandler : TransportHandler, IMqttIotHubEventHandler {
+            public override Task SendPropertyPatchAsync(PropertyCollection reportedProperties, CancellationToken cancellationToken);
         }
     }
 }
```