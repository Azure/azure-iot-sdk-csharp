// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The serialization class for the parameters to execute a direct method.
    /// </summary>
    public class DirectMethodRequest
    {
        // For serialization only
        internal DirectMethodRequest()
        {
        }

        internal DirectMethodRequest(string methodName, DirectMethodRequestOptions options)
        {
            MethodName = methodName;
            ConnectionTimeoutInSeconds = options?.ConnectionTimeout;
            ResponseTimeoutInSeconds = options?.ResponseTimeout;
        }

        /// <summary>
        /// Method to run
        /// </summary>
        [JsonProperty("methodName", Required = Required.Always)]
        public string MethodName { get; internal set; }

        /// <summary>
        /// Method timeout in seconds
        /// </summary>
        [JsonProperty("responseTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        public int? ResponseTimeoutInSeconds { get; internal set; }

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        [JsonProperty("connectTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        public int? ConnectionTimeoutInSeconds { get; internal set; }

        [JsonProperty("payload")]
        internal JRaw Payload { get; set; }

        /// <summary>
        /// Get the json payload
        /// </summary>
        public string GetPayload()
        {
            return (string)Payload;
        }
    }
}
