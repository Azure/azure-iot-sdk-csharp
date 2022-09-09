using System;
using CommandLine;

namespace Microsoft.Azure.Devices.Samples.JobsSample
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    /// <remarks>
    /// To get these connection strings, log into https://portal.azure.com, go to Resources, open the IoT hub, open Shared Access Policies, open iothubowner, and copy a connection string.
    /// </remarks>
    internal class Parameters
    {
        [Option(
            'c',
            "HubConnectionString",
            Required = false,
            HelpText = "The connection string of the IoT hub instance to connect to. Defaults to the IOTHUB_CONNECTION_STRING Environment Variable.")]
        public string HubConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");

        public bool Validate()
        {
            return !string.IsNullOrWhiteSpace(HubConnectionString);
        }
    }
}
