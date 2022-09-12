// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Encapsulates the result of a bulk registry operation.
    /// </summary>
    public sealed class BulkRegistryOperationResult
    {
        /// <summary>
        /// Initialize an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        internal BulkRegistryOperationResult()
        {
        }

        /// <summary>
        /// Whether or not the operation was successful.
        /// </summary>
        [JsonProperty(PropertyName = "isSuccessful", Required = Required.Always)]
        public bool IsSuccessful { get; internal set; }

        /// <summary>
        /// If the operation was not successful, this contains an array of DeviceRegistryOperationError objects.
        /// </summary>
        [JsonProperty(PropertyName = "errors", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1819:Properties should not return arrays",
            Justification = "Cannot change property types on public classes.")]
        public DeviceRegistryOperationError[] Errors { get; internal set; }
    }
}
