// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mash.Logging;
using Microsoft.Azure.Devices.Client;
using static Microsoft.Azure.Devices.LongHaul.AmqpPooling.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.AmqpPooling
{
    internal class DeviceOperations
    {
        private readonly IotHubDeviceClient _deviceClient;
        private readonly string _deviceId;
        private readonly Logger _logger;

        private long _totalTelemetryMessagesSent = 0;

        public DeviceOperations(IotHubDeviceClient deviceClient, string deviceId, Logger logger)
        {
            _deviceClient = deviceClient;
            _deviceId = deviceId;
            logger.LoggerContext.Add("deviceId", deviceId);
            _logger = logger;
        }

        public IotHubDeviceClient DeviceClient => _deviceClient;

        public async Task<Task> SendAsync(TelemetryMessage message, CancellationToken ct)
        {
            try
            {
                var sw = new Stopwatch();

                _logger.Trace($"Sending a telemetry message from the device with Id [{_deviceId}].", TraceSeverity.Information);
                sw.Restart();

                await _deviceClient.SendTelemetryAsync(message, ct).ConfigureAwait(false);
                sw.Stop();

                _logger.Metric(TelemetryMessageDelaySeconds, sw.Elapsed.TotalSeconds);
                _logger.Metric(TotalTelemetryMessagesSent, ++_totalTelemetryMessagesSent);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Trace($"Exception when sending telemetry from the device with Id [{_deviceId}].\n{ex}", TraceSeverity.Warning);

                return Task.FromException(ex);
            }
        }
    }
}
