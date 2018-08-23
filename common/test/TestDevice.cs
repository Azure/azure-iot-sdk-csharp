// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        private Device _device;
        private Client.IAuthenticationMethod _authenticationMethod;

        private TestDevice(Device device, Client.IAuthenticationMethod authenticationMethod)
        {
            _device = device;
            _authenticationMethod = authenticationMethod;
        }

        /// <summary>
        /// Factory method.
        /// IMPORTANT: Not thread safe!
        /// </summary>
        /// <param name="namePrefix"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static async Task<TestDevice> GetTestDeviceAsync(string namePrefix, TestDeviceType type = TestDeviceType.Sasl)
        {
            string prefix = namePrefix + type + "_";

            if (!s_deviceCache.TryGetValue(prefix, out TestDevice testDevice))
            {
                string deviceName = prefix + Guid.NewGuid();
                s_log.WriteLine($"{nameof(GetTestDeviceAsync)}: Device with prefix {prefix} not found. Removing old devices.");

                // Delete existing devices named this way and create a new one.
                RegistryManager rm = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
                await RemoveDevicesAsync(prefix, rm).ConfigureAwait(false);
                                
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

            TestDevice ret = s_deviceCache[prefix];

            s_log.WriteLine($"{nameof(GetTestDeviceAsync)}: Using device {ret.Id}.");
            return ret;
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
                s_log.WriteLine($"{nameof(CreateDeviceClient)}: Creating {nameof(DeviceClient)} from connection string: {transport}");
                deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, transport);
            }
            else
            {
                s_log.WriteLine($"{nameof(CreateDeviceClient)}: Create {nameof(DeviceClient)} from IAuthenticationMethod: {transport}");
                deviceClient = DeviceClient.Create(IoTHubHostName, AuthenticationMethod, transport);
            }

            return deviceClient;
        }

        private static string GetHostName(string iotHubConnectionString)
        {
            Regex regex = new Regex("HostName=([^;]+)", RegexOptions.None);
            return regex.Match(iotHubConnectionString).Groups[1].Value;
        }

        private static async Task RemoveDevicesAsync(string devicePrefix, RegistryManager rm)
        {
            s_log.WriteLine($"{nameof(RemoveDevicesAsync)} Enumerating devices.");

            IQuery q = rm.CreateQuery("SELECT * FROM devices", 100);
            while (q.HasMoreResults)
            {
                IEnumerable<Twin> results = await q.GetNextAsTwinAsync().ConfigureAwait(false);
                foreach (Twin t in results)
                {
                    if (t.DeviceId.StartsWith(devicePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        s_log.WriteLine($"{nameof(RemoveDevicesAsync)} Removing device: {t.DeviceId}");
                        await rm.RemoveDeviceAsync(t.DeviceId).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
