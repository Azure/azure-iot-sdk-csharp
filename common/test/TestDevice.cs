// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Azure.Devices.E2ETests
{
    public enum TestDeviceType
    {
        Sasl,
        X509
    }

    public class TestDevice
    {
        private const int DelayAfterDeviceCreationSeconds = 3;
        private static Dictionary<string, TestDevice> s_deviceCache = new Dictionary<string, TestDevice>();
        private static TestLogging s_log = TestLogging.GetInstance();
        private static SemaphoreSlim s_semaphore = new SemaphoreSlim(1, 1);

        private Device _device;
        private Client.IAuthenticationMethod _authenticationMethod;

        private TestDevice(Device device, Client.IAuthenticationMethod authenticationMethod)
        {
            _device = device;
            _authenticationMethod = authenticationMethod;
        }

        /// <summary>
        /// Factory method.
        /// </summary>
        /// <param name="namePrefix"></param>
        /// <param name="type"></param>
        public static async Task<TestDevice> GetTestDeviceAsync(string namePrefix, TestDeviceType type = TestDeviceType.Sasl)
        {
            string prefix = namePrefix + type + "_";

            try
            {
                await s_semaphore.WaitAsync().ConfigureAwait(false);
                if (!s_deviceCache.TryGetValue(prefix, out TestDevice testDevice))
                {
                    await CreateDeviceAsync(type, prefix).ConfigureAwait(false);
                }

                TestDevice ret = s_deviceCache[prefix];

                s_log.WriteLine($"{nameof(GetTestDeviceAsync)}: Using device {ret.Id}.");
                return ret;
            }
            finally
            {
                s_semaphore.Release();
            }
        }

        private static async Task CreateDeviceAsync(TestDeviceType type, string prefix)
        {
            string deviceName = prefix + Guid.NewGuid();
            s_log.WriteLine($"{nameof(GetTestDeviceAsync)}: Device with prefix {prefix} not found.");

            // Delete existing devices named this way and create a new one.
            using (RegistryManager rm = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                s_log.WriteLine($"{nameof(GetTestDeviceAsync)}: Creating device {deviceName} with type {type}.");

                Client.IAuthenticationMethod auth = null;

                Device requestDevice = new Device(deviceName);
                if (type == TestDeviceType.X509)
                {
                    requestDevice.Authentication = new AuthenticationMechanism()
                    {
                        X509Thumbprint = new X509Thumbprint()
                        {
                            PrimaryThumbprint = Configuration.IoTHub.GetCertificateWithPrivateKey().Thumbprint
                        }
                    };

                    auth = new DeviceAuthenticationWithX509Certificate(deviceName, Configuration.IoTHub.GetCertificateWithPrivateKey());
                }

                Device device = await rm.AddDeviceAsync(requestDevice).ConfigureAwait(false);

                s_log.WriteLine($"{nameof(GetTestDeviceAsync)}: Pausing for {DelayAfterDeviceCreationSeconds}s after device was created.");
                await Task.Delay(DelayAfterDeviceCreationSeconds * 1000).ConfigureAwait(false);

                s_deviceCache[prefix] = new TestDevice(device, auth);

                await rm.CloseAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Used in conjunction with DeviceClient.CreateFromConnectionString()
        /// </summary>
        public string ConnectionString
        {
            get
            {
                string iotHubHostName = GetHostName(Configuration.IoTHub.ConnectionString);
                return $"HostName={iotHubHostName};DeviceId={_device.Id};SharedAccessKey={_device.Authentication.SymmetricKey.PrimaryKey}";
            }
        }

        /// <summary>
        /// Used in conjunction with DeviceClient.Create()
        /// </summary>
        public string IoTHubHostName
        {
            get
            {
                return GetHostName(Configuration.IoTHub.ConnectionString);
            }
        }

        /// <summary>
        /// Device ID
        /// </summary>
        public string Id
        {
            get
            {
                return _device.Id;
            }
        }

        /// <summary>
        /// Device identity object.
        /// </summary>
        public Device Identity
        {
            get
            {
                return _device;
            }
        }

        public Client.IAuthenticationMethod AuthenticationMethod
        {
            get
            {
                return _authenticationMethod;
            }
        }

        public DeviceClient CreateDeviceClient(Client.TransportType transport)
        {
            DeviceClient deviceClient = null;

            if (_authenticationMethod == null)
            {
                deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, transport);
                s_log.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} from connection string: {transport} ID={TestLogging.IdOf(deviceClient)}");
            }
            else
            {
                deviceClient = DeviceClient.Create(IoTHubHostName, AuthenticationMethod, transport);
                s_log.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} from IAuthenticationMethod: {transport} ID={TestLogging.IdOf(deviceClient)}");
            }

            return deviceClient;
        }

        private static string GetHostName(string iotHubConnectionString)
        {
            Regex regex = new Regex("HostName=([^;]+)", RegexOptions.None);
            return regex.Match(iotHubConnectionString).Groups[1].Value;
        }
    }
}
