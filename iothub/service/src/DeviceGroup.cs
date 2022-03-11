// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    ///  A device group is made up of devices and modules whose attributes match the query expression.
    /// </summary>
    public class DeviceGroup : IETagHolder
    {
        /// <summary>
        /// The name of the device group. The name is unique within the Iot hub and case sensitive.
        /// </summary>
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// The device group properties.
        /// </summary>
        [JsonProperty(PropertyName = "properties", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceGroupProperties Properties { get; set; }

        /// <summary>
        /// The ETag for the device group.
        /// </summary>
        [JsonProperty(PropertyName = "etag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag { get; set; }

        /// <summary>
        /// The device group member count.
        /// </summary>
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }
    }
}
