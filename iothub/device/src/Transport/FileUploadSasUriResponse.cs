// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// The information provided from IoT Hub that can be used with the Azure Storage SDK to upload a file from your device.
    /// </summary>
    public class FileUploadSasUriResponse
    {
        /// <summary>
        /// The correlation id to use when notifying IoT Hub later once this file upload has completed.
        /// </summary>
        [JsonProperty(PropertyName = "correlationId")]
        public string CorrelationId { get; set; }

        /// <summary>
        /// The host name of the storage account that the file can be uploaded to.
        /// </summary>
        [JsonProperty(PropertyName = "hostName")]
        public string HostName { get; set; }

        /// <summary>
        /// The container in the storage account that the file can be uploaded to.
        /// </summary>
        [JsonProperty(PropertyName = "containerName")]
        public string ContainerName { get; set; }

        /// <summary>
        /// The name of the blob in the container that the file can be uploaded to.
        /// </summary>
        [JsonProperty(PropertyName = "blobName")]
        public string BlobName { get; set; }

        /// <summary>
        /// The sas token to use for authentication while using the Azure Storage SDK to upload the file.
        /// </summary>
        [JsonProperty(PropertyName = "sasToken")]
        public string SasToken { get; set; }
    }
}
