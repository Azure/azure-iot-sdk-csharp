// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mash.Logging;
using static Microsoft.Azure.Devices.LongHaul.AmqpPooling.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.AmqpPooling
{
    internal class SystemHealthMonitor
    {
        private readonly int _port;
        private readonly Logger _logger;
        private static readonly TimeSpan s_interval = TimeSpan.FromSeconds(3);

        public SystemHealthMonitor(int portFilter, Logger logger)
        {
            _port = portFilter;
            _logger = logger;
            _logger.LoggerContext.Add(Component, nameof(SystemHealthMonitor));
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

            while (!ct.IsCancellationRequested)
            {
                BuildAndLogSystemHealth();

                await Task.Delay(s_interval, ct).ConfigureAwait(false);
            }
        }

        internal void BuildAndLogSystemHealth()
        {
            var telemetry = new SystemHealthTelemetry(_port);
            _logger.Metric(nameof(telemetry.TotalAssignedMemoryBytes), telemetry.TotalAssignedMemoryBytes);
            _logger.Metric(nameof(telemetry.ProcessCpuUsagePercent), telemetry.ProcessCpuUsagePercent);
            _logger.Metric(nameof(telemetry.TotalGCBytes), telemetry.TotalGCBytes);
            _logger.Metric(nameof(telemetry.ActiveTcpConnections), telemetry.ActiveTcpConnections);
        }
    }
}
