// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IList<Device> _devices;
        private readonly List<DeviceOperations> _deviceOperations = new();

        private static readonly TimeSpan s_messageLoopSleepTime = TimeSpan.FromSeconds(3);

        private SemaphoreSlim _lifetimeControl = new(1, 1);

        public IotHub(Logger logger, Parameters parameters, IList<Device> devices)
        {
            _logger = logger;
            _hubConnectionString = parameters.IotHubConnectionString;
            _transportSettings = parameters.GetTransportSettingsWithPooling();
            _clientOptions = new IotHubClientOptions(_transportSettings);

            _devices = devices;
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
                    string deviceConnectionString = $"HostName={helper.HostName};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";

                    var deviceClient = new IotHubDeviceClient(deviceConnectionString, _clientOptions);

                    var deviceOperations = new DeviceOperations(deviceClient, device.Id, _logger.Clone());
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

            _logger.Trace($"{nameof(IotHub)} instance disposed", TraceSeverity.Verbose);
        }
    }
}
