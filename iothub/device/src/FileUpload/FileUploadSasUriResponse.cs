// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// The information provided from IoT hub that can be used with the Azure Storage SDK to upload a file from your device.
    /// </summary>
    public class FileUploadSasUriResponse
    {
        /// <summary>
        /// The correlation id to use when notifying IoT hub later once this file upload has completed.
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

        /// <summary>
        /// Get the complete Uri for the blob that can be uploaded to from this device. This Uri includes credentials, too.
        /// </summary>
        /// <returns>The complete Uri for the blob that can be uploaded to from this device</returns>
        public Uri GetBlobUri()
        {
            return new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "https://{0}/{1}/{2}{3}",
                    HostName,
                    ContainerName,
                    Uri.EscapeDataString(BlobName), // Pass URL encoded device name and blob name to support special characters
                    SasToken));
        }
    }
}
