// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The request parameters when getting a file upload SAS URI from IoT hub.
    /// </summary>
    public class FileUploadSasUriRequest
    {
        /// <summary>
        /// Serialization constructor.
        /// </summary>
        internal FileUploadSasUriRequest()
        {
        }
        /// <summary>
        /// The request parameters when getting a file upload SAS URI from IoT hub.
        /// </summary>
        /// <param name="blobName">The name of the file for which a SAS URI will be generated.</param>
        public FileUploadSasUriRequest(string blobName)
        {
            BlobName = blobName;
        }

        /// <summary>
        /// The name of the file for which a SAS URI will be generated.
        /// </summary>
        [JsonPropertyName("blobName")]
        public string BlobName { get; }
    }
}
