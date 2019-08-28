// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Azure.Iot.DigitalTwin.Device;
using Azure.Iot.DigitalTwin.Device.Model;
using Newtonsoft.Json;

namespace EnvironmentalSensorSample
{
    public class DeviceInformationInterface : DigitalTwinInterfaceClient
    {
        private const string deviceInformationInterfaceId = "urn:azureiot:DeviceManagement:DeviceInformation:1";
        private const string deviceInformationInterfaceName = "deviceInformation";
        private ICollection<DigitalTwinPropertyReport> propertyCollection;

        public DeviceInformationInterface() : base(deviceInformationInterfaceId, deviceInformationInterfaceName, false, false)
        {
            this.propertyCollection = new Collection<DigitalTwinPropertyReport>();
        }

        public void SetManufacturer(string value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(Constants.Manufacturer, JsonConvert.SerializeObject(value)));
        }

        public void SetModel(string value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(Constants.Model, JsonConvert.SerializeObject(value)));
        }

        public void SetSoftwareVersion(string value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(Constants.SoftwareVersion, JsonConvert.SerializeObject(value)));
        }

        public void SetOperatingSystemName(string value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(Constants.OperatingSystemName, JsonConvert.SerializeObject(value)));
        }

        public void SetProcessorArchitecture(string value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(Constants.ProcessorArchitecture, JsonConvert.SerializeObject(value)));
        }

        public void SetProcessorManufacturer(string value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(Constants.ProcessorManufacturer, JsonConvert.SerializeObject(value)));
        }

        public void SetTotalMemory(double value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(Constants.TotalMemory, value.ToString()));
        }

        public void SetTotalStorage(double value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(Constants.TotalStorage, value.ToString()));
        }

        public async Task SendAllPropertiesAsync()
        {
            await this.ReportPropertiesAsync(this.propertyCollection).ConfigureAwait(false);
            this.propertyCollection.Clear();
        }
    }
}
