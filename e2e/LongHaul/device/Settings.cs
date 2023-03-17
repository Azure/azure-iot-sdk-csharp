using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.IoT.Thief.Device
{
    internal class Settings
    {
        public string AiKey { get; set; }
        public string DeviceConnectionString { get; set; }
        public TransportType Transport { get; set; }
    }
}
