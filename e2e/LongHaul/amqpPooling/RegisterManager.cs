// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mash.Logging;

namespace Microsoft.Azure.Devices.LongHual.AmqpPooling
{
    internal class RegisterManager : IDisposable
    {
        private readonly Logger _logger;
        private readonly int _devicesCountNumber;

        private const string DevicePrefix = "LongHaulAmqpPoolingDevice_";

        private static IotHubServiceClient s_serviceClient;
        private static IList<Device> s_devices;

        public RegisterManager(Logger logger, Parameters parameters)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _devicesCountNumber = parameters.DevicesCountNumber;

            s_serviceClient = new IotHubServiceClient(parameters.IotHubConnectionString);
            s_devices = new List<Device>(_devicesCountNumber);
        }

        public async Task<IList<Device>> AddDevicesAsync(CancellationToken ct)
        {
            _logger.Trace($"Start creating totally {_devicesCountNumber} devices.", TraceSeverity.Information);

            for (int i=0; i< _devicesCountNumber; i++)
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
            _logger.Trace($"Start deleting totally {_devicesCountNumber} devices for Amqp pooling long haul testing.", TraceSeverity.Information);

            foreach (var device in s_devices)
            {
                string deviceId = device.Id;
                _logger.Trace($"Deleting a device with Id {deviceId}", TraceSeverity.Verbose);
                await s_serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
            }

            s_devices = null;
        }

        public async void Dispose()
        {
            _logger.Trace("Disposing RegisterManager instance...", TraceSeverity.Verbose);

            s_serviceClient?.Dispose();
            if (s_devices != null)
            {
                await RemoveDevicesAsync().ConfigureAwait(false);
            }

            _logger.Trace("RegisterManager instance disposed.", TraceSeverity.Verbose);
        }
    }
}
