// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The service response to a digital twin update operation.
    /// </summary>
    public class DigitalTwinUpdateResponse
    {
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="eTag">Weak ETag of the modified resource.</param>
        /// <param name="location">URI of the digital twin.</param>
        public DigitalTwinUpdateResponse(string eTag = default(string), string location = default(string))
        {
            ETag = eTag;
            Location = location;
        }

        /// <summary>
        /// Gets or sets weak ETag of the modified resource.
        /// </summary>
        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; internal set; }

        /// <summary>
        /// Gets the URI of the digital twin.
        /// </summary>
        [JsonProperty(PropertyName = "Location")]
        public string Location { get; internal set; }
    }
}
