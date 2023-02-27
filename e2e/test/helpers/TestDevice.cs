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
        X509,
    }

    public enum ConnectionStringAuthScope
    {
        IotHub,
        Device,
    }

    public sealed class TestDevice : IDisposable
    {
        private static readonly IIotHubServiceRetryPolicy s_createRetryPolicy = new IotHubServiceExponentialBackoffRetryPolicy(0, TimeSpan.FromMinutes(1), true);
        private static readonly IIotHubServiceRetryPolicy s_getRetryPolicy = new HubServiceTestRetryPolicy(
            new()
            {
                IotHubServiceErrorCode.DeviceNotFound,
                IotHubServiceErrorCode.ModuleNotFound,
                IotHubServiceErrorCode.ThrottlingBacklogTimeout,
            });

        private X509Certificate2 _authCertificate;
        private static readonly IotHubServiceClient _client = new(TestConfiguration.IotHub.ConnectionString);

        private TestDevice(Device device, IAuthenticationMethod authenticationMethod)
        {
            Device = device;
            AuthenticationMethod = authenticationMethod;
        }

        /// <summary>
        /// Factory method.
        /// </summary>
        /// <param name="namePrefix">The prefix to apply to your device name</param>
        /// <param name="type">The way the device will authenticate</param>
        public static async Task<TestDevice> GetTestDeviceAsync(string namePrefix, TestDeviceType type = TestDeviceType.Sasl)
        {
            TestDevice ret = await CreateDeviceAsync(type, $"{namePrefix}{type}_").ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"{nameof(GetTestDeviceAsync)}: Using device {ret.Id}.");
            return ret;
        }

        private static async Task<TestDevice> CreateDeviceAsync(TestDeviceType type, string prefix)
        {
            string deviceName = $"E2E_{prefix}{Guid.NewGuid()}";

            // Delete existing devices named this way and create a new one.
            VerboseTestLogger.WriteLine($"{nameof(GetTestDeviceAsync)}: Creating device {deviceName} with type {type}.");

            IAuthenticationMethod auth = null;

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

#pragma warning disable CA2000 // Dispose objects before losing scope - X509Certificate and ClientAuthenticationWithX509Certificate are disposed when TestDevice is disposed.
                authCertificate = TestConfiguration.IotHub.GetCertificateWithPrivateKey();
                auth = new ClientAuthenticationWithX509Certificate(authCertificate, deviceName);
#pragma warning restore CA2000 // Dispose objects before losing scope - X509Certificate and ClientAuthenticationWithX509Certificate are disposed when TestDevice is disposed.
            }

            Device device = null;

            await RetryOperationHelper
                .RunWithHubServiceRetryAsync(
                    async () =>
                    {
                        device = await _client.Devices.CreateAsync(requestDevice).ConfigureAwait(false);
                    },
                    s_createRetryPolicy,
                    CancellationToken.None)
                .ConfigureAwait(false);

            // Confirm the device exists in the registry before calling it good to avoid downstream test failures.
            await RetryOperationHelper
                .RunWithHubServiceRetryAsync(
                    async () =>
                    {
                        await _client.Devices.GetAsync(requestDevice.Id).ConfigureAwait(false);
                    },
                    s_getRetryPolicy,
                    CancellationToken.None)
                .ConfigureAwait(false);

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
        public string IotHubHostName { get; } = GetHostName(TestConfiguration.IotHub.ConnectionString);

        /// <summary>
        /// Device Id
        /// </summary>
        public string Id => Device.Id;

        /// <summary>
        /// Device identity object.
        /// </summary>
        public Device Device { get; private set; }

        public IAuthenticationMethod AuthenticationMethod { get; private set; }

        public IotHubDeviceClient CreateDeviceClient(IotHubClientOptions options = default, ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            IotHubDeviceClient deviceClient = null;

            if (AuthenticationMethod == null)
            {
                if (authScope == ConnectionStringAuthScope.Device)
                {
                    deviceClient = new IotHubDeviceClient(ConnectionString, options);
                    VerboseTestLogger.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(IotHubDeviceClient)} {Device.Id} from device connection string");
                }
                else
                {
                    deviceClient = new IotHubDeviceClient($"{TestConfiguration.IotHub.ConnectionString};DeviceId={Device.Id}", options);
                    VerboseTestLogger.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(IotHubDeviceClient)} {Device.Id} from IoTHub connection string");
                }
            }
            else
            {
                deviceClient = new IotHubDeviceClient(IotHubHostName, AuthenticationMethod, options);
                VerboseTestLogger.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(IotHubDeviceClient)} {Device.Id} from IAuthenticationMethod");
            }

            return deviceClient;
        }

        public async Task RemoveDeviceAsync()
        {
            await RetryOperationHelper
                .RunWithHubServiceRetryAsync(
                    async () =>
                    {
                        await _client.Devices.DeleteAsync(Id).ConfigureAwait(false);
                    },
                    s_getRetryPolicy,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        public void Dispose()
        {
            // Normally we wouldn't be disposing the X509 Certificates here, but rather delegate that to whoever was creating the TestDevice.
            // For the design that our test suite follows, it is ok to dispose the X509 certificate here since it won't be referenced by anyone else
            // within the scope of the test using this TestDevice.
            if (_authCertificate is IDisposable disposableCert)
            {
                disposableCert?.Dispose();
            }
            _authCertificate = null;
            AuthenticationMethod = null;
        }
    }
}
