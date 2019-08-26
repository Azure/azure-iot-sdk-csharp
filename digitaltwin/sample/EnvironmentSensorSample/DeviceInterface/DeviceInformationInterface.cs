// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Iot.DigitalTwin.Device;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentalSensorSample
{
    public class DeviceInformationInterface : DigitalTwinInterfaceClient
    {
        private static string s_deviceInformationInterfaceId = "urn:azureiot:DeviceManagement:DeviceInformation:1";
        private static string s_deviceInformationInterfaceName = "deviceInformation";
        private Dictionary<string, object> propertyCollection;

        public DeviceInformationInterface() : base(s_deviceInformationInterfaceId, s_deviceInformationInterfaceName)
        {
            propertyCollection = new Dictionary<string, object>();
        }

        public void SetManufacturer(string value)
        {
            propertyCollection.Add(Constants.Manufacturer, value);
        }

        public void SetModel(string value)
        {
            propertyCollection.Add(Constants.Model, value);
        }

        public void SetSoftwareVersion(string value)
        {
            propertyCollection.Add(Constants.SoftwareVersion, value);
        }

        public void SetOperatingSystemName(string value)
        {
            propertyCollection.Add(Constants.OperatingSystemName, value);
        }

        public void SetProcessorArchitecture(string value)
        {
            propertyCollection.Add(Constants.ProcessorArchitecture, value);
        }
        public void SetProcessorManufacturer(string value)
        {
            propertyCollection.Add(Constants.ProcessorManufacturer, value);
        }

        public void SetTotalMemory(double value)
        {
            propertyCollection.Add(Constants.TotalMemory, value);
        }

        public void SetTotalStorage(double value)
        {
            propertyCollection.Add(Constants.TotalStorage, value);
        }

        #region Read-Only properties
        public async Task SendAllPropertiesAsync()
        {
            string output = JsonConvert.SerializeObject(propertyCollection);
            await ReportPropertiesAsync(new Memory<byte>(Encoding.UTF8.GetBytes(output))).ConfigureAwait(false);
            propertyCollection.Clear();
        }
        #endregion
    }
}
