using Microsoft.Azure.Devices.Client;
using ThiefDevice;

namespace Microsoft.Azure.IoT.Thief.Device
{
    internal class Settings
    {
        public string AiKey { get; set; }
        public string DeviceConnectionString { get; set; }
        public TransportType TransportType { get; set; }
        public IotHubClientTransportProtocol TransportProtocol { get; set; }
    }
}
