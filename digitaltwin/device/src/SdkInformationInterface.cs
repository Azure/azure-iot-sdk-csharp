// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Azure.IoT.DigitalTwin.Device;
using Azure.IoT.DigitalTwin.Device.Model;
using Newtonsoft.Json;

namespace Azure.IoT.DigitalTwin.Device
{
    internal class SdkInformationInterface : DigitalTwinInterfaceClient
    {
        private const string SdkInformationInterfaceId = "urn:azureiot:Client:SDKInformation:1";
        private const string SdkInformationInterfaceName = "urn_azureiot_Client_SDKInformation";
        private const string Language = "language";
        private const string Version = "version";
        private const string Vendor = "vendor";
        private const string SdkLanguage = "Csharp";
        private const string SdkVendor = "Microsoft";
        private const string SdkVersion = "0.0.1";

        private ICollection<DigitalTwinPropertyReport> propertyCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SdkInformationInterface"/> class.
        /// </summary>
        internal SdkInformationInterface()
            : base(SdkInformationInterfaceId, SdkInformationInterfaceName)
        {
            this.propertyCollection = new Collection<DigitalTwinPropertyReport>();
            this.propertyCollection.Add(new DigitalTwinPropertyReport(Language, JsonConvert.SerializeObject(SdkLanguage)));
            this.propertyCollection.Add(new DigitalTwinPropertyReport(Version, JsonConvert.SerializeObject(SdkVersion)));
            this.propertyCollection.Add(new DigitalTwinPropertyReport(Vendor, JsonConvert.SerializeObject(SdkVendor)));
        }

        /// <summary>
        /// Send properties to service.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task SendSdkInformationAsync()
        {
            await this.ReportPropertiesAsync(this.propertyCollection).ConfigureAwait(false);
        }

        /// <summary>
        /// No ops for SdkInformationInterface.
        /// </summary>
        protected internal override void OnRegistrationCompleted()
        {
        }
    }
}
