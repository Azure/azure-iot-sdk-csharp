// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Configurations for devices, modules, the module management agent, and Edge hub.
    /// </summary>
    public class ConfigurationContent
    {
        /// <summary>
        /// The modules configuration content.
        /// </summary>
        /// <remarks>
        /// See <see href="https://docs.microsoft.com/azure/iot-edge/module-composition?view=iotedge-2020-11#create-a-deployment-manifest"/>
        /// and <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/> for more details.
        /// <para>
        /// For Edge deployments, this should have a key of <c>"$edgeAgent"</c>.
        /// </para>
        /// </remarks>
        [JsonProperty("modulesContent")]
        public IDictionary<string, IDictionary<string, object>> ModulesContent { get; set; } = new Dictionary<string, IDictionary<string, object>>();

        /// <summary>
        /// The device module configuration content.
        /// </summary>
        [JsonProperty("moduleContent")]
        public IDictionary<string, object> ModuleContent { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The device configuration content.
        /// </summary>
        [JsonProperty("deviceContent")]
        public IDictionary<string, object> DeviceContent { get; set; } = new Dictionary<string, object>();
    }
}
