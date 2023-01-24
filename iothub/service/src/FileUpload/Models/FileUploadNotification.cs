// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains properties for file upload notifications.
    /// </summary>
    public class FileUploadNotification
    {
        /// <summary>
        /// This constructor is for deserialization and unit test mocking purposes.
        /// </summary>
        /// <remarks>
        /// To unit test methods that use this type as a response, inherit from this class and give it a constructor
        /// that can set the properties you want.
        /// </remarks>
        protected internal FileUploadNotification()
        { }

        /// <summary>
        /// Id of the device which uploaded the file.
        /// </summary>
        [JsonProperty("deviceId")]
        public string DeviceId { get; protected internal set; }

        /// <summary>
        /// URI path of the uploaded file.
        /// </summary>
        [JsonProperty("blobUri")]
        public Uri BlobUriPath { get; protected internal set; }

        /// <summary>
        /// Name of the uploaded file.
        /// </summary>
        [JsonProperty("blobName")]
        public string BlobName { get; protected internal set; }

        /// <summary>
        /// Date and time indicating when the file was last updated in UTC.
        /// </summary>
        [JsonProperty("lastUpdatedTime")]
        public DateTimeOffset? LastUpdatedOnUtc { get; protected internal set; }

        /// <summary>
        /// Size of the uploaded file in bytes.
        /// </summary>
        [JsonProperty("blobSizeInBytes")]
        public long BlobSizeInBytes { get; protected internal set; }

        /// <summary>
        /// Date and time indicating when the notification was created in UTC.
        /// </summary>
        [JsonProperty("enqueuedTimeUtc")]
        public DateTimeOffset EnqueuedOnUtc { get; protected internal set; }
    }
}
