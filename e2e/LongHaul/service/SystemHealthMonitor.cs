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
        private static readonly TimeSpan s_interval = TimeSpan.FromSeconds(3);

        public SystemHealthMonitor(Logger logger)
        {
            _logger = logger;
            _logger.LoggerContext.Add("Component", nameof(SystemHealthMonitor));
        }

        public async Task RunAsync(CancellationToken ct)
        {
            int errorCount = 0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    _ = BuildAndLogSystemHealth(_logger);
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

        internal static SystemHealthC2dMessage BuildAndLogSystemHealth(Logger logger)
        {
            var message = new SystemHealthC2dMessage();
            logger.Metric(nameof(message.ProcessCpuUsagePercent), message.ProcessCpuUsagePercent);
            logger.Metric(nameof(message.ProcessWorkingSet), message.ProcessWorkingSet);
            logger.Metric(nameof(message.ProcessWorkingSetPrivate), message.ProcessWorkingSetPrivate);
            logger.Metric(nameof(message.ProcessPrivateBytes), message.ProcessPrivateBytes);
            if (message.ProcessBytesInAllHeaps.HasValue)
            {
                logger.Metric(nameof(message.ProcessBytesInAllHeaps), message.ProcessBytesInAllHeaps.Value);
            }

            return message;
        }
    }
}
