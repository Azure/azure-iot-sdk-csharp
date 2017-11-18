// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The Device Provisioning Service bulk operation modes.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/iot-dps/deviceenrollment">Device Enrollment</seealso>
    public enum BulkOperationMode
    {
        [JsonProperty(PropertyName = "create")]
        Create,
        [JsonProperty(PropertyName = "update")]
        Update,
        [JsonProperty(PropertyName = "updateIfMatchETag")]
        UpdateIfMatchETag,
        [JsonProperty(PropertyName = "delete")]
        Delete,
    }
}
