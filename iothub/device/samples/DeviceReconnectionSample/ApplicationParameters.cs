using CommandLine;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public enum Transport
    {
        Mqtt,
        Amqp,
    }

    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class ApplicationParameters
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
            "Transport",
            Default = Transport.Mqtt,
            Required = false,
            HelpText = "The transport to use to communicate with the IoT hub.")]
        public Transport Transport { get; set; }

        [Option(
            "Protocol",
            Default = IotHubClientTransportProtocol.Tcp,
            Required = false,
            HelpText = "The protocol to connect over.")]
        public IotHubClientTransportProtocol Protocol { get; set; }

        [Option(
            'r',
            "Application running time (in seconds)",
            Required = false,
            HelpText = "The running time for this console application. Leave it unassigned to run the application until it is explicitly canceled using Control+C.")]
        public double? ApplicationRunningTime { get; set; }

        public int MyProperty { get; set; }

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
