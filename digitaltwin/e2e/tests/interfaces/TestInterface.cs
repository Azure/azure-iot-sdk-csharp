// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.IoT.DigitalTwin.Device.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoT.DigitalTwin.E2ETests.interfaces
{
    class TestInterface : DigitalTwinInterfaceClientWithTracking
    {
        public const string InterfaceId = "urn:contoso:azureiot:sdk:testinterface:1";

        public const string ReadOnlyPropertyName = "ReadOnlyProperty";
        public const string ReadWritePropertyName = "writableProperty";
        public const string AnotherWritablePropertyName = "anotherWritableProperty";

        public TestInterface(string interfaceName)
            : base(InterfaceId, interfaceName)
        {
        }

        public async Task updateReadOnlyPropertyAsync(string value)
        {
            DigitalTwinPropertyReport propertyReport = new DigitalTwinPropertyReport(ReadOnlyPropertyName, value);
            List<DigitalTwinPropertyReport> reportedProperties = new List<DigitalTwinPropertyReport>();
            reportedProperties.Add(propertyReport);
            await this.ReportPropertiesAsync(reportedProperties);
        }

        public async Task updateReadWritePropertyAsync(string value)
        {
            DigitalTwinPropertyReport propertyReport = new DigitalTwinPropertyReport(ReadWritePropertyName, value);
            List<DigitalTwinPropertyReport> reportedProperties = new List<DigitalTwinPropertyReport>();
            reportedProperties.Add(propertyReport);
            await this.ReportPropertiesAsync(reportedProperties);
        }

        public async Task updateMultiplePropertiesAsync(string name1, string value1, string name2, string value2)
        {
            DigitalTwinPropertyReport propertyReport1 = new DigitalTwinPropertyReport(name1, value1);
            DigitalTwinPropertyReport propertyReport2 = new DigitalTwinPropertyReport(name2, value2);
            List<DigitalTwinPropertyReport> reportedProperties = new List<DigitalTwinPropertyReport>();
            reportedProperties.Add(propertyReport1);
            reportedProperties.Add(propertyReport2);
            await this.ReportPropertiesAsync(reportedProperties);
        }

        protected override async Task<DigitalTwinCommandResponse> OnDelegatedCommandRequest(DigitalTwinCommandRequest commandRequest)
        {
            switch (commandRequest.Name)
            {
                case "syncCommand":
                case "anotherSyncCommand":
                    return new DigitalTwinCommandResponse(StatusCodeCompleted, commandRequest.Payload);
                case "asyncCommand":
                case "anotherAsyncCommand":
                    return new DigitalTwinCommandResponse(StatusCodePending, asyncCommandResponsePayload);
                default:
                    return new DigitalTwinCommandResponse(StatusCodeNotImplemented, null);
            }
        }

        public static string asyncCommandResponsePayload = "{\"CommandStatus\":\"Processing\"}";

        protected override async Task OnPropertyUpdated(DigitalTwinPropertyUpdate propertyUpdate)
        {
            await base.OnPropertyUpdated(propertyUpdate);

            switch (propertyUpdate.PropertyName)
            {
                case "readOnlyProperty":
                    break;
                case "writableProperty":
                    break;
                case "anotherWritableProperty":
                    break;
                case "complexProperty":
                    break;
                default:
                    break;
            }
        }
    }
}
