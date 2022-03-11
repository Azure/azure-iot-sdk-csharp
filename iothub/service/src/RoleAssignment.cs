// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A role assignment connects a role definition to a scope for a principal.
    /// </summary>
    public class RoleAssignment : IETagHolder
    {
        /// <summary>
        /// The role assignment name. Each role assignment name is globally unique and case insensitive.
        /// </summary>
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public Guid Name { get; set; }

        /// <summary>
        /// The role assignment properties.
        /// </summary>
        [JsonProperty(PropertyName = "properties", NullValueHandling = NullValueHandling.Ignore)]
        public RoleAssignmentProperties Properties { get; set; }

        /// <summary>
        /// The ETag for this role assignment.
        /// </summary>
        [JsonProperty(PropertyName = "etag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag { get; set; }
    }
}