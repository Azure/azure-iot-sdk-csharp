// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Configurations for Module Management Agent, Edge Hub and Modules on the device.
    /// </summary>
    public class ConfigurationContent
    {
        /// <summary>
        /// Gets or sets Module Configurations
        /// </summary>
        [JsonProperty(PropertyName = "modulesContent")]
#pragma warning disable CA2227 // Collection properties should be read only
        public IDictionary<string, IDictionary<string, object>> ModulesContent { get; set; }

#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets Module Configurations
        /// </summary>
        [JsonProperty(PropertyName = "deviceContent")]
#pragma warning disable CA2227 // Collection properties should be read only
        public IDictionary<string, object> DeviceContent { get; set; }

#pragma warning restore CA2227 // Collection properties should be read only
    }
}
