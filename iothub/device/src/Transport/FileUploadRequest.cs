// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class FileUploadRequest
    {
        [JsonProperty(PropertyName = "blobName")]
        public string BlobName { get; set; }
    }
}
