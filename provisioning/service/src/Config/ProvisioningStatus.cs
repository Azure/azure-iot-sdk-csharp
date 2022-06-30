﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Device provisioning status.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1717:OnlyFlagsEnumsShouldHavePluralNames",
        Justification = "Public API cannot change name.")]
    [JsonConverter(typeof(StringEnumConverter))]
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
