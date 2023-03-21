using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.IoT.Thief.Device
{
    internal class SystemHealthTelemetry : TelemetryBase
    {
        private static readonly Process s_currentProcess = Process.GetCurrentProcess();
        private static readonly string s_processName = s_currentProcess.ProcessName;

        private static readonly PerformanceCounter s_processCpuCounter = new(
            "Process",
            "% Processor Time",
            s_processName,
            true);
        private static readonly PerformanceCounter s_processWorkingSet = new(
            "Process",
            "Working Set",
            s_processName,
            true);
        private static readonly PerformanceCounter s_processWorkingSetPrivate = new(
            "Process",
            "Working Set - Private",
            s_processName,
            true);
        private static readonly PerformanceCounter s_processPrivateBytes = new(
            "Process",
            "Private bytes",
            s_processName,
            true);
        private static readonly PerformanceCounter _processBytesInAllHeaps = null;
        //new PerformanceCounter(
        //    ".NET CLR Memory",
        //    "# Bytes in all Heaps",
        //    _processName,
        //    true);

        [JsonPropertyName("processCpuUsagePercent")]
        public float ProcessCpuUsagePercent { get; set; } = s_processCpuCounter.NextValue();

        [JsonPropertyName("processWorkingSet")]
        public float ProcessWorkingSet { get; set; } = s_processWorkingSet.NextValue();

        [JsonPropertyName("processWorkingSetPrivate")]
        public float ProcessWorkingSetPrivate { get; set; } = s_processWorkingSetPrivate.NextValue();

        [JsonPropertyName("processPrivateBytes")]
        public float ProcessPrivateBytes { get; set; } = s_processPrivateBytes.NextValue();

        [JsonPropertyName("processBytesInAllHeaps")]
        public float? ProcessBytesInAllHeaps { get; set; } = _processBytesInAllHeaps?.NextValue();
    }
}
