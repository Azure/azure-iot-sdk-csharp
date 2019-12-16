// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.DigitalTwin.Client.Model;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.DigitalTwin.E2ETests.interfaces
{
    class TestInterface2 : DigitalTwinInterfaceClientWithTracking
    {
        public const string TELEMETRY_NAME_INTEGER = "telemetryWithIntegerValue";
        public const string TELEMETRY_NAME_LONG = "telemetryWithLongValue";
        public const string TELEMETRY_NAME_DOUBLE = "telemetryWithDoubleValue";
        public const string TELEMETRY_NAME_FLOAT = "telemetryWithFloatValue";
        public const string TELEMETRY_NAME_BOOLEAN = "telemetryWithBooleanValue";
        public const string TELEMETRY_NAME_STRING = "telemetryWithStringValue";
        public const string TELEMETRY_NAME_DATE = "telemetryWithDateValue";
        public const string TELEMETRY_NAME_TIME = "telemetryWithTimeValue";
        public const string TELEMETRY_NAME_DATETIME = "telemetryWithDateTimeValue";
        public const string TELEMETRY_NAME_DURATION = "telemetryWithDurationValue";
        public const string TELEMETRY_NAME_ARRAY = "telemetryWithIntegerArrayValue";
        public const string TELEMETRY_NAME_MAP = "telemetryWithMapValue";
        public const string TELEMETRY_NAME_ENUM = "telemetryWithEnumValue";
        public const string TELEMETRY_NAME_COMPLEX_OBJECT = "telemetryWithComplexValueComplexObject";
        public const string TELEMETRY_NAME_COMPLEX_VALUE = "telemetryWithComplexValue";
        public const string COMMAND_SYNC_COMMAND = "syncCommand";
        public const string COMMAND_ASYNC_COMMAND = "asyncCommand";
        public const string COMMAND_ANOTHER_SYNC_COMMAND = "anotherSyncCommand";
        public const string COMMAND_ANOTHER_ASYNC_COMMAND = "anotherAsyncCommand";
        public const string PROPERTY_NAME_WRITABLE = "writableProperty";

        public const string InterfaceId = "urn:contoso:azureiot:sdk:testinterface2:2";

        public TestInterface2(string interfaceName)
            : base(InterfaceId, interfaceName)
        {
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

