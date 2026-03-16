// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Encapsulates the result of a bulk registry operation.
    /// </summary>
    public class BulkRegistryOperationResult
    {
        /// <summary>
        /// Whether or not the operation was successful.
        /// </summary>
        [JsonPropertyName("isSuccessful")]
        public bool IsSuccessful { get; protected internal set; }

        /// <summary>
        /// If the operation was not successful, this contains DeviceRegistryOperationError objects.
        /// </summary>
        [JsonPropertyName("errors")]
        public IList<DeviceRegistryOperationError> Errors { get; internal set; } = new List<DeviceRegistryOperationError>();
    }
}
