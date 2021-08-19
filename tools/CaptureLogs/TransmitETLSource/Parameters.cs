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
            Group = "insightsconfig",
            HelpText = "The Application Insights connection string.")]
        public string ConnectionString { get; set; }

        [Option(
            "useinsightsconfig",
            Group = "insightsconfig",
            HelpText = "This flag will attempt to use the ApplicationInsights.config file. The file must me in the same path as the tool.")]
        public bool UseInsightsConfig { get; set; }

        [Option(
            "offlinestore",
            Required = false,
            Default = ".\\offlinestore",
            HelpText = "Sets the directory for the Application Insights telemetry channel to store telemetry if the device is offline.")]
            
        public string OfflineStore { get; set; }

        [Option(
            "maxstoresizemb",
            Required = false,
            Default = 10,
            HelpText = "Sets the maximum store size in MB for telemetry that is persisted if the device is offline.")]

        public int MaxStoreSize { get; set; }

        [Option(
            "heartbeatinterval",
            Required = false,
            Default = 300,
            HelpText = "The interval in seconds to send the heartbeat. Set to 0 to disable.")]
        public int HeartBeatInterval { get; set; }
    }
}
