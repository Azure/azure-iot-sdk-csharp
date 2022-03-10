// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using System;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;
    /// <summary>
    /// The data structure represent the RoleAssignment (A role assignment connects a role definition to a scope for a principal)
    /// </summary>
    public class RoleAssignment : IETagHolder
    {
        /// <summary>
        /// name
        /// </summary>
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public Guid Name { get; set; }
        /// <summary>
        /// properties
        /// </summary>
        [JsonProperty(PropertyName = "properties", NullValueHandling = NullValueHandling.Ignore)]
        public RoleAssignmentProperties Properties { get; set; }
        //
        /// <summary>
        /// etag
        /// </summary>
        [JsonProperty(PropertyName = "etag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag { get; set; }
    }
}