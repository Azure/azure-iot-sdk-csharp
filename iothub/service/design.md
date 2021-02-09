# RBAC API Design for IoT hub

## Service Client

```csharp
/// <summary>
/// Creates a <see cref="ServiceClient"/> using AAD token credentials and the specified transport type.
/// </summary>
/// <param name="hostName">IoT hub host name.</param>
/// <param name="credential">AAD <see cref="TokenCredential"> to authenticate with IoT hub.</param>
/// <param name="transportType">Specifies whether Amqp or Amqp_WebSocket_Only transport is used.</param>
/// <param name="transportSettings">Specifies the AMQP_WS and HTTP proxy settings for service client.</param>
/// <param name="options">The options that allow configuration of the service client instance during initialization.</param>
/// <returns>An instance of <see cref="ServiceClient"/>.</returns>
public static ServiceClient Create(
    string hostName,
    TokenCredential credential,
    TransportType transportType,
    ServiceClientTransportSettings transportSettings = default,
    ServiceClientOptions options = default)

/// <summary>
/// Creates a see <see cerf="ServiceClient"/> using SAS token credentials and the specified transport type.
/// </summary>
/// <param name="hostName">IoT hub host name.</param>
/// <param name="credential"><see cref="AzureSasCredential"> to authenticate with IoT hub.</param>
/// <param name="transportType">Specifies whether Amqp or Amqp_WebSocket_Only transport is used.</param>
/// <param name="transportSettings">Specifies the AMQP_WS and HTTP proxy settings for service client.</param>
/// <param name="options">The options that allow configuration of the service client instance during initialization.</param>
/// <returns>An instance of <see cref="ServiceClient"/>.</returns>
public static ServiceClient Create(
    string hostName,
    AzureSasCredential credential,
    TransportType transportType,
    ServiceClientTransportSettings transportSettings = default,
    ServiceClientOptions options = default)
```

## Registry Manager

```csharp
/// <summary>
/// Creates an instance of <see cref="RegistryManager"/>.
/// </summary>
/// <param name="hostName">IoT hub host name.</param>
/// <param name="credential">AAD <see cref="TokenCredential"> to authenticate with IoT hub.</param>
/// <param name="transportSettings">The HTTP transport settings.</param>
/// <returns>An instance of <see cref="RegistryManager"/>.</returns>
public static RegistryManager Create(
    string hostName,
    TokenCredential credential,
    HttpTransportSettings transportSettings = default)

/// <summary>
/// Creates an instance of <see cref="RegistryManager"/>.
/// </summary>
/// <param name="hostName">IoT hub host name.</param>
/// <param name="credential"><see cref="AzureSasCredential"> to authenticate with IoT hub.</param>
/// <param name="transportSettings">The HTTP transport settings.</param>
/// <returns>An instance of <see cref="RegistryManager"/>.</returns>
public static RegistryManager Create(
    string hostName,
    AzureSasCredential credential,
    HttpTransportSettings transportSettings = default)
```

## Job Client

```csharp
/// <summary>
/// Creates an instance of <see cref="JobClient"/>.
/// </summary>
/// <param name="hostName">IoT hub host name.</param>
/// <param name="credential">AAD <see cref="TokenCredential"> to authenticate with IoT hub.</param>
/// <param name="transportSettings">The HTTP transport settings.</param>
/// <returns>An instance of <see cref="JobClient"/>.</returns>
public static JobClient Create(
    string hostName,
    TokenCredential credential,
    HttpTransportSettings transportSettings = default)

/// <summary>
/// Creates an instance of <see cref="JobClient"/>.
/// </summary>
/// <param name="hostName">IoT hub host name.</param>
/// <param name="credential"><see cref="TokenCredential"> to authenticate with IoT hub.</param>
/// <param name="transportSettings">The HTTP transport settings.</param>
/// <returns>An instance of <see cref="JobClient"/>.</returns>
public static JobClient Create(
    string hostName,
    AzureSasCredential credential,
    HttpTransportSettings transportSettings = default)
```

## Digital Twin Client

```csharp
/// <summary>
/// Initializes a new instance of the <see cref="DigitalTwinClient"/> class.</summary>
/// <param name="hostName">IoT hub host name.</param>
/// <param name="credential">AAD <see cref="TokenCredential"> to authenticate with IoT hub.</param>
/// <param name="handlers">The delegating handlers to add to the http client pipeline. You can add handlers for tracing, implementing a retry strategy, routing requests through a proxy, etc.</param>
/// <returns>An instance of <see cref="DigitalTwinClient"/></returns>
public static DigitalTwinClient Create(
    string hostName,
    TokenCredential credential,
    HttpTransportSettings transportSettings = default)

/// <summary>
/// Initializes a new instance of the <see cref="DigitalTwinClient"/> class.</summary>
/// <param name="hostName">IoT hub host name.</param>
/// <param name="credential"><see cref="TokenCredential"> to authenticate with IoT hub.</param>
/// <param name="handlers">The delegating handlers to add to the http client pipeline. You can add handlers for tracing, implementing a retry strategy, routing requests through a proxy, etc.</param>
/// <returns>An instance of <see cref="DigitalTwinClient"/></returns>
public static DigitalTwinClient Create(
    string hostName,
    AzureSasCredential credential,
    HttpTransportSettings transportSettings = default)
```
