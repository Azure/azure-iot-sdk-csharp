using CommandLine;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'c',
            "DeviceConnectionString",
            Required = false,
            HelpText = "The primary connection string for the device to simulate.")]
        public string DeviceConnectionString { get; set; }

        [Option(
            'p',
            "HubConnectionString",
            Required = false,
            HelpText = "The primary connection string for the IoT hub in the event a device should be created by this sample.")]
        public string HubConnectionString { get; set; }

        [Option(
            't',
            "TransportType",
            Default = TransportType.Mqtt,
            Required = false,
            HelpText = "The transport to use to communicate with the IoT Hub. Possible values include Mqtt, Mqtt_WebSocket_Only, Mqtt_Tcp_Only, Amqp, Amqp_WebSocket_Only, Amqp_Tcp_only, and Http1.")]
        public TransportType TransportType { get; set; }
    }
}
