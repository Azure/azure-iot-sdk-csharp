﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The managed identity used to access the storage account for IoT hub import and export jobs.
    /// For more information on managed identity configuration on IoT hub, see <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-managed-identity"/>.
    /// For more information on managed identities, see <see href="https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview"/>
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
