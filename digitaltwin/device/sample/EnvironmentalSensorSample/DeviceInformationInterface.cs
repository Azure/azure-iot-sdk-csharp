// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Microsoft.Azure.IoT.DigitalTwin.Device;
using Microsoft.Azure.IoT.DigitalTwin.Device.Model;
using Newtonsoft.Json;

namespace EnvironmentalSensorSample
{
    /// <summary>
    /// Sample of DeviceInformationInterface.
    /// </summary>
    public class DeviceInformationInterface : DigitalTwinInterfaceClient
    {
        private const string DeviceInformationInterfaceId = "urn:azureiot:DeviceManagement:DeviceInformation:1";
        private const string DeviceInformationInterfaceName = "deviceInformation";
        private const string Manufacturer = "manufacturer";
        private const string Model = "model";
        private const string SoftwareVersion = "swVersion";
        private const string OperatingSystemName = "osName";
        private const string ProcessorArchitecture = "processorArchitecture";
        private const string ProcessorManufacturer = "processorManufacturer";
        private const string TotalStorage = "totalStorage";
        private const string TotalMemory = "totalMemory";
        private ICollection<DigitalTwinPropertyReport> propertyCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceInformationInterface"/> class.
        /// </summary>
        public DeviceInformationInterface()
            : base(DeviceInformationInterfaceId, DeviceInformationInterfaceName)
        {
            this.propertyCollection = new Collection<DigitalTwinPropertyReport>();
        }

        /// <summary>
        /// Setter for Manufacturer property.
        /// </summary>
        /// <param name="value">value.</param>
        public void SetManufacturer(string value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(Manufacturer, JsonConvert.SerializeObject(value)));
        }

        /// <summary>
        /// Setter for Model property.
        /// </summary>
        /// <param name="value">value.</param>
        public void SetModel(string value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(Model, JsonConvert.SerializeObject(value)));
        }

        /// <summary>
        /// Setter for Software Version property.
        /// </summary>
        /// <param name="value">value.</param>
        public void SetSoftwareVersion(string value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(SoftwareVersion, JsonConvert.SerializeObject(value)));
        }

        /// <summary>
        /// Setter for Operating System Name property.
        /// </summary>
        /// <param name="value">value.</param>
        public void SetOperatingSystemName(string value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(OperatingSystemName, JsonConvert.SerializeObject(value)));
        }

        /// <summary>
        /// Setter for Processor Architecture property.
        /// </summary>
        /// <param name="value">value.</param>
        public void SetProcessorArchitecture(string value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(ProcessorArchitecture, JsonConvert.SerializeObject(value)));
        }

        /// <summary>
        /// Setter for Processor Manufacturer property.
        /// </summary>
        /// <param name="value">value.</param>
        public void SetProcessorManufacturer(string value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(ProcessorManufacturer, JsonConvert.SerializeObject(value)));
        }

        /// <summary>
        /// Setter for Total Memory property.
        /// </summary>
        /// <param name="value">value.</param>
        public void SetTotalMemory(double value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(TotalMemory, value.ToString()));
        }

        /// <summary>
        /// Setter for Total Storage property.
        /// </summary>
        /// <param name="value">value.</param>
        public void SetTotalStorage(double value)
        {
            this.propertyCollection.Add(
                new DigitalTwinPropertyReport(TotalStorage, value.ToString()));
        }

        /// <summary>
        /// Send properties to service.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendAllPropertiesAsync()
        {
            await this.ReportPropertiesAsync(this.propertyCollection).ConfigureAwait(false);
            this.propertyCollection.Clear();
        }

        /// <summary>
        /// Callback when registration is completed.
        /// </summary>
        protected override void OnRegistrationCompleted()
        {
            Console.WriteLine($"OnRegistrationCompleted.");
        }
    }
}
