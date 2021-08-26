# Configure TLS Protocol Version and Ciphers

When using the Azure IoT .NET SDK in your application, you may wish to control which version of TLS is used and which ciphers are used by TLS.

## Configuring the TLS version

When targetting .NET Standard, the SDK does not specify the TLS version.
Instead, it is preferred to let the OS decide; this gives users control over security.

> One exception to this is clients using .NET Framework 4.5.1 which does not have a "let the OS decide" option.
In this case, the SDK is hard-coded to use the latest version, TLS 1.2, only.
The [.NET Framework 4.5.1 is no longer supported], meaning no security updates, so it is highly recommended that clients update to the latest .NET Standard release of these SDK clients.

To ensure your Azure IoT .NET application is using the most secure version one should configure the operating system to use explictly disable the undesirable SChannels.
See below for operating system-specific instructions.

### Windows Instructions

One can use the Windows registry to control the TLS versions that is negotiated during connection.
Follow these [TLS registry settings].

For more information, check out this article about [best practices with .NET and TLS].

Also follow the instructions on how to [enable and disable ciphers].

[.NET Framework 4.5.1 is no longer supported]: https://devblogs.microsoft.com/dotnet/support-ending-for-the-net-framework-4-4-5-and-4-5-1/
[TLS registry settings]: https://docs.microsoft.com/windows-server/security/tls/tls-registry-settings
[best practices with .NEt and TLS]: https://docs.microsoft.com/dotnet/framework/network-programming/tls
[enable and disable ciphers]: https://support.microsoft.com/help/245030/how-to-restrict-the-use-of-certain-cryptographic-algorithms-and-protoc

### Linux Instructions

On Linux, one cannot influence the version of TLS used, like on Windows.
Although the SDK will default to the latest version of TLS by default, if one wishes to hard-code it to a specific version, there is a provision for that.

For example:

```C#
Microsoft.Azure.Devices.Shared.TlsVersions.Instance.SetTlsMinimumVersions(SslProtocols.Tls12);
```

Some notes:

- It will only allow one to specify TLS 1.0, 1.1, 1.2, or any combination of the 3.
- It will always add TLS 1.2 because by calling this method you are setting the minimum version, and it should also incude the latest version.
- The default is SslProtocols.None, which to let the OS decide.
- If you wish to set it back to None, that is also allowed.