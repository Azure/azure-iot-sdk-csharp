// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if ENABLE_MODULES_SDK
namespace Microsoft.Azure.Devices
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Configurations for Module Management Agent, Edge Hub and Modules on the device.
    /// </summary>
    public class ConfigurationContent
    {
        /// <summary>
        /// Gets or sets Module Configurations
        /// </summary>
        [JsonProperty(PropertyName = "modulesContent")]
        public IDictionary<string, IDictionary<string, object>> ModulesContent { get; set; }

        /// <summary>
        /// Gets or sets Module Configurations
        /// </summary>
        [JsonProperty(PropertyName = "deviceContent")]
        public IDictionary<string, object> DeviceContent { get; set; }
    }
}
#endif