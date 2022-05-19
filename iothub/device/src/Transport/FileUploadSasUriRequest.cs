// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// The request parameters when getting a file upload sas uri from IoT hub.
    /// </summary>
    public class FileUploadSasUriRequest
    {
        /// <summary>
        /// The name of the file for which a SAS URI will be generated. This field is mandatory.
        /// </summary>
        [JsonProperty(PropertyName = "blobName")]
        public string BlobName { get; set; }
    }
}
