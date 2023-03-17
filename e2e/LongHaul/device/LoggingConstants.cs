namespace Microsoft.Azure.IoT.Thief.Device
{
    internal static class LoggingConstants
    {
        // Metrics

        public const string DisconnectedDurationSeconds = "DisconnectedDurationSeconds";
        public const string TotalMessagesSent = "TotalMessagesSent";
        public const string MessageDelaySeconds = "MessageDelaySeconds";
        public const string MessageBacklog = "MessageBacklog";

        // Events

        public const string StartingRun = "StartingRun";


        // Logging properties

        public const string RunId = "runId";
        public const string SdkLanguage = "sdkLanguage";
        public const string SdkVersion = "sdkVersion";
        
        public const string Hub = "hub";
        public const string DeviceId = "deviceId";
        public const string Transport = "transport";

        public const string DisconnectedStatus = "disconnectedStatus";
        public const string DisconnectedReason = "disconnectedReason";
        public const string ConnectionStatusChangeCount = "connectionStatusChangeCount";
    }
}
