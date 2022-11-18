// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Device provisioning status.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProvisioningStatus
    {
        /// <summary>
        /// Device provisioning is enabled.
        /// </summary>
        [EnumMember(Value = "enabled")]
        Enabled,

        /// <summary>
        /// Device provisioning is disabled.
        /// </summary>
        [EnumMember(Value = "disabled")]
        Disabled,
    }
}
