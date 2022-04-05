// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains DeviceJob Errors information
    /// </summary>
    public class DeviceJobError
    {
        /// <summary>
        /// Device Job Error 
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Device Job Error description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
