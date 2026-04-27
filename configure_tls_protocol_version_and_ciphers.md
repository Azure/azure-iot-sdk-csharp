# Configure TLS protocol version and ciphers

When using the Azure IoT .NET SDK in your application, you may wish to control which version of TLS is used and which ciphers are used by TLS.

## Configuring the TLS version

When targetting .NET Standard, the SDK does not specify the TLS version.
Instead, it is preferred to let the OS decide; this gives users control over security.

> One exception to this is clients using .NET Framework 4.5.1 which does not have a "let the OS decide" option.
> In this case, the SDK is hard-coded to use the latest version, TLS 1.2, only.
>
> **WARNING** - The [.NET Framework 4.5.1 is no longer supported], meaning no security updates, so it is highly recommended that clients update to the latest [.NET version][dotnet releases] (or LTS version) to benefit from the latest security fixes.

To ensure your Azure IoT .NET application is using the most secure version one should configure the operating system to use explictly disable the undesirable SChannels.
See below for operating system-specific instructions.

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
Microsoft.Azure.Devices.Shared.TlsVersions.Instance.SetTlsMinimumVersions(SslProtocols.Tls12);
```

>- It will only allow one to specify TLS 1.0, 1.1, 1.2, 1.3, or any combination of the 3.
>- It will always add TLS 1.2 because by calling this method you are setting the minimum version, and it should also incude the latest version.
>- The default is SslProtocols.None, which is to let the OS decide.
>- If you wish to set it back to None, that is also allowed.

#### Cipher suites

.NET on Linux, as of .NET 5.0, now respects the OpenSSL configuration for default cipher suites when doing TLS/SSL. More information and instructions can be found
[here][linux cipher suites].

[.NET Framework 4.5.1 is no longer supported]: https://devblogs.microsoft.com/dotnet/support-ending-for-the-net-framework-4-4-5-and-4-5-1/
[dotnet releases]: https://docs.microsoft.com/lifecycle/products/microsoft-net-and-net-core
[TLS registry settings]: https://docs.microsoft.com/windows-server/security/tls/tls-registry-settings
[best practices with .NEt and TLS]: https://docs.microsoft.com/dotnet/framework/network-programming/tls
[enable and disable ciphers]: https://support.microsoft.com/help/245030/how-to-restrict-the-use-of-certain-cryptographic-algorithms-and-protoc
[linux cipher suites]: https://docs.microsoft.com/dotnet/core/compatibility/cryptography/5.0/default-cipher-suites-for-tls-on-linux
