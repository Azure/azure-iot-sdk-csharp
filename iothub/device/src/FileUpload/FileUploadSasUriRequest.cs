// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The request parameters when getting a file upload SAS URI from IoT hub.
    /// </summary>
    public class FileUploadSasUriRequest
    {
        /// <summary>
        /// The request parameters when getting a file upload SAS URI from IoT hub.
        /// </summary>
        /// <param name="blobName">The name of the file for which a SAS URI will be generated.</param>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="blobName"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="blobName"/> is empty or whitespace.</exception>
        public FileUploadSasUriRequest(string blobName)
        {
            Argument.AssertNotNullOrWhiteSpace(blobName, nameof(blobName));
            BlobName = blobName;
        }

        /// <summary>
        /// The name of the file for which a SAS URI will be generated.
        /// </summary>
        [JsonProperty("blobName")]
        public string BlobName { get; }
    }
}
