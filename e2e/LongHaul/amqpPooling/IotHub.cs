﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mash.Logging;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.LongHaul.AmqpPooling
{
    internal class IotHub : IAsyncDisposable
    {
        private readonly string _hubConnectionString;
        private readonly Logger _logger;
        private readonly IotHubClientTransportSettings _transportSettings;
        private readonly IotHubClientOptions _clientOptions;
        private static IList<Device> s_devices;
        private static IDictionary<string, DeviceOperations> s_deviceOperations;

        private static readonly TimeSpan s_messageLoopSleepTime = TimeSpan.FromSeconds(3);

        private SemaphoreSlim _lifetimeControl = new(1, 1);

        public IotHub(Logger logger, Parameters parameters, IList<Device> devices)
        {
            _logger = logger;
            _hubConnectionString = parameters.IotHubConnectionString;
            _transportSettings = parameters.GetTransportSettingsWithPooling();
            _clientOptions = new IotHubClientOptions(_transportSettings);

            s_devices = devices;
            s_deviceOperations = new Dictionary<string, DeviceOperations>();
        }

        public Dictionary<string, string> TelemetryUserProperties { get; } = new();

        public async Task InitializeAsync()
        {
            await _lifetimeControl.WaitAsync().ConfigureAwait(false);

            var helper = new IotHubConnectionStringHelper(_hubConnectionString);

            try
            {
                _logger.Trace(
                    $"Creating {s_devices.Count} device clients with transport settings [{_transportSettings.ToString()}].",
                    TraceSeverity.Information);

                foreach (var device in s_devices)
                {
                    string deviceConnectionString = $"HostName={helper.HostName};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";

                    var deviceClient = new IotHubDeviceClient(deviceConnectionString, _clientOptions);

                    var deviceOperations = new DeviceOperations(deviceClient, device.Id, _logger.Clone());
                    await deviceOperations.InitializeAsync().ConfigureAwait(false);

                    s_deviceOperations.Add(device.Id, deviceOperations);
                }
            }
            finally
            {
                _lifetimeControl.Release();
            }
        }

        public async Task SendTelemetryMessagesAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(s_messageLoopSleepTime, ct).ConfigureAwait(false);

                List<Task> deviceOperationTasks = new();

                if (s_deviceOperations != null)
                {
                    foreach (KeyValuePair<string, DeviceOperations> entry in s_deviceOperations)
                    {
                        string deviceId = entry.Key;
                        DeviceOperations deviceOperations = entry.Value;

                        var telemetryObject = new DeviceTelemetry
                        {
                            DeviceId = deviceId,
                            GuidValue = Guid.NewGuid().ToString(),
                        };

                        var message = new TelemetryMessage(telemetryObject);

                        foreach (KeyValuePair<string, string> prop in TelemetryUserProperties)
                        {
                            message.Properties.TryAdd(prop.Key, prop.Value);
                        }

                        deviceOperationTasks.Add(await deviceOperations.SendAsync(message, ct).ConfigureAwait(false));
                    }

                    await Task.WhenAll(deviceOperationTasks).ConfigureAwait(false);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _logger.Trace($"Disposing the {nameof(IotHub)} instance", TraceSeverity.Verbose);

            if (_lifetimeControl != null)
            {
                _lifetimeControl.Dispose();
                _lifetimeControl = null;
            }

            if (s_deviceOperations != null)
            {
                foreach (KeyValuePair<string, DeviceOperations> entry in s_deviceOperations)
                {
                    IotHubDeviceClient deviceClient = entry.Value.DeviceClient;
                    await deviceClient.DisposeAsync().ConfigureAwait(false);
                }
            }

            _logger.Trace($"{nameof(IotHub)} instance disposed", TraceSeverity.Verbose);
        }

        private class DeviceTelemetry
        {
            [JsonProperty("deviceId")]
            public string DeviceId { get; set; }

            [JsonProperty("sentTimeUtc")]
            public DateTimeOffset SentOnUtc { get; set; } = DateTimeOffset.UtcNow;

            [JsonProperty("guidValue")]
            public string GuidValue { get; set; }
        }
    }
}
