// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The managed identity used to access the storage account for IoT hub import and export jobs.
    /// </summary>
    public class ManagedIdentity
    {
        /// <summary>
        /// The user identity resource Id used to access the storage account for import and export jobs.
        /// </summary>
        [JsonProperty(PropertyName = "userAssignedIdentity", NullValueHandling = NullValueHandling.Ignore)]
        public string userAssignedIdentity { get; set; }
    }
}
