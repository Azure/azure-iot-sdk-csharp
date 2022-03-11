// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The properties defining a device group
    /// </summary>
    public class DeviceGroupProperties
    {
        /// <summary>
        /// The flag to mark deleted records.
        /// </summary>
        [JsonProperty(PropertyName = "deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        ///     The version of the query language used in the query expression. TODO: links to
        ///    queryVersion documentation.
        /// </summary>
        [JsonProperty(PropertyName = "queryVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string QueryVersion { get; set; }

        /// <summary>
        ///     The query expression that describes the devices and modules in the device group.
        ///     The query expression uses attributes to filter devices and modules into the device
        ///     group. TODO: add link to Query definition grammar and sample queries.
        /// </summary>
        [JsonProperty(PropertyName = "query", NullValueHandling = NullValueHandling.Ignore)]
        public string Query { get; set; }

        /// <summary>
        ///     The description of the device group.
        /// </summary>
        [JsonProperty(PropertyName = "description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        /// <summary>
        ///    The unique identifier of the device group often used as the principal identifier.
        /// </summary>
        [JsonProperty(PropertyName = "principalId", NullValueHandling = NullValueHandling.Ignore)]
        public string PrincipalId { get; set; }

        /// <summary>
        ///   The time of deletion when isDeleted is true.
        /// </summary>
        [JsonProperty(PropertyName = "deletedTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime DeletedTimeUtc { get; set; }

        /// <summary>
        ///     The time the record will be automatically purged when isDeleted is true.
        /// </summary>
        [JsonProperty(PropertyName = "purgeTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime PurgeTimeUtc { get; set; }
    }
}