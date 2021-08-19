using CommandLine;

namespace TransmitETL
{
    internal class Parameters
    {
        [Option(
            "sessionname",
            Required = true,
            HelpText = "The trace session to attach to.")]
        public string SessionName { get; set; }

        [Option(
            "connectionstring",
            Required = true,
            HelpText = "The Application Insights connection string.")]
        public string ConnectionString { get; set; }

        [Option(
            "offlinestore",
            Required = false,
            Default = ".\\offlinestore",
            HelpText = "Sets the directory for the Application Insights telemetry channel to store telemetry if the device is offline.")]
            
        public string OfflineStore { get; set; }

        [Option(
            "maxstoresizemb",
            Required = false,
            Default = "10",
            HelpText = "Sets the maximum store size in MB for telemetry that is persisted if the device is offline.")]

        public int MaxStoreSize { get; set; }

        [Option(
            "heartbeatinterval",
            Required = false,
            Default = 300,
            HelpText = "The interval in seconds to send the heartbeat.")]
        public int HeartBeatInterval { get; set; }
    }
}
