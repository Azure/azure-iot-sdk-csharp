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
        /// Gets or sets the configurations to be applied to the Edge agent.
        /// </summary>
        /// <remarks>
        /// For more information on this field, see <see href="https://docs.microsoft.com/en-us/azure/iot-edge/module-composition?view=iotedge-2020-11#create-a-deployment-manifest">this documentation</see>.
        /// </remarks>
        [JsonProperty(PropertyName = "modulesContent")]
#pragma warning disable CA2227 // Collection properties should be read only
        public IDictionary<string, IDictionary<string, object>> ModulesContent { get; set; }

#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the configurations to be applied on device modules
        /// </summary>
        [JsonProperty(PropertyName = "moduleContent")]
#pragma warning disable CA2227 // Collection properties should be read only
        public IDictionary<string, object> ModuleContent { get; set; }

#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the configurations to be applied on devices.
        /// </summary>
        [JsonProperty(PropertyName = "deviceContent")]
#pragma warning disable CA2227 // Collection properties should be read only
        public IDictionary<string, object> DeviceContent { get; set; }

#pragma warning restore CA2227 // Collection properties should be read only
    }
}
