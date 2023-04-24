// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Mash.Logging;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.LongHaul.AmqpPooling
{
    internal class IotHub : IAsyncDisposable
    {
        private readonly string _hubConnectionString;
        private readonly int _devicesCount;
        private readonly Logger _logger;
        private readonly IotHubServiceClient _serviceClient;
        private readonly IotHubClientTransportSettings _transportSettings;
        private readonly IotHubClientOptions _clientOptions;
        private readonly IList<Device> _devices;
        private readonly List<DeviceOperations> _deviceOperations = new();

        private const string DevicePrefix = "LongHaulAmqpPoolingDevice_";

        private static readonly TimeSpan s_messageLoopSleepTime = TimeSpan.FromSeconds(3);

        private SemaphoreSlim _lifetimeControl = new(1, 1);

        public IotHub(Logger logger, Parameters parameters)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _devicesCount = parameters.DevicesCount;
            _hubConnectionString = parameters.IotHubConnectionString;
            _transportSettings = parameters.GetTransportSettingsWithPooling();
            _clientOptions = new IotHubClientOptions(_transportSettings);
            _serviceClient = new IotHubServiceClient(parameters.IotHubConnectionString);
            _devices = new List<Device>(_devicesCount);
        }

        public async Task RemoveDevicesAsync(CancellationToken ct = default)
        {
            _logger.Trace($"Clean up devices with Id prefix [{DevicePrefix}].", TraceSeverity.Information);

            AsyncPageable<ClientTwin> allDevices = _serviceClient.Query.Create<ClientTwin>("SELECT deviceId FROM devices", ct);

            await foreach (ClientTwin device in allDevices)
            {
                string deviceId = device.DeviceId;
                if (deviceId.StartsWith(DevicePrefix))
                {
                    await _serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
                    _logger.Trace($"Deleted the device with Id [{deviceId}].", TraceSeverity.Verbose);
                }
            }
        }

        public async Task<IList<Device>> AddDevicesAsync(CancellationToken ct)
        {
            _logger.Trace($"Start creating {_devicesCount} devices.", TraceSeverity.Information);

            for (int i = 0; i < _devicesCount; i++)
            {
                string deviceId = DevicePrefix + i;
                _logger.Trace($"Creating a device with Id {deviceId}", TraceSeverity.Verbose);

                var device = new Device(deviceId);
                device = await _serviceClient.Devices.CreateAsync(device, ct).ConfigureAwait(false);
                _devices.Add(device);
            }

            return _devices;
        }

        public async Task InitializeAsync()
        {
            await _lifetimeControl.WaitAsync().ConfigureAwait(false);

            var helper = new IotHubConnectionStringHelper(_hubConnectionString);

            try
            {
                _logger.Trace(
                    $"Creating {_devices.Count} device clients with transport settings [{_transportSettings.ToString()}].",
                    TraceSeverity.Information);

                foreach (Device device in _devices)
                {
                    var deviceOperations = new DeviceOperations(device, helper.HostName, _clientOptions, _logger.Clone());
                    await deviceOperations.InitializeAsync().ConfigureAwait(false);

                    _deviceOperations.Add(deviceOperations);
                }
            }
            finally
            {
                _lifetimeControl.Release();
            }
        }

        public async Task RunDevicesTasksAsync(CancellationToken ct)
        {
            var deviceOperationTasks = _deviceOperations
                .Select(deviceOperation => deviceOperation.SendMessagesAsync(ct))
                .ToList();

            await Task.WhenAll(deviceOperationTasks).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            _logger.Trace($"Disposing the {nameof(IotHub)} instance", TraceSeverity.Verbose);

            if (_lifetimeControl != null)
            {
                _lifetimeControl.Dispose();
                _lifetimeControl = null;
            }

            if (_deviceOperations != null)
            {
                foreach (DeviceOperations deviceOp in _deviceOperations)
                {
                    await deviceOp.DisposeAsync().ConfigureAwait(false);
                }
            }

            _serviceClient?.Dispose();

            _logger.Trace($"{nameof(IotHub)} instance disposed", TraceSeverity.Verbose);
        }
    }
}
