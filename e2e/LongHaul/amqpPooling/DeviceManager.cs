// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Mash.Logging;

namespace Microsoft.Azure.Devices.LongHaul.AmqpPooling
{
    internal class DeviceManager : IAsyncDisposable
    {
        private readonly Logger _logger;
        private readonly int _devicesCount;

        private const string DevicePrefix = "LongHaulAmqpPoolingDevice_";

        private static IotHubServiceClient s_serviceClient;
        private static IList<Device> s_devices;

        public DeviceManager(Logger logger, Parameters parameters)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _devicesCount = parameters.DevicesCount;

            s_serviceClient = new IotHubServiceClient(parameters.IotHubConnectionString);
            s_devices = new List<Device>(_devicesCount);
        }

        public async Task RemoveDevicesBeforeRunningAsync(CancellationToken ct)
        {
            _logger.Trace($"Clean up devices with Id prefix [{DevicePrefix}] before running the app.", TraceSeverity.Information);

            AsyncPageable<ClientTwin> allDevices = s_serviceClient.Query.Create<ClientTwin>("SELECT deviceId FROM devices", ct);

            await foreach (ClientTwin device in allDevices)
            {
                string deviceId = device.DeviceId;
                if(deviceId.StartsWith(DevicePrefix))
                {
                    await s_serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
                }
            }
        }

        public async Task<IList<Device>> AddDevicesAsync(CancellationToken ct)
        {
            _logger.Trace($"Start creating totally {_devicesCount} devices.", TraceSeverity.Information);

            for (int i = 0; i < _devicesCount; i++)
            {
                string deviceId = DevicePrefix + i;
                _logger.Trace($"Creating a device with Id {deviceId}", TraceSeverity.Verbose);

                var device = new Device(deviceId);
                device = await s_serviceClient.Devices.CreateAsync(device, ct).ConfigureAwait(false);
                s_devices.Add(device);
            }

            return s_devices;
        }

        public async Task RemoveDevicesAsync()
        {
            _logger.Trace($"Start deleting totally {_devicesCount} devices for Amqp pooling long haul testing.", TraceSeverity.Information);

            foreach (var device in s_devices)
            {
                string deviceId = device.Id;
                _logger.Trace($"Deleting a device with Id {deviceId}", TraceSeverity.Verbose);
                await s_serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
            }

            s_devices = null;
        }

        public async ValueTask DisposeAsync()
        {
            _logger.Trace($"Disposing {nameof(DeviceManager)} instance...", TraceSeverity.Verbose);

            if (s_devices != null)
            {
                await RemoveDevicesAsync().ConfigureAwait(false);
            }
            s_serviceClient?.Dispose();

            _logger.Trace($"{nameof(DeviceManager)} instance disposed.", TraceSeverity.Verbose);
        }
    }
}
