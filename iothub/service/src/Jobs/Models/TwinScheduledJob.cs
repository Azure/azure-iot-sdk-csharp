// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains properties of twin scheduled job.
    /// </summary>
    public class TwinScheduledJob
    {
        /// <summary>
        /// [Required] The update twin tags and desired properties.
        /// </summary>
        [JsonProperty(PropertyName = "updateTwin", NullValueHandling = NullValueHandling.Ignore)]
        public Twin UpdateTwin { get; internal set; }
    }
}
