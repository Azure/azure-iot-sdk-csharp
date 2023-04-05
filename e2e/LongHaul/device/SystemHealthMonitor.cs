// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mash.Logging;

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
        private static int s_port;
        private static readonly TimeSpan s_interval = TimeSpan.FromSeconds(3);

        public SystemHealthMonitor(IIotHub iotHub, int portFilter, Logger logger)
        {
            _iotHub = iotHub;
            s_port = portFilter;
            _logger = logger;
            _logger.LoggerContext.Add("Component", nameof(SystemHealthMonitor));
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var properties = new SystemProperties();
            _logger.Trace(
                "System Properties",
                TraceSeverity.Information,
                new Dictionary<string, string>
                {
                    { nameof(properties.FrameworkDescription), properties.FrameworkDescription },
                    { nameof(properties.OsVersion), properties.OsVersion },
                    { nameof(properties.SystemArchitecture), properties.SystemArchitecture },
                });
            await _iotHub.SetPropertiesAsync("systemProperties", properties, ct).ConfigureAwait(false);

            while (!ct.IsCancellationRequested)
            {
                SystemHealthTelemetry telemetry = BuildAndLogSystemHealth(_logger);
                _iotHub.AddTelemetry(telemetry);

                await Task.Delay(s_interval, ct).ConfigureAwait(false);
            }
        }

        internal static SystemHealthTelemetry BuildAndLogSystemHealth(Logger logger)
        {
            var telemetry = new SystemHealthTelemetry(s_port);
            logger.Metric(nameof(telemetry.TotalAssignedMemoryBytes), telemetry.TotalAssignedMemoryBytes);
            logger.Metric(nameof(telemetry.ProcessCpuUsagePercent), telemetry.ProcessCpuUsagePercent);
            logger.Metric(nameof(telemetry.TotalGCBytes), telemetry.TotalGCBytes);
            logger.Metric(nameof(telemetry.ActiveTcpConnections), telemetry.ActiveTcpConnections);

            return telemetry;
        }
    }
}
