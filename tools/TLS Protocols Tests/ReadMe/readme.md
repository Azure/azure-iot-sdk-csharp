# TLS Protocol Tests

These tools can be used to validate that the SDK targetting .NET Framework 4.5.1 or .NET Core. honors OS settings, and/or enforces TLS 1.2 when appropriate.

To run it, you will need:

1. An Azure IoT Hub, device, a hub connection string, and device connection string.
1. A device provisioning service, enrollment group, generated device SAS token (see dps-keygen)
1. WireShark (https://www.wireshark.org/) - to observe which TLS versions are actually used on the wire.
  - It is recommended add a filter in WireShark (i.e. `ip.dst == xx.xx.xx.xx` where xx.xx.xx.xx is the IP address of your hub or dps).
    - Ping the hub/dps mentioned in the connection strings to get those IP addresses.

## Running the tool

1. Configure Windows registry as desired.
  - See <https://docs.microsoft.com/en-us/dotnet/framework/network-programming/tls#configuring-security-via-the-windows-registry> and <https://docs.microsoft.com/en-us/windows-server/security/tls/tls-registry-settings> for details.
    - Or take a look at the .reg in this project to quickly enable only TLS 1.2 or 1.1.
1. Start WireShark and add IP filters.
1. Consider using the azure CLI to watch for messages that reach the hub: `az iot hub monitor-events -n "your hub name"`
1. Set the necessary environment variables specified in the Program.cs file.
  - If using VS, open the project properties' Debug tab. You can configure environment variables in there.
1. Run the project:
  - If using VS, open Program.cs and select either net451 or netcoreapp2.1., and set **TLS protocol tests** as the startup project.
  - If running from command-line, use `dotnet run -f net451` or `dotnet run -f netcoreapp2.1`
1. Observe output of the tool.
  - If the device client failed to connect it will report the exception types and messages.
    - The key error for when a common TLS protocol could not be negotiated is: _System.ComponentModel.Win32Exception: The client and server cannot communicate, because they do not possess a common algorithm_
1. Observe the WireShark output. The Protocol column will indicate the TLS version used, when relevant.

**Be sure to delete all the folders added by these exported registry files (look under HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols) registry key when finished to revert to OS defaults!**