// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using static Microsoft.Azure.Devices.E2ETests.Helpers.HostNameHelper;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    public enum TestDeviceType
    {
        Sasl,
        X509
    }

    public enum ConnectionStringAuthScope
    {
        IoTHub,
        Device
    }

    public class TestDevice
    {
        private const int DelayAfterDeviceCreationSeconds = 0;
        private static readonly SemaphoreSlim s_semaphore = new SemaphoreSlim(1, 1);

        private static MsTestLogger _logger;
        private bool _disposedValue;

        private TestDevice(Device device, Client.IAuthenticationMethod authenticationMethod)
        {
            Device = device;
            AuthenticationMethod = authenticationMethod;
        }

        /// <summary>
        /// Factory method.
        /// </summary>
        /// <param name="namePrefix">The prefix to apply to your device name</param>
        /// <param name="type">The way the device will authenticate</param>
        public static async Task<TestDevice> GetTestDeviceAsync(MsTestLogger logger, string namePrefix, TestDeviceType type = TestDeviceType.Sasl)
        {
            _logger = logger;
            string prefix = namePrefix + type + "_";

            try
            {
                await s_semaphore.WaitAsync().ConfigureAwait(false);
                TestDevice ret = await CreateDeviceAsync(type, prefix).ConfigureAwait(false);

                _logger.Trace($"{nameof(GetTestDeviceAsync)}: Using device {ret.Id}.");
                return ret;
            }
            finally
            {
                s_semaphore.Release();
            }
        }

        private static async Task<TestDevice> CreateDeviceAsync(TestDeviceType type, string prefix)
        {
            string deviceName = "E2E_" + prefix + Guid.NewGuid();

            // Delete existing devices named this way and create a new one.
            using var rm = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            _logger.Trace($"{nameof(GetTestDeviceAsync)}: Creating device {deviceName} with type {type}.");

            Client.IAuthenticationMethod auth = null;

            var requestDevice = new Device(deviceName);
            if (type == TestDeviceType.X509)
            {
                requestDevice.Authentication = new AuthenticationMechanism
                {
                    X509Thumbprint = new X509Thumbprint
                    {
                        PrimaryThumbprint = Configuration.IoTHub.GetCertificateWithPrivateKey().Thumbprint
                    }
                };

                auth = new DeviceAuthenticationWithX509Certificate(deviceName, Configuration.IoTHub.GetCertificateWithPrivateKey());
            }

            Device device = await rm.AddDeviceAsync(requestDevice).ConfigureAwait(false);

            _logger.Trace($"{nameof(GetTestDeviceAsync)}: Pausing for {DelayAfterDeviceCreationSeconds}s after device was created.");
            await Task.Delay(DelayAfterDeviceCreationSeconds * 1000).ConfigureAwait(false);

            await rm.CloseAsync().ConfigureAwait(false);

            return new TestDevice(device, auth);
        }

        /// <summary>
        /// Used in conjunction with DeviceClient.CreateFromConnectionString()
        /// </summary>
        public string ConnectionString
        {
            get
            {
                string iotHubHostName = GetHostName(Configuration.IoTHub.ConnectionString);
                return $"HostName={iotHubHostName};DeviceId={Device.Id};SharedAccessKey={Device.Authentication.SymmetricKey.PrimaryKey}";
            }
        }

        /// <summary>
        /// Used in conjunction with DeviceClient.Create()
        /// </summary>
        public string IoTHubHostName => GetHostName(Configuration.IoTHub.ConnectionString);

        /// <summary>
        /// Device Id
        /// </summary>
        public string Id => Device.Id;

        /// <summary>
        /// Device identity object.
        /// </summary>
        public Device Device { get; private set; }

        public Client.IAuthenticationMethod AuthenticationMethod { get; private set; }

        public DeviceClient CreateDeviceClient(Client.TransportType transport, ClientOptions options = default)
        {
            DeviceClient deviceClient = null;

            if (AuthenticationMethod == null)
            {
                deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, transport, options);
                _logger.Trace($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {Device.Id} from connection string: {transport} ID={TestLogger.IdOf(deviceClient)}");
            }
            else
            {
                deviceClient = DeviceClient.Create(IoTHubHostName, AuthenticationMethod, transport, options);
                _logger.Trace($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {Device.Id} from IAuthenticationMethod: {transport} ID={TestLogger.IdOf(deviceClient)}");
            }

            return deviceClient;
        }

        public DeviceClient CreateDeviceClient(ITransportSettings[] transportSettings, ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device, ClientOptions options = default)
        {
            DeviceClient deviceClient = null;

            if (AuthenticationMethod == null)
            {
                if (authScope == ConnectionStringAuthScope.Device)
                {
                    deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, transportSettings, options);
                    _logger.Trace($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {Device.Id} from device connection string: ID={TestLogger.IdOf(deviceClient)}");
                }
                else
                {
                    deviceClient = DeviceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, Device.Id, transportSettings, options);
                    _logger.Trace($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {Device.Id} from IoTHub connection string: ID={TestLogger.IdOf(deviceClient)}");
                }
            }
            else
            {
                deviceClient = DeviceClient.Create(IoTHubHostName, AuthenticationMethod, transportSettings, options);
                _logger.Trace($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {Device.Id} from IAuthenticationMethod: ID={TestLogger.IdOf(deviceClient)}");
            }

            return deviceClient;
        }

        public async Task RemoveDeviceAsync()
        {
            using var rm = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            await rm.RemoveDeviceAsync(Id).ConfigureAwait(false);
        }
    }
}
