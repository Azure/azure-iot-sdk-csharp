// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Useful for deserializing errors that occur from a bulk import job.
    /// </summary>
    /// <remarks>
    /// Any errors that occur during a bulk import job can be deserialized with this class from a blob file named "importErrors.log"
    /// in the container specified during import.
    /// </remarks>
    public sealed class ImportJobError
    {
        /// <summary>
        /// The name of the blob file that contains the import errors.
        /// </summary>
        public const string ImportErrorsBlobName = "importErrors.log";

        /// <summary>
        /// The Id of the device for the error.
        /// </summary>
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// An error code for the device import.
        /// </summary>
        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        /// <summary>
        /// A textual reason for the error.
        /// </summary>
        [JsonProperty("errorStatus")]
        public string ErrorStatus { get; set; }
    }
}
