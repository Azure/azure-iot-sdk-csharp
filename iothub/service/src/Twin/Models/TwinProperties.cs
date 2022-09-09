// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Represents twin properties.
    /// </summary>
    public class TwinProperties
    {
        /// <summary>
        /// Initializes an instance of this class.
        /// </summary>
        public TwinProperties()
        {
            Desired = new TwinCollection();
            Reported = new TwinCollection();
        }

        /// <summary>
        /// Gets and sets the twin desired properties.
        /// </summary>
        [JsonProperty(PropertyName = "desired", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TwinCollection Desired { get; set; }

        /// <summary>
        /// Gets and sets the twin reported properties.
        /// </summary>
        [JsonProperty(PropertyName = "reported", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TwinCollection Reported { get; set; }
    }
}
