﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceExplorer
{
    class DevicesProcessor
    {
        private List<DeviceEntity> listOfDevices;
        private RegistryManager registryManager;
        private String iotHubConnectionString;
        private int maxCountOfDevices;
        private String protocolGatewayHostName;
        private Task getDeviceDetailsTask;

        public DevicesProcessor(string iotHubConnenctionString, int devicesCount, string protocolGatewayName)
        {
            this.listOfDevices = new List<DeviceEntity>();
            this.iotHubConnectionString = iotHubConnenctionString;
            this.maxCountOfDevices = devicesCount;
            this.protocolGatewayHostName = protocolGatewayName;
            this.registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
        }

        public async Task<List<DeviceEntity>> GetDevices()
        {
            try
            {
                DeviceEntity deviceEntity;
                IQuery query = registryManager.CreateQuery("select * from devices", null); ;

                while (query.HasMoreResults)
                {
                    IEnumerable<Twin> page = await query.GetNextAsTwinAsync();
                    foreach (Twin twin in page)
                    {
                        deviceEntity = new DeviceEntity()
                        {
                            Id = twin.DeviceId,
                            ConnectionState = twin.ConnectionState.ToString(),
                            LastActivityTime = twin.LastActivityTime,
                            LastStateUpdatedTime = twin.StatusUpdatedTime,
                            MessageCount = twin.CloudToDeviceMessageCount,
                            State = twin.Status.ToString(),
                            SuspensionReason = twin.StatusReason,

                        };

                        deviceEntity.PrimaryThumbPrint = twin.X509Thumbprint?.PrimaryThumbprint;
                        deviceEntity.SecondaryThumbPrint = twin.X509Thumbprint?.SecondaryThumbprint;

                        listOfDevices.Add(deviceEntity);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (getDeviceDetailsTask == null) getDeviceDetailsTask = GetDeviceDetailsAsync();

            return listOfDevices;
        }

        public async Task GetDeviceDetailsAsync()
        {
            try
            {
                DeviceEntity deviceEntity;
                IQuery query = registryManager.CreateQuery("select * from devices", null);

                while (query.HasMoreResults)
                {
                    IEnumerable<Twin> page = await query.GetNextAsTwinAsync();
                    foreach (Twin twin in page)
                    {
                        Device device = await registryManager.GetDeviceAsync(twin.DeviceId);

                        deviceEntity = this.listOfDevices.Find((e) => { return twin.DeviceId == e.Id; });

                        if (deviceEntity == null) continue;

                        deviceEntity.ConnectionString = CreateDeviceConnectionString(device);
                        deviceEntity.LastConnectionStateUpdatedTime = device.ConnectionStateUpdatedTime;

                        deviceEntity.PrimaryThumbPrint = twin.X509Thumbprint?.PrimaryThumbprint;
                        deviceEntity.SecondaryThumbPrint = twin.X509Thumbprint?.SecondaryThumbprint;

                        deviceEntity.PrimaryKey = device.Authentication?.SymmetricKey?.PrimaryKey;
                        deviceEntity.SecondaryKey = device.Authentication?.SymmetricKey?.SecondaryKey;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                this.getDeviceDetailsTask = null;
            }
        }

        private String CreateDeviceConnectionString(Device device)
        {
            StringBuilder deviceConnectionString = new StringBuilder();

            var hostName = String.Empty;
            var tokenArray = iotHubConnectionString.Split(';');
            for (int i = 0; i < tokenArray.Length; i++)
            {
                var keyValueArray = tokenArray[i].Split('=');
                if (keyValueArray[0] == "HostName")
                {
                    hostName =  tokenArray[i] + ';';
                    break;
                }
            }

            if (!String.IsNullOrWhiteSpace(hostName))
            {
                deviceConnectionString.Append(hostName);
                deviceConnectionString.AppendFormat("DeviceId={0}", device.Id);

                if (device.Authentication != null)
                {
                    if ((device.Authentication.SymmetricKey != null) && (device.Authentication.SymmetricKey.PrimaryKey != null))
                    {
                        deviceConnectionString.AppendFormat(";SharedAccessKey={0}", device.Authentication.SymmetricKey.PrimaryKey);
                    }
                    else
                    {
                        deviceConnectionString.AppendFormat(";x509=true");
                    }
                }

                if (this.protocolGatewayHostName.Length > 0)
                {
                    deviceConnectionString.AppendFormat(";GatewayHostName=ssl://{0}:8883", this.protocolGatewayHostName);
                }
            }
            
            return deviceConnectionString.ToString();
        }
        // For testing without connecting to a live service
        static public List<DeviceEntity> GetDevicesForTest()
        {
            List<DeviceEntity> deviceList;
            deviceList = new List<DeviceEntity>();
            deviceList.Add(new DeviceEntity() { Id = "TestDevice01", PrimaryKey = "TestPrimKey01", SecondaryKey = "TestSecKey01" });
            deviceList.Add(new DeviceEntity() { Id = "TestDevice02", PrimaryKey = "TestPrimKey02", SecondaryKey = "TestSecKey02" });
            deviceList.Add(new DeviceEntity() { Id = "TestDevice03", PrimaryKey = "TestPrimKey03", SecondaryKey = "TestSecKey03" });
            deviceList.Add(new DeviceEntity() { Id = "TestDevice04", PrimaryKey = "TestPrimKey04", SecondaryKey = "TestSecKey04" });
            deviceList.Add(new DeviceEntity() { Id = "TestDevice05", PrimaryKey = "TestPrimKey05", SecondaryKey = "TestSecKey05" });
            return deviceList;
        }
    }
}
