namespace Microsoft.Azure.Devices
{
    using System;
    using Newtonsoft.Json;
    #pragma warning disable 1591
    public class DeviceGroupProperties
    {

        //
        // Summary:
        //     The flag to mark deleted records.
        [JsonProperty(PropertyName = "deleted", NullValueHandling = NullValueHandling.Ignore)]
        public bool Deleted { get; set; }
        //
        // Summary:
        //     The version of the query language used in the query expression. TODO: links to
        //     queryVersion documentation.
        [JsonProperty(PropertyName = "queryVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string QueryVersion { get; set; }
        //
        // Summary:
        //     The query expression that describes the devices and modules in the device group.
        //     The query expression uses attributes to filter devices and modules into the device
        //     group. TODO: add link to Query definition grammar and sample queries.
        [JsonProperty(PropertyName = "query", NullValueHandling = NullValueHandling.Ignore)]
        public string Query { get; set; }
        //
        // Summary:
        //     The description of the device group.
        [JsonProperty(PropertyName = "description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }
        //
        // Summary:
        //     The unique identifier of the device group often used as the principal identifier.
        [JsonProperty(PropertyName = "principalId", NullValueHandling = NullValueHandling.Ignore)]
        public string PrincipalId { get; set; }
        //
        // Summary:
        //     The time of deletion when isDeleted is true.
        [JsonProperty(PropertyName = "deletedTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime DeletedTimeUtc { get; set; }
        //
        // Summary:
        //     The time the record will be automatically purged when isDeleted is true.
        [JsonProperty(PropertyName = "purgeTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime PurgeTimeUtc { get; set; }
    }
}