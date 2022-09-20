// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
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
        private const int MaxRetryCount = 5;
        private static readonly HashSet<Type> s_throttlingExceptions = new HashSet<Type> { typeof(ThrottlingException), };
        private static readonly HashSet<Type> s_getRetryableExceptions = new HashSet<Type>(s_throttlingExceptions) { typeof(DeviceNotFoundException) };
        private static readonly SemaphoreSlim s_semaphore = new SemaphoreSlim(1, 1);

        private static readonly IRetryPolicy s_exponentialBackoffRetryStrategy = new ExponentialBackoff(
            retryCount: MaxRetryCount,
            minBackoff: TimeSpan.FromMilliseconds(100),
            maxBackoff: TimeSpan.FromSeconds(10),
            deltaBackoff: TimeSpan.FromMilliseconds(100));

        private X509Certificate2 _authCertificate;

        private static MsTestLogger s_logger;

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
            s_logger = logger;
            string prefix = namePrefix + type + "_";

            try
            {
                await s_semaphore.WaitAsync().ConfigureAwait(false);
                TestDevice ret = await CreateDeviceAsync(type, prefix).ConfigureAwait(false);

                s_logger.Trace($"{nameof(GetTestDeviceAsync)}: Using device {ret.Id}.");
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
            using var rm = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            Client.IAuthenticationMethod auth = null;

            var requestDevice = new Device(deviceName);
            X509Certificate2 authCertificate = null;

            if (type == TestDeviceType.X509)
            {
                requestDevice.Authentication = new AuthenticationMechanism
                {
                    X509Thumbprint = new X509Thumbprint
                    {
                        PrimaryThumbprint = TestConfiguration.IotHub.GetCertificateWithPrivateKey().Thumbprint
                    }
                };

#pragma warning disable CA2000 // Dispose objects before losing scope - X509Certificate and DeviceAuthenticationWithX509Certificate are disposed when TestDevice is disposed.
                authCertificate = TestConfiguration.IotHub.GetCertificateWithPrivateKey();
                auth = new DeviceAuthenticationWithX509Certificate(deviceName, authCertificate);
#pragma warning restore CA2000 // Dispose objects before losing scope - X509Certificate and DeviceAuthenticationWithX509Certificate are disposed when TestDevice is disposed.
            }

            Device device = null;

            await RetryOperationHelper
                .RetryOperationsAsync(
                    async () =>
                    {
                        device = await rm.AddDeviceAsync(requestDevice).ConfigureAwait(false);
                    },
                    s_exponentialBackoffRetryStrategy,
                    s_throttlingExceptions,
                    s_logger)
                .ConfigureAwait(false);

            // Confirm the device exists in the registry before calling it good to avoid downstream test failures.
            await RetryOperationHelper
                .RetryOperationsAsync(
                    async () =>
                    {
                        device = await rm.GetDeviceAsync(requestDevice.Id).ConfigureAwait(false);
                        if (device is null)
                        {
                            throw new DeviceNotFoundException(ErrorCode.DeviceNotFound, $"Created device {requestDevice.Id} not yet gettable from IoT hub.");
                        }
                    },
                    s_exponentialBackoffRetryStrategy,
                    s_getRetryableExceptions,
                    s_logger)
                .ConfigureAwait(false);

            await rm.CloseAsync().ConfigureAwait(false);

            return device == null
                ? throw new Exception($"Exhausted attempts for creating device {device.Id}, requests got throttled.")
                : new TestDevice(device, auth)
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
                string iotHubHostName = GetHostName(TestConfiguration.IotHub.ConnectionString);
                return $"HostName={iotHubHostName};DeviceId={Device.Id};SharedAccessKey={Device.Authentication.SymmetricKey.PrimaryKey}";
            }
        }

        /// <summary>
        /// Used in conjunction with DeviceClient.Create()
        /// </summary>
        public string IotHubHostName => GetHostName(TestConfiguration.IotHub.ConnectionString);

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
                s_logger.Trace($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {Device.Id} from connection string: {transport} ID={TestLogger.IdOf(deviceClient)}");
            }
            else
            {
                deviceClient = DeviceClient.Create(IotHubHostName, AuthenticationMethod, transport, options);
                s_logger.Trace($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {Device.Id} from IAuthenticationMethod: {transport} ID={TestLogger.IdOf(deviceClient)}");
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
                    s_logger.Trace($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {Device.Id} from device connection string: ID={TestLogger.IdOf(deviceClient)}");
                }
                else
                {
                    deviceClient = DeviceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString, Device.Id, transportSettings, options);
                    s_logger.Trace($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {Device.Id} from IoTHub connection string: ID={TestLogger.IdOf(deviceClient)}");
                }
            }
            else
            {
                deviceClient = DeviceClient.Create(IotHubHostName, AuthenticationMethod, transportSettings, options);
                s_logger.Trace($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {Device.Id} from IAuthenticationMethod: ID={TestLogger.IdOf(deviceClient)}");
            }

            return deviceClient;
        }

        public async Task RemoveDeviceAsync()
        {
            using var rm = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            await RetryOperationHelper
                .RetryOperationsAsync(
                    async () =>
                    {
                        await rm.RemoveDeviceAsync(Id).ConfigureAwait(false);
                    },
                    s_exponentialBackoffRetryStrategy,
                    s_throttlingExceptions,
                    s_logger)
                .ConfigureAwait(false);
        }

        public void Dispose()
        {
            // X509Certificate needs to be disposed for implementations !NET451 (NET451 doesn't implement X509Certificates as IDisposable).

            // Normally we wouldn't be disposing the X509 Certificates here, but rather delegate that to whoever was creating the TestDevice.
            // For the design that our test suite follows, it is ok to dispose the X509 certificate here since it won't be referenced by anyone else
            // within the scope of the test using this TestDevice.
            if (_authCertificate is IDisposable disposableCert)
            {
                disposableCert?.Dispose();
            }
            _authCertificate = null;

            if (AuthenticationMethod is DeviceAuthenticationWithX509Certificate x509Auth)
            {
                x509Auth?.Dispose();
            }
            AuthenticationMethod = null;
        }
    }
}
