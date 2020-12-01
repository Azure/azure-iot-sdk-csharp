// ---------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{

    /// <summary>
    /// Encapsulates the result of a bulk registry operation.
    /// </summary>
    public sealed class BulkRegistryOperationResult
    {
        /// <summary>
        /// Whether or not the operation was successful.
        /// </summary>
        [JsonProperty(PropertyName = "isSuccessful", Required = Required.Always)]
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// If the operation was not successful, this contains an array of DeviceRegistryOperationError objects.
        /// </summary>
        [JsonProperty(PropertyName = "errors", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1819:Properties should not return arrays",
            Justification = "Cannot change property types on public classes.")]
        public DeviceRegistryOperationError[] Errors { get; set; }
    }
}
