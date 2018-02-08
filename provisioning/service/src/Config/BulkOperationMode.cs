// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The Device Provisioning Service bulk operation modes.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/iot-dps/deviceenrollment">Device Enrollment</seealso>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BulkOperationMode
    {
        [EnumMember(Value = "create")]
        Create,
        [EnumMember(Value = "update")]
        Update,
        [EnumMember(Value = "updateIfMatchETag")]
        UpdateIfMatchETag,
        [EnumMember(Value = "delete")]
        Delete,
    }
}
