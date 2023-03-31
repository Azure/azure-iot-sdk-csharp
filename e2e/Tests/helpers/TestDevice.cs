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

    internal sealed class TestDevice : IAsyncDisposable
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

        /// <summary>
        /// A single service client instance to be used by all the tests that simply need one initialized with a connection string.
        /// </summary>
        /// <remarks>
        /// It is better for tests to use a single instance so we don't run out of sockets due to the HttpClient dispose anti-pattern.
        /// <para>
        /// We won't dispose this client either, but it'll be fine because the test process will exit and memory leaks aren't a concern then.
        /// </para>
        /// </remarks>
        internal static IotHubServiceClient ServiceClient { get; } = new(TestConfiguration.IotHub.ConnectionString);

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
        internal static async Task<TestDevice> GetTestDeviceAsync(string namePrefix, TestDeviceType type = TestDeviceType.Sasl, CancellationToken ct = default)
        {
            TestDevice ret = await CreateDeviceAsync(type, $"{namePrefix}{type}_", ct).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"{nameof(GetTestDeviceAsync)}: Using device {ret.Id}.");
            return ret;
        }

        /// <summary>
        /// Used in conjunction with DeviceClient.CreateFromConnectionString()
        /// </summary>
        internal string ConnectionString
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
        internal string IotHubHostName { get; } = GetHostName(TestConfiguration.IotHub.ConnectionString);

        /// <summary>
        /// Device Id
        /// </summary>
        internal string Id => Device.Id;

        /// <summary>
        /// Device identity object.
        /// </summary>
        internal Device Device { get; set; }

        internal IotHubDeviceClient DeviceClient { get; private set; }

        internal IAuthenticationMethod AuthenticationMethod { get; private set; }

        internal IotHubDeviceClient CreateDeviceClient(IotHubClientOptions options = default, ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            if (AuthenticationMethod == null)
            {
                if (authScope == ConnectionStringAuthScope.Device)
                {
                    DeviceClient = new IotHubDeviceClient(ConnectionString, options);
                    VerboseTestLogger.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(IotHubDeviceClient)} {Device.Id} from device connection string");
                }
                else
                {
                    DeviceClient = new IotHubDeviceClient($"{TestConfiguration.IotHub.ConnectionString};DeviceId={Device.Id}", options);
                    VerboseTestLogger.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(IotHubDeviceClient)} {Device.Id} from IoTHub connection string");
                }
            }
            else
            {
                DeviceClient = new IotHubDeviceClient(IotHubHostName, AuthenticationMethod, options);
                VerboseTestLogger.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(IotHubDeviceClient)} {Device.Id} from IAuthenticationMethod");
            }

            return DeviceClient;
        }

        /// Sometimes the just newly created device can fail to connect if there is lag in the hub for the identity existing.
        /// Retry a few times to prevent test failures.
        /// </summary>
        internal async Task OpenWithRetryAsync(CancellationToken ct)
        {
            if (DeviceClient == null)
            {
                throw new InvalidOperationException("The device client has not been initialized. Call CreateDeviceClient() first.");
            }
            await OpenWithRetryAsync(DeviceClient, ct).ConfigureAwait(false);
        }

        /// <summary>
        internal static async Task OpenWithRetryAsync(IotHubDeviceClient client, CancellationToken ct)
        {
            int attempt = 1;
            while (true)
            {
                try
                {
                    VerboseTestLogger.WriteLine("Opening device client...");
                    await client.OpenAsync(ct).ConfigureAwait(false);
                    break;
                }
                catch (IotHubClientException ex) when (ex.ErrorCode == IotHubClientErrorCode.Unauthorized
                    && attempt++ < 4)
                {
                    VerboseTestLogger.WriteLine($"Attempt #{attempt} failed to open device connection due to {ex}");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }
        }

        internal async Task RemoveDeviceAsync(CancellationToken ct)
        {
            await RetryOperationHelper
                .RunWithHubServiceRetryAsync(
                    async () =>
                    {
                        await ServiceClient.Devices.DeleteAsync(Id).ConfigureAwait(false);
                    },
                    s_getRetryPolicy,
                    ct)
                .ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            if (DeviceClient != null)
            {
                await DeviceClient.DisposeAsync().ConfigureAwait(false);
            }

            // Normally we wouldn't be disposing the X509 Certificates here, but rather delegate that to whoever was creating the TestDevice.
            // For the design that our test suite follows, it is ok to dispose the X509 certificate here since it won't be referenced by anyone else
            // within the scope of the test using this TestDevice.
            if (_authCertificate is IDisposable disposableCert)
            {
                disposableCert?.Dispose();
            }
            _authCertificate = null;
            AuthenticationMethod = null;

            await RemoveDeviceAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private static async Task<TestDevice> CreateDeviceAsync(TestDeviceType type, string prefix, CancellationToken ct)
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
                        device = await ServiceClient.Devices.CreateAsync(requestDevice).ConfigureAwait(false);
                    },
                    s_createRetryPolicy,
                    ct)
                .ConfigureAwait(false);

            // Confirm the device exists in the registry before calling it good to avoid downstream test failures.
            await RetryOperationHelper
                .RunWithHubServiceRetryAsync(
                    async () =>
                    {
                        await ServiceClient.Devices.GetAsync(requestDevice.Id).ConfigureAwait(false);
                    },
                    s_getRetryPolicy,
                    ct)
                .ConfigureAwait(false);

            return device == null
                ? throw new Exception($"Exhausted attempts for creating device {device.Id}, requests got throttled.")
                : new TestDevice(device, auth)
                {
                    _authCertificate = authCertificate,
                };
        }
    }
}
