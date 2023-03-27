using Mash.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    /// <summary>
    /// Acts as a sensor for the device, but what it "senses" is system and process health.
    /// In this way we can have the SDK be used for various functionality, but also get reports
    /// of its health.
    /// </summary>
    internal class SystemHealthMonitor
    {
        private readonly IIotHub _iotHub;
        private readonly Logger _logger;
        private static readonly TimeSpan s_interval = TimeSpan.FromSeconds(3);

        public SystemHealthMonitor(IIotHub iotHub, Logger logger)
        {
            _iotHub = iotHub;
            _logger = logger;
            _logger.LoggerContext.Add("Component", nameof(SystemHealthMonitor));
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var properties = new SystemProperties();
            _logger.Trace(
                "System properties",
                TraceSeverity.Information,
                new Dictionary<string, string>
                {
                    { nameof(properties.FrameworkDescription), properties.FrameworkDescription },
                    { nameof(properties.OsVersion), properties.OsVersion },
                    { nameof(properties.SystemArchitecture), properties.SystemArchitecture },
                });
            await _iotHub.SetPropertiesAsync("systemProperties", properties, ct).ConfigureAwait(false);

            int errorCount = 0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    SystemHealthTelemetry telemetry = BuildAndLogSystemHealth(_logger);
                    _iotHub.AddTelemetry(telemetry);
                    errorCount = 0;
                }
                catch (Exception ex)
                {
                    _logger.Trace(
                        $"Error {++errorCount} reporting system health telemetry {ex}",
                        TraceSeverity.Error);

                    if (errorCount > 10)
                    {
                        _logger.Trace("Quitting system health monitor due to too many consecutive errors", TraceSeverity.Critical);
                        break;
                    }
                }

                await Task.Delay(s_interval, ct).ConfigureAwait(false);
            }
        }

        internal static SystemHealthTelemetry BuildAndLogSystemHealth(Logger logger)
        {
            var telemetry = new SystemHealthTelemetry();
            logger.Metric(nameof(telemetry.ProcessCpuUsagePercent), telemetry.ProcessCpuUsagePercent);
            logger.Metric(nameof(telemetry.ProcessWorkingSet), telemetry.ProcessWorkingSet);
            logger.Metric(nameof(telemetry.ProcessWorkingSetPrivate), telemetry.ProcessWorkingSetPrivate);
            logger.Metric(nameof(telemetry.ProcessPrivateBytes), telemetry.ProcessPrivateBytes);
            if (telemetry.ProcessBytesInAllHeaps.HasValue)
            {
                logger.Metric(nameof(telemetry.ProcessBytesInAllHeaps), telemetry.ProcessBytesInAllHeaps.Value);
            }

            return telemetry;
        }
    }
}
