// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
    using System.Collections.Generic;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum BulkOperationMode
    {
        Create = 1,
        Update,
        UpdateIfMatchETag,
        Delete
    }

    public sealed class BulkOperation
    {
        /// <summary>
        /// Operation mode
        /// </summary>
        [JsonProperty(PropertyName = "mode", Required = Required.Always)]
        public BulkOperationMode Mode { get; set; }

        /// <summary>
        /// Enrollments for bulk operation
        /// </summary>
        [JsonProperty(PropertyName = "enrollments", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<Enrollment> Enrollments { get; set; }
    }
}
