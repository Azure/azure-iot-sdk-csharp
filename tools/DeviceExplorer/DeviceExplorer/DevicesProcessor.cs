// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace DeviceExplorer
{
    class DevicesProcessor
    {
        private RegistryManager registryManager;
        private String iotHubConnectionString;
        private int maxCountOfDevices;
        private String protocolGatewayHostName;

        public DevicesProcessor(string iotHubConnenctionString, int devicesCount, string protocolGatewayName)
        {
            this.iotHubConnectionString = iotHubConnenctionString;
            this.maxCountOfDevices = devicesCount;
            this.protocolGatewayHostName = protocolGatewayName;
            this.registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
        }

        /// <summary>
        /// This method took well over an hour to load 120,000 devices
        /// </summary>
        /// <returns></returns>
        public async Task GetDeviceSample(
            int sampleSize, 
            SortableBindingList<DeviceEntity> list)
        {
            try
            {
                // Get the first sampleSize of devices, starting at the beginning
                List<Twin> page = await GetDevicesFromPage(0, sampleSize);
                foreach (Twin twin in page)
                {
                    DeviceEntity deviceEntity = await FetchDeviceFromTwin(twin);
                    list.Add(deviceEntity);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<List<Twin>> GetDevicesFromPage(int pageNumber, int pageSize)
        {
            IQuery query = registryManager.CreateQuery("SELECT * FROM devices", pageSize);

            string pageToken = "skip=" + pageNumber * pageSize + "&total=" + pageSize;
            string continuationToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(pageToken));

            Task<QueryResponse<Twin>> pageTask = query.GetNextAsTwinAsync(new QueryOptions { ContinuationToken = continuationToken });
            await pageTask.ConfigureAwait(false);
            return pageTask.Result.ToList();
        }

        public async Task<int> GetDeviceCount()
        {
            const string propName = "numberOfDevices";
            IQuery query = registryManager.CreateQuery($"SELECT COUNT() AS {propName} FROM devices");
            string json = (await query.GetNextAsJsonAsync()).FirstOrDefault();
            JObject jObj = JObject.Parse(json);
            return (int)jObj[propName];
        }

        private string ParseDeviceIdFromJson(string deviceIdJson)
        {
            // deviceIdJson = <space>"{\r\n  \"DeviceId\": \"Simulated-100004\"\r\n}"
            string[] tokens = deviceIdJson.Split(':'); 
            // tokens[1] = <space>"Simulated-100004"\r\n}
            return tokens[1].Trim().TrimStart('"').TrimEnd('"', '\r', '\n', '}');
        }

        public async Task<DeviceEntity> GetDeviceById(string deviceId)
        {
            Device device = await registryManager.GetDeviceAsync(deviceId);
            return device == null ? null : MapDeviceToDeviceEntity(device);

            // IQuery query = registryManager.CreateQuery($"SELECT * FROM devices WHERE deviceId = '{deviceId}'");
            // Twin twin = (await query.GetNextAsTwinAsync()).FirstOrDefault();
            // return await FetchDeviceFromTwin(twin);
        }

        private async Task<DeviceEntity> FetchDeviceFromTwin(Twin twin)
        {
            Device device = await registryManager.GetDeviceAsync(twin.DeviceId);
            return MapDeviceToDeviceEntity(device, twin);
        }

        private DeviceEntity MapDeviceToDeviceEntity(Device device, Twin twin=null)
        {
            return new DeviceEntity
            {
                Id = device.Id,
                PrimaryKey = device.Authentication?.SymmetricKey.PrimaryKey,
                SecondaryKey = device.Authentication?.SymmetricKey.SecondaryKey,
                PrimaryThumbPrint = twin?.X509Thumbprint?.PrimaryThumbprint,
                SecondaryThumbPrint = twin?.X509Thumbprint?.SecondaryThumbprint,
                ConnectionState = device.ConnectionState.ToString(),
                ConnectionString = CreateDeviceConnectionString(device),
                LastActivityTime = device.LastActivityTime,
                LastStateUpdatedTime = device.StatusUpdatedTime,
                MessageCount = device.CloudToDeviceMessageCount,
                State = device.Status.ToString(),
                SuspensionReason = device.StatusReason,
                LastConnectionStateUpdatedTime = device.ConnectionStateUpdatedTime,
            };
        }

        private string CreateDeviceConnectionString(Device device)
        {
            var deviceConnectionString = new StringBuilder();

            var hostName = string.Empty;

            string[] tokenArray = iotHubConnectionString.Split(';');
            for (int i = 0; i < tokenArray.Length; i++)
            {
                var keyValueArray = tokenArray[i].Split('=');
                if (keyValueArray[0] == "HostName")
                {
                    hostName =  tokenArray[i] + ';';
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(hostName)) return deviceConnectionString.ToString();

            deviceConnectionString.Append(hostName);
            deviceConnectionString.AppendFormat("DeviceId={0}", device.Id);

            if (device.Authentication != null)
            {
                if (device.Authentication.SymmetricKey?.PrimaryKey != null)
                {
                    deviceConnectionString.AppendFormat(
                        ";SharedAccessKey={0}", 
                        device.Authentication.SymmetricKey.PrimaryKey);
                }
                else
                {
                    deviceConnectionString.AppendFormat(";x509=true");
                }
            }

            if (this.protocolGatewayHostName.Length > 0)
            {
                deviceConnectionString.AppendFormat(
                    ";GatewayHostName=ssl://{0}:8883", 
                    this.protocolGatewayHostName);
            }

            return deviceConnectionString.ToString();
        }
        // For testing without connecting to a live service
        public static List<DeviceEntity> GetDevicesForTest()
        {
           return new List<DeviceEntity>
           {
               new DeviceEntity { Id = "TestDevice01", PrimaryKey = "TestPrimKey01", SecondaryKey = "TestSecKey01" },
               new DeviceEntity { Id = "TestDevice02", PrimaryKey = "TestPrimKey02", SecondaryKey = "TestSecKey02" },
               new DeviceEntity { Id = "TestDevice03", PrimaryKey = "TestPrimKey03", SecondaryKey = "TestSecKey03" },
               new DeviceEntity { Id = "TestDevice04", PrimaryKey = "TestPrimKey04", SecondaryKey = "TestSecKey04" },
               new DeviceEntity { Id = "TestDevice05", PrimaryKey = "TestPrimKey05", SecondaryKey = "TestSecKey05" },
           };

        }
    }
}
