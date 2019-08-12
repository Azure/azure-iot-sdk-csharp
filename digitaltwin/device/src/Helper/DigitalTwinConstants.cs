// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Helper
{
    /// <summary>
    /// All constants related with Digital Twin Service sdk.
    /// </summary>
    public static class DigitalTwinConstants
    {
        ///// <summary>
        ///// The Digital Twin Interfaces section.
        ///// </summary>
        //public const string DigitalTwinInterfaces = "__iot:interfaces";

        /// <summary>
        /// Digital Twin interface instance id constant used for events.
        /// </summary>
        public const string IothubInterfaceInstance = "$.ifname";

        /// <summary>
        /// Digital Twin interface id constant used for events.
        /// </summary>
        public const string IoTHubInterfaceId = "$.ifid";

        /// <summary>
        /// The json content type.
        /// </summary>
        public const string JsonContentType = "application/json";

        /// <summary>
        /// The maximum allowed length of interface id.
        /// </summary>
        public const int MaxInterfaceIdLength = 64;

        /// <summary>
        /// The invalid interface id error message.
        /// </summary>
        public const string InvalidInterfaceIdErrorMessage = "invalid interface id";

        /// <summary>
        /// The invalid interface id error message.
        /// </summary>
        public const string InterfaceIdLengthErrorMessage = "interface id maximum allowed size of 64 ASCII characters.";

        /// <summary>
        /// The parameter null error message format.
        /// </summary>
        public const string ParameterNullErrorMessageFormat = "The parameter named {0} can't be null.";

        /// <summary>
        /// The parameter null or whitespace error message format.
        /// </summary>
        public const string ParameterNullWhiteSpaceErrorMessageFormat = "The parameter named {0} can't be null, empty string or white space.";

        /// <summary>
        /// The parameter uses invalid Digital Twin type.
        /// </summary>
        public const string InvalidDigitalTwinTypeErrorMessage = "The type is invalid, only support TwinCollection and primitive type (string, number)";

        /// <summary>
        /// The interface not registered error message format.
        /// </summary>
        public const string DeviceInterfaceNotRegisteredErrorMessageFormat = "The interface {0} is not registered.";
    }
}
