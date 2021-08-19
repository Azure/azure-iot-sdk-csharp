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
            "heartbeatinterval",
            Required = false,
            Default = 300,
            HelpText = "The interval in seconds to send the heartbeat.")]
        public int HeartBeatInterval { get; set; }
    }
}
