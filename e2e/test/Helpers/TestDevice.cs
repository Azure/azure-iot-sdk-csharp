// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
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

    public class TestDevice : IDisposable
    {
        private const int DelayAfterDeviceCreationSeconds = 0;
        private static readonly SemaphoreSlim s_semaphore = new SemaphoreSlim(1, 1);

        private X509Certificate2 _authCertificate;

        private static MsTestLogger _logger;

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
            X509Certificate2 authCertificate = null;

            if (type == TestDeviceType.X509)
            {
                requestDevice.Authentication = new AuthenticationMechanism
                {
                    X509Thumbprint = new X509Thumbprint
                    {
                        PrimaryThumbprint = Configuration.IoTHub.GetCertificateWithPrivateKey().Thumbprint
                    }
                };

                authCertificate = Configuration.IoTHub.GetCertificateWithPrivateKey();
#pragma warning disable CA2000 // Dispose objects before losing scope - auth is disposed when TestDevice is disposed.
                auth = new DeviceAuthenticationWithX509Certificate(deviceName, authCertificate);
#pragma warning restore CA2000 // Dispose objects before losing scope - auth is disposed when TestDevice is disposed.
            }

            Device device = await rm.AddDeviceAsync(requestDevice).ConfigureAwait(false);

            _logger.Trace($"{nameof(GetTestDeviceAsync)}: Pausing for {DelayAfterDeviceCreationSeconds}s after device was created.");
            await Task.Delay(DelayAfterDeviceCreationSeconds * 1000).ConfigureAwait(false);

            await rm.CloseAsync().ConfigureAwait(false);

            return new TestDevice(device, auth)
            {
                _authCertificate = authCertificate,
            };
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

        public void Dispose()
        {
#if !NET451
            // Ideally we wouldn't be disposing the X509 Certificates here, but rather delegate that to whoever was creating the TestDevice.
            // For the design that our test suite follows, it is ok to dispose the X509 certificate here since it won't be referenced by anyone else
            // within the scope of the test using this TestDevice.
            _authCertificate?.Dispose();
            _authCertificate = null;
#endif

            if (AuthenticationMethod is DeviceAuthenticationWithX509Certificate x509Auth)
            {
                x509Auth?.Dispose();
                x509Auth = null;
            }

        }
    }
}
