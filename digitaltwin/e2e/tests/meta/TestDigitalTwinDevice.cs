// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.DigitalTwin.Client;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.DigitalTwin.E2ETests.Meta
{
    internal class TestDigitalTwinDevice : IDisposable
    {
        private RegistryManager registryManager;
        public string digitalTwinId;
        private Devices.Device device;
        public DeviceClient deviceClient;

        public TestDigitalTwinDevice(String digitalTwinIdPrefix, Microsoft.Azure.Devices.Client.TransportType transportType)
        {
            registryManager = RegistryManager.CreateFromConnectionString(Configuration.IotHubConnectionString);

            digitalTwinId = digitalTwinIdPrefix + "-" + Guid.NewGuid();
            device = new Microsoft.Azure.Devices.Device(digitalTwinId);

            Task<Microsoft.Azure.Devices.Device> task = registryManager.AddDeviceAsync(device);
            task.Wait();
            device = task.Result;

            deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, transportType);
            digitalTwinClient = new DigitalTwinClient(deviceClient);
        }

        public string ConnectionString
        {
            get
            {
                string iotHubHostName = GetHostName(Configuration.IotHubConnectionString);
                return $"HostName={iotHubHostName};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";
            }
        }

        public DigitalTwinClient digitalTwinClient { get; }

        private static string GetHostName(string iotHubConnectionString)
        {
            Regex regex = new Regex("HostName=([^;]+)", RegexOptions.None);
            return regex.Match(iotHubConnectionString).Groups[1].Value;
        }

        public void Dispose()
        {
            deviceClient.CloseAsync().Wait();
            registryManager.RemoveDeviceAsync(digitalTwinId).Wait();
        }
    }
}
