// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The request payload to send to IoT hub to notify it when a file upload is completed, whether successful or not.
    /// </summary>
    public class FileUploadCompletionNotification
    {
        /// <summary>
        /// Used to tell the service that the client is done uploading a file with the specified correlation Id.
        /// </summary>
        /// <param name="correlationId">The correlation Id of the SAS URI.</param>
        /// <param name="isSuccess">Whether the file upload was successful or not.</param>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="correlationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="correlationId"/> is empty or whitespace.</exception>
        public FileUploadCompletionNotification(string correlationId, bool isSuccess)
        {
            Argument.AssertNotNullOrWhiteSpace(correlationId, nameof(correlationId));
            CorrelationId = correlationId;
            IsSuccess = isSuccess;
        }

        /// <summary>
        /// The correlation id that maps this completion notification to the file upload.
        /// The value should equal the <see cref="FileUploadSasUriResponse.CorrelationId"/>
        /// returned from IoT hub when first getting the SAS URI for this file upload.
        /// </summary>
        [JsonProperty("correlationId")]
        public string CorrelationId { get; }

        /// <summary>
        /// Whether the file upload was successful or not.
        /// </summary>
        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; }

        /// <summary>
        /// The status code for the file upload. This is user defined and will be presented to the service client listening
        /// for file upload notifications.
        /// </summary>
        /// <remarks>
        /// This property is optional.
        /// </remarks>
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// A brief description of the file upload status. This is user defined and will be presented to the service client listening
        /// for file upload notifications.
        /// <remarks>
        /// This property is optional.
        /// </remarks>
        /// </summary>
        [JsonProperty("statusDescription")]
        public string StatusDescription { get; set; }
    }
}
