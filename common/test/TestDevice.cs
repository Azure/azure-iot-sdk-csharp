// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
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
                TestDevice ret = await CreateDeviceAsync(type, prefix).ConfigureAwait(false);

                s_log.WriteLine($"{nameof(GetTestDeviceAsync)}: Using device {ret.Id}.");
                return ret;
            }
            finally
            {
                s_semaphore.Release();
            }
        }

        private static async Task<TestDevice> CreateDeviceAsync(TestDeviceType type, string prefix)
        {
            string deviceName = prefix + Guid.NewGuid();
            s_log.WriteLine($"{nameof(GetTestDeviceAsync)}: Device with prefix {prefix} not found.");

            // Delete existing devices named this way and create a new one.
            using (var rm = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                s_log.WriteLine($"{nameof(GetTestDeviceAsync)}: Creating device {deviceName} with type {type}.");

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

                s_log.WriteLine($"{nameof(GetTestDeviceAsync)}: Pausing for {DelayAfterDeviceCreationSeconds}s after device was created.");
                await Task.Delay(DelayAfterDeviceCreationSeconds * 1000).ConfigureAwait(false);

                await rm.CloseAsync().ConfigureAwait(false);
                
                return new TestDevice(device, auth);
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
                s_log.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {_device.Id} from connection string: {transport} ID={TestLogging.IdOf(deviceClient)}");
            }
            else
            {
                deviceClient = DeviceClient.Create(IoTHubHostName, AuthenticationMethod, transport);
                s_log.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {_device.Id} from IAuthenticationMethod: {transport} ID={TestLogging.IdOf(deviceClient)}");
            }

            return deviceClient;
        }

        public DeviceClient CreateDeviceClient(ITransportSettings[] transportSettings, ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            DeviceClient deviceClient = null;

            if (_authenticationMethod == null)
            {
                if (authScope == ConnectionStringAuthScope.Device)
                {
                    deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, transportSettings);
                    s_log.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {_device.Id} from device connection string: ID={TestLogging.IdOf(deviceClient)}");
                }
                else
                {
                    deviceClient = DeviceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, _device.Id, transportSettings);
                    s_log.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {_device.Id} from IoTHub connection string: ID={TestLogging.IdOf(deviceClient)}");
                }
            }
            else
            {
                deviceClient = DeviceClient.Create(IoTHubHostName, AuthenticationMethod, transportSettings);
                s_log.WriteLine($"{nameof(CreateDeviceClient)}: Created {nameof(DeviceClient)} {_device.Id} from IAuthenticationMethod: ID={TestLogging.IdOf(deviceClient)}");
            }

            return deviceClient;
        }

        public static string GetHostName(string iotHubConnectionString)
        {
            Regex regex = new Regex("HostName=([^;]+)", RegexOptions.None);
            return regex.Match(iotHubConnectionString).Groups[1].Value;
        }
    }
}
