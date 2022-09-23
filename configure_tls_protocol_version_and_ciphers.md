# Configure TLS protocol version and ciphers

When using the Azure IoT .NET SDK in your application, you may wish to control which version of TLS is used and which ciphers are used by TLS.

## Configuring the TLS version

When targetting .NET Standard, the SDK does not specify the TLS version.
Instead, it is preferred to let the OS decide by defaulting to SslProtcols.None; this gives users control over security.

### Windows instructions

#### TLS version

One can use the Windows registry to control the TLS versions that is negotiated during connection.
Follow these [TLS registry settings].

For more information, check out this article about [best practices with .NET and TLS].

#### Cipher suites

Also follow the instructions on how to [enable and disable ciphers].

### Linux instructions

#### TLS version

On Linux, one cannot influence the version of TLS used, like on Windows with the registry.
Although the SDK will default to the latest version of TLS by default, if one wishes to hard-code it to a specific version, there is a provision for that.

For example:

```csharp
// For device and module clients - pass options into the client constructor.
var options = new IotHubClientOptions
{
    TransportSettings =
    {
        SslProtocols = SslProtocols.Tls12,
    },
};

// For service client - pass options into the client constructor.
var options = new IotHubServiceOptions
{
    SslProtocols = SslProtocols.Tsl12,
};
```

#### Cipher suites

.NET on Linux, as of .NET 5.0, now respects the OpenSSL configuration for default cipher suites when doing TLS/SSL. More information and instructions can be found
[here][linux cipher suites].

[dotnet releases]: https://docs.microsoft.com/lifecycle/products/microsoft-net-and-net-core
[TLS registry settings]: https://docs.microsoft.com/windows-server/security/tls/tls-registry-settings
[best practices with .NEt and TLS]: https://docs.microsoft.com/dotnet/framework/network-programming/tls
[enable and disable ciphers]: https://support.microsoft.com/help/245030/how-to-restrict-the-use-of-certain-cryptographic-algorithms-and-protoc
[linux cipher suites]: https://docs.microsoft.com/dotnet/core/compatibility/cryptography/5.0/default-cipher-suites-for-tls-on-linux
