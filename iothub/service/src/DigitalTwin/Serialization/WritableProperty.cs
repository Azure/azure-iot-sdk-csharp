// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// An optional, helper class for deserializing a digital twin.
    /// </summary>
    /// <remarks>
    /// A writable property is one that the service may request a change for from the device.
    /// </remarks>
    public class WritableProperty
    {
        /// <summary>
        /// The desired value.
        /// </summary>
        [JsonProperty("desiredValue")]
        public object DesiredValue { get; set; }

        /// <summary>
        /// The version of the property with the specified desired value.
        /// </summary>
        [JsonProperty("desiredVersion")]
        public int DesiredVersion { get; set; }

        /// <summary>
        /// The version of the reported property value.
        /// </summary>
        [JsonProperty("ackVersion")]
        public int AckVersion { get; set; }

        /// <summary>
        /// The response code of the property update request, usually an HTTP Status Code (e.g. 200).
        /// </summary>
        [JsonProperty("ackCode")]
        public int AckCode { get; set; }

        /// <summary>
        /// The message response of the property update request.
        /// </summary>
        [JsonProperty("ackDescription")]
        public string AckDescription { get; set; }

        /// <summary>
        /// The time when this property was last updated.
        /// </summary>
        [JsonProperty("lastUpdateTime")]
        public DateTimeOffset LastUpdatedOn { get; set; }
    }
}
