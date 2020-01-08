# Configure TLS protocol

In the Azure Iot SDK for .NET, we do not specify the TLS version and let the OS decide on the TLS version.
So, to be sure your .NET code is using TLS1.2, you should configure your operating system to use TLS1.2 and disable TLS1.0 and TLS 1.1.

## Windows (SChannel)

You can use the registry to control over the protocols that your client and/or server app negotiates.

Start with the ```HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols``` registry key.

Under that key you can create any subkeys in the set SSL 2.0, SSL 3.0, TLS 1.0, TLS 1.1, and TLS 1.2. Under each of those subkeys, you can create subkeys Client and/or Server. Under Client and Server, you can create DWORD values DisabledByDefault (0 or 1) and Enabled (0 or 0xFFFFFFFF).

[See here more information on TLS best practices](https://docs.microsoft.com/en-us/dotnet/framework/network-programming/tls)

[See here more on cipher restriction](https://support.microsoft.com/en-us/help/245030/how-to-restrict-the-use-of-certain-cryptographic-algorithms-and-protoc)

## Linux

TBD

## iOS

TBD

