// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("modulesContent")]
        public IDictionary<string, IDictionary<string, object>> ModulesContent { get; set; } = new Dictionary<string, IDictionary<string, object>>();

        /// <summary>
        /// The device module configuration content.
        /// </summary>
        [JsonPropertyName("moduleContent")]
        public IDictionary<string, object> ModuleContent { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The device configuration content.
        /// </summary>
        [JsonPropertyName("deviceContent")]
        public IDictionary<string, object> DeviceContent { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// For use in serialization.
        /// </summary>
        /// <remarks>
        /// To give the properties above a default instance to prevent <see cref="NullReferenceException"/> but
        /// avoid serializing them when the dictionary is empty, we use this feature of Newtonsoft.Json, which must
        /// be public, and hide it from web docs and intellisense using the EditorBrowsable attribute.
        /// </remarks>
        /// <seealso href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm#ShouldSerialize"/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeModulesContent()
        {
            return ModulesContent != null && ModulesContent.Any();
        }

        /// <summary>
        /// For use in serialization.
        /// </summary>
        /// <remarks>
        /// To give the properties above a default instance to prevent <see cref="NullReferenceException"/> but
        /// avoid serializing them when the dictionary is empty, we use this feature of Newtonsoft.Json, which must
        /// be public, and hide it from web docs and intellisense using the EditorBrowsable attribute.
        /// </remarks>
        /// <seealso href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm#ShouldSerialize"/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeModuleContent()
        {
            return ModuleContent != null && ModuleContent.Any();
        }

        /// <summary>
        /// For use in serialization.
        /// </summary>
        /// <remarks>
        /// To give the properties above a default instance to prevent <see cref="NullReferenceException"/> but
        /// avoid serializing them when the dictionary is empty, we use this feature of Newtonsoft.Json, which must
        /// be public, and hide it from web docs and intellisense using the EditorBrowsable attribute.
        /// </remarks>
        /// <seealso href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm#ShouldSerialize"/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeDeviceContent()
        {
            return DeviceContent != null && DeviceContent.Any();
        }
    }
}
