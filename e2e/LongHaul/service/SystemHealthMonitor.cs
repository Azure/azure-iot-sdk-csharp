// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Mash.Logging;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    /// <summary>
    /// Acts as a sensor for the hub, but what it "senses" is system and process health.
    /// In this way we can have the SDK be used for various functionality, but also get reports
    /// of its health.
    /// </summary>
    internal class SystemHealthMonitor
    {
        private readonly Logger _logger;
        private static int s_port;
        private static readonly TimeSpan s_interval = TimeSpan.FromSeconds(3);

        public SystemHealthMonitor(int port, Logger logger)
        {
            s_port = port;
            _logger = logger;
            _logger.LoggerContext.Add("Component", nameof(SystemHealthMonitor));
        }

        public async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                _ = BuildAndLogSystemHealth(_logger);
                await Task.Delay(s_interval, ct).ConfigureAwait(false);
            }
        }

        internal static SystemHealthMessage BuildAndLogSystemHealth(Logger logger)
        {
            var message = new SystemHealthMessage(s_port);
            logger.Metric(nameof(message.TotalAssignedMemoryBytes), message.TotalAssignedMemoryBytes);
            logger.Metric(nameof(message.ProcessCpuUsagePercent), message.ProcessCpuUsagePercent);
            logger.Metric(nameof(message.TotalGCBytes), message.TotalGCBytes);
            logger.Metric(nameof(message.ActiveTcpConnections), message.ActiveTcpConnections);

            return message;
        }
    }
}
