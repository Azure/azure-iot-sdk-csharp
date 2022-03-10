namespace Microsoft.Azure.Devices
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;
    /// <summary>
    /// The data structure represent the DeviceGroup(A device group is made up of devices and modules whose attributes match the query expression)
    /// </summary>
    public class DeviceGroup : IETagHolder
    {
        /// <summary>
        /// name
        /// </summary>
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
        /// <summary>
        /// properties
        /// </summary>
        [JsonProperty(PropertyName = "properties", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceGroupProperties Properties { get; set; }
        /// <summary>
        /// etag
        /// </summary>
        [JsonProperty(PropertyName = "etag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag { get; set; }
        /// <summary>
        /// count
        /// </summary>
        [JsonProperty(PropertyName = "count", NullValueHandling = NullValueHandling.Ignore)]
        public int Count { get; set; }
    }
}
