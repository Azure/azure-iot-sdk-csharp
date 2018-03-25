// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if ENABLE_MODULES_SDK
namespace Microsoft.Azure.Devices
{
    using Microsoft.Azure.Devices.Shared;

    using Newtonsoft.Json;

    /// <summary>
    /// The twin configuration.
    /// </summary>
    [JsonConverter(typeof(TwinContentJsonConverter))]
    public class TwinContent
    {
        public const string DesiredPropertiesPath = "properties.desired";

        public TwinContent()
        {
            this.TargetPropertyPath = DesiredPropertiesPath;
        }

        /// <summary>
        /// Gets or sets the target property path.
        /// </summary>
        public string TargetPropertyPath { get; internal set; }

        /// <summary>
        /// Gets or sets the target content.
        /// </summary>
        public TwinCollection TargetContent { get; set; }
    }
}
#endif