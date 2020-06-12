// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// The request payload to send to IoT Hub to notify it when a file upload is completed, whether successful or not.
    /// </summary>
    public class FileUploadCompletionNotification
    {
        /// <summary>
        /// The correlation id that maps this completion notification to the file upload. The value should equal the <see cref="FileUploadSasUriResponse.CorrelationId">correlation id </see>
        /// returned from IoT Hub when first getting the SAS Uri for this file upload .
        /// </summary>
        [JsonProperty(PropertyName = "correlationId")]
        public string CorrelationId { get; set; }

        /// <summary>
        /// Whether the file upload was successful or not.
        /// </summary>
        [JsonProperty(PropertyName = "isSuccess")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The status code for the file upload.
        /// </summary>
        [JsonProperty(PropertyName = "statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// A brief description of the file upload status.
        /// </summary>
        [JsonProperty(PropertyName = "statusDescription")]
        public string StatusDescription { get; set; }
    }
}
