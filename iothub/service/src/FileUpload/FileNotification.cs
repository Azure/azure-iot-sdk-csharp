// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains properties for file upload notifications
    /// </summary>
    public class FileNotification
    {
        /// <summary>
        /// Id of the device which uploaded the file.
        /// </summary>
        [JsonProperty(PropertyName = "deviceId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DeviceId { get; set; }

        /// <summary>
        /// URI of the uploaded file.
        /// </summary>
        [JsonProperty(PropertyName = "blobUri", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056: Uri properties should not be strings.", Justification = "Public facing types cannot change as they are considered a breaking change.")]
        public string BlobUri { get; set; }

        /// <summary>
        /// Name of the uploaded file.
        /// </summary>
        [JsonProperty(PropertyName = "blobName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BlobName { get; set; }

        /// <summary>
        /// Date and time indicating when the file was last updated in UTC.
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdatedTime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset? LastUpdatedTime { get; set; }

        /// <summary>
        /// Size of the uploaded file in bytes.
        /// </summary>
        [JsonProperty(PropertyName = "blobSizeInBytes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long BlobSizeInBytes { get; set; }

        /// <summary>
        /// Date and time indicating when the notification was created in UTC.
        /// </summary>
        [JsonProperty(PropertyName = "enqueuedTimeUtc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime EnqueuedTimeUtc { get; set; }

        [JsonIgnore]
        internal string LockToken { get; set; }
    }
}
