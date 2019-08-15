// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.DigitalTwin.Client;
using Microsoft.Azure.Devices.DigitalTwin.Client.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnvironmentalSensorSample
{
    public class DeviceInformationInterface : DigitalTwinInterface
    {
        private static string s_deviceInformationInterfaceId = "urn:azureiot:DeviceInformation:1";
        private static string s_deviceInformationInterfaceName = "deviceinfo";
        private List<DigitalTwinProperty> propertyCollection;

        public DeviceInformationInterface() : base(s_deviceInformationInterfaceId, s_deviceInformationInterfaceName)
        {
            propertyCollection = new List<DigitalTwinProperty>();
        }

        public void SetFirmwareVersion(string value)
        {
            propertyCollection.Add(new DigitalTwinProperty(Constants.FirmwareVersion, DigitalTwinValue.CreateString(value)));
        }

        public void SetHardwareVersion(string value)
        {
            propertyCollection.Add(new DigitalTwinProperty(Constants.HardwareVersion, DigitalTwinValue.CreateString(value)));
        }

        public void SetManufacturer(string value)
        {
            propertyCollection.Add(new DigitalTwinProperty(Constants.Manufacturer, DigitalTwinValue.CreateString(value)));
        }

        public void SetModel(string value)
        {
            propertyCollection.Add(new DigitalTwinProperty(Constants.Model, DigitalTwinValue.CreateString(value)));
        }

        public void SetOriginalEquipmentManufacturer(string value)
        {
            propertyCollection.Add(new DigitalTwinProperty(Constants.OriginalEquipmentManufacturer, DigitalTwinValue.CreateString(value)));
        }

        public void SetOperatingSystemName(string value)
        {
            propertyCollection.Add(new DigitalTwinProperty(Constants.OperatingSystemName, DigitalTwinValue.CreateString(value)));
        }

        public void SetOperatingSystemVersion(string value)
        {
            propertyCollection.Add(new DigitalTwinProperty(Constants.OperatingSystemVersion, DigitalTwinValue.CreateString(value)));
        }

        public void SetProcessorArchitecture(string value)
        {
            propertyCollection.Add(new DigitalTwinProperty(Constants.ProcessorArchitecture, DigitalTwinValue.CreateString(value)));
        }

        public void SetProcessorType(string value)
        {
            propertyCollection.Add(new DigitalTwinProperty(Constants.ProcessorType, DigitalTwinValue.CreateString(value)));
        }

        public void SetSerialNumber(string value)
        {
            propertyCollection.Add(new DigitalTwinProperty(Constants.SerialNumber, DigitalTwinValue.CreateString(value)));
        }

        public void SetTotalMemory(double value)
        {
            propertyCollection.Add(new DigitalTwinProperty(Constants.TotalMemory, DigitalTwinValue.CreateDouble(value)));
        }

        public void SetTotalStorage(double value)
        {
            propertyCollection.Add(new DigitalTwinProperty(Constants.TotalStorage, DigitalTwinValue.CreateDouble(value)));
        }

        #region Read-Only properties
        public async Task SendAllPropertiesAsync()
        {
            await ReportPropertiesAsync(propertyCollection).ConfigureAwait(false);
            propertyCollection.Clear();
        }
        #endregion
    }
}
