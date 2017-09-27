// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    using Newtonsoft.Json;

    /// <summary>
    /// Contains bulk enrollment operation result
    /// </summary>
    public sealed class BulkOperationResult
    {
        /// <summary>
        /// If false, not all operations in the bulk enrollment succeeded.
        /// </summary>
        [JsonProperty(PropertyName = "isSuccessful", Required = Required.Always)]
        public bool IsSuccessful { get; internal set; }

        /// <summary>
        /// Registration errors.
        /// </summary>
        [JsonProperty(PropertyName = "errors", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DeviceRegistrationOperationError[] Errors { get; set; }
    }
}
