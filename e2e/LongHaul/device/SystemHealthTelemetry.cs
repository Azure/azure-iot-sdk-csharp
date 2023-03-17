using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.IoT.Thief.Device
{
    internal class SystemHealthTelemetry : TelemetryBase
    {
        private static readonly Process _currentProcess = Process.GetCurrentProcess();
        private static readonly string _processName = _currentProcess.ProcessName;

        private static readonly PerformanceCounter _processCpuCounter = new PerformanceCounter(
            "Process",
            "% Processor Time",
            _processName,
            true);
        private static readonly PerformanceCounter _processWorkingSet = new PerformanceCounter(
            "Process",
            "Working Set",
            _processName,
            true);
        private static readonly PerformanceCounter _processWorkingSetPrivate = new PerformanceCounter(
            "Process",
            "Working Set - Private",
            _processName,
            true);
        private static readonly PerformanceCounter _processPrivateBytes = new PerformanceCounter(
            "Process",
            "Private bytes",
            _processName,
            true);
        private static readonly PerformanceCounter _processBytesInAllHeaps = null;
        //new PerformanceCounter(
        //    ".NET CLR Memory",
        //    "# Bytes in all Heaps",
        //    _processName,
        //    true);

        [JsonPropertyName("processCpuUsagePercent")]
        public float ProcessCpuUsagePercent { get; set; } = _processCpuCounter.NextValue();

        [JsonPropertyName("processWorkingSet")]
        public float ProcessWorkingSet { get; set; } = _processWorkingSet.NextValue();

        [JsonPropertyName("processWorkingSetPrivate")]
        public float ProcessWorkingSetPrivate { get; set; } = _processWorkingSetPrivate.NextValue();

        [JsonPropertyName("processPrivateBytes")]
        public float ProcessPrivateBytes { get; set; } = _processPrivateBytes.NextValue();

        [JsonPropertyName("processBytesInAllHeaps")]
        public float? ProcessBytesInAllHeaps { get; set; } = _processBytesInAllHeaps?.NextValue();
    }
}
