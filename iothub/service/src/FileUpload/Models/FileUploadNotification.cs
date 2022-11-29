﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public FileUploadNotification()
        { }

        /// <summary>
        /// Id of the device which uploaded the file.
        /// </summary>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; protected internal set; }

        /// <summary>
        /// URI path of the uploaded file.
        /// </summary>
        // TODO: consider changing this to System.Uri before GA
        [JsonPropertyName("blobUri")]
        public string BlobUriPath { get; protected internal set; }

        /// <summary>
        /// Name of the uploaded file.
        /// </summary>
        [JsonPropertyName("blobName")]
        public string BlobName { get; protected internal set; }

        /// <summary>
        /// Date and time indicating when the file was last updated in UTC.
        /// </summary>
        [JsonPropertyName("lastUpdatedTime")]
        public DateTimeOffset? LastUpdatedOnUtc { get; protected internal set; }

        /// <summary>
        /// Size of the uploaded file in bytes.
        /// </summary>
        [JsonPropertyName("blobSizeInBytes")]
        public long BlobSizeInBytes { get; protected internal set; }

        /// <summary>
        /// Date and time indicating when the notification was created in UTC.
        /// </summary>
        [JsonPropertyName("enqueuedTimeUtc")]
        public DateTimeOffset EnqueuedOnUtc { get; protected internal set; }
    }
}
