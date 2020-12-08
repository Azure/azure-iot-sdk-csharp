using CommandLine;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'p',
            "PrimaryConnectionString",
            Required = true,
            HelpText = "The primary connection string for the device to simulate.")]
        public string PrimaryConnectionString { get; set; }

        [Option(
            's',
            "SecondaryConnectionString",
            Required = false,
            HelpText = "The secondary connection string for the device to simulate.")]
        public string SecondaryConnectionString { get; set; }

        [Option(
            't',
            "TransportType",
            Default = TransportType.Mqtt,
            Required = false,
            HelpText = "The transport to use to communicate with the IoT Hub. Possible values include Mqtt, Mqtt_WebSocket_Only, Mqtt_Tcp_Only, Amqp, Amqp_WebSocket_Only, Amqp_Tcp_only, and Http1.")]
        public TransportType TransportType { get; set; }

        [Option(
            'r',
            "Application running time (in seconds)",
            Required = false,
            HelpText = "The running time for this console application. Leave it unassigned to run the application until it is explicitly canceled using Control+C.")]
        public double? ApplicationRunningTime { get; set; }

        public List<string> GetConnectionStrings()
        {
            var cs = new List<string>(2)
            {
                PrimaryConnectionString,
            };

            if (!string.IsNullOrWhiteSpace(SecondaryConnectionString))
            {
                cs.Add(SecondaryConnectionString);
            }

            return cs;
        }
    }
}
