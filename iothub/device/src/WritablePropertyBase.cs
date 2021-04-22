// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The writable property class.
    /// </summary>
    /// <remarks>
    /// This abstract base class is used to allow extension to use a different set of attributes or serialization. For example our default implementation found in <see cref="WritablePropertyResponse"/> is based on Newtonsoft seriailizer attributes.
    /// </remarks>
    public abstract class WritablePropertyBase
    {
        /// <summary>
        /// Represents the JSON document property name for the value
        /// </summary>
        protected const string ValuePropertyName = "value";

        /// <summary>
        /// Represents the JSON document property name for the Ack Code
        /// </summary>
        protected const string AckCodePropertyName = "ac";

        /// <summary>
        /// Represents the JSON document property name for the Ack Version
        /// </summary>
        protected const string AckVersionPropertyName = "av";

        /// <summary>
        /// Represents the JSON document property name for the Ack Description
        /// </summary>
        protected const string AckDescriptionPropertyName = "ad";

        /// <summary>
        /// Convenience constructor for specifying the properties.
        /// </summary>
        /// <param name="propertyValue">The unserialized property value.</param>
        /// <param name="ackCode">The acknowledgement code, usually an HTTP Status Code e.g. 200, 400.</param>
        /// <param name="ackVersion">The acknowledgement version, as supplied in the property update request.</param>
        /// <param name="ackDescription">The acknowledgement description, an optional, human-readable message about the result of the property update.</param>
        public WritablePropertyBase(object propertyValue, int ackCode, long ackVersion, string ackDescription = default)
        {
            // null checks
            Value = propertyValue;
            AckCode = ackCode;
            AckVersion = ackVersion;
            AckDescription = ackDescription;
        }

        /// <summary>
        /// The unserialized property value.
        /// </summary>
        public abstract object Value { get; set; }

        /// <summary>
        /// The acknowledgement code, usually an HTTP Status Code e.g. 200, 400.
        /// </summary>
        public abstract int AckCode { get; set; }

        /// <summary>
        /// The acknowledgement version, as supplied in the property update request.
        /// </summary>
        public abstract long AckVersion { get; set; }

        /// <summary>
        /// The acknowledgement description, an optional, human-readable message about the result of the property update.
        /// </summary>
        public abstract string AckDescription { get; set; }
    }
}
