// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// The request payload to send to IoT hub to notify it when a file upload is completed, whether successful or not.
    /// </summary>
    public class FileUploadCompletionNotification
    {
        /// <summary>
        /// Initialize an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        public FileUploadCompletionNotification()
        {
        }

        /// <summary>
        /// The correlation id that maps this completion notification to the file upload.
        /// The value should equal the <see cref="FileUploadSasUriResponse.CorrelationId">correlation id </see>
        /// returned from IoT hub when first getting the SAS Uri for this file upload
        /// </summary>
        [JsonProperty(PropertyName = "correlationId")]
        public string CorrelationId { get; set; }

        /// <summary>
        /// Whether the file upload was successful or not. This field is mandatory.
        /// </summary>
        [JsonProperty(PropertyName = "isSuccess")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The status code for the file upload. This is user defined and will be presented to the service client listening
        /// for file upload notifications. This field is optional.
        /// </summary>
        [JsonProperty(PropertyName = "statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// A brief description of the file upload status. This is user defined and will be presented to the service client listening
        /// for file upload notifications. This field is optional.
        /// </summary>
        [JsonProperty(PropertyName = "statusDescription")]
        public string StatusDescription { get; set; }
    }
}
