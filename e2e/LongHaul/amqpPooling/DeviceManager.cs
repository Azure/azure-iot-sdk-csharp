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
    internal class DeviceManager : IDisposable
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

        public async Task RemoveDevicesAsync(CancellationToken ct = default)
        {
            _logger.Trace($"Clean up devices with Id prefix [{DevicePrefix}].", TraceSeverity.Information);

            AsyncPageable<ClientTwin> allDevices = s_serviceClient.Query.Create<ClientTwin>("SELECT deviceId FROM devices", ct);

            await foreach (ClientTwin device in allDevices)
            {
                string deviceId = device.DeviceId;
                if (deviceId.StartsWith(DevicePrefix))
                {
                    await s_serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
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
                device = await s_serviceClient.Devices.CreateAsync(device, ct).ConfigureAwait(false);
                s_devices.Add(device);
            }

            return s_devices;
        }

        public void Dispose()
        {
            _logger.Trace($"Disposing {nameof(DeviceManager)} instance...", TraceSeverity.Verbose);

            s_serviceClient?.Dispose();

            _logger.Trace($"{nameof(DeviceManager)} instance disposed.", TraceSeverity.Verbose);
        }
    }
}
