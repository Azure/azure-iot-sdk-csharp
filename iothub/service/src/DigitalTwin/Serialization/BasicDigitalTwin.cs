// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Serialization
{
    /// <summary>
    /// An optional, helper class for deserializing a digital twin.
    /// </summary>
    public class BasicDigitalTwin
    {
        /// <summary>
        /// The unique Id of the digital twin.
        /// </summary>
        /// <remarks>
        /// This is present at the root of every digital twin.
        /// </remarks>
        [JsonProperty("$dtId")]
        public string Id { get; set; }

        /// <summary>
        /// Information about the model a digital twin conforms to.
        /// </summary>
        /// <remarks>
        /// This field is present on every digital twin.
        /// </remarks>
        [JsonProperty("$metadata")]
        public DigitalTwinMetadata Metadata { get; set; } = new DigitalTwinMetadata();

        /// <summary>
        /// Additional properties of the digital twin.
        /// </summary>
        /// <remarks>
        /// This field will contain any properties of the digital twin that are not already defined by the other strong types of this class.
        /// </remarks>
        [JsonExtensionData]
        public IDictionary<string, object> CustomProperties { get; } = new Dictionary<string, object>();
    }
}
