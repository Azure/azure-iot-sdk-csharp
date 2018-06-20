// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Parameters to execute a direct method on the device or module
    /// </summary>
    internal class MethodInvokeRequest
    {
        internal MethodInvokeRequest() { } // @ailn: for serialization only

        /// <summary>
        /// Creates an instance of DirectMethodRequest type
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="responseTimeout">Method timeout</param>
        /// <param name="connectionTimeout">Device connection timeout</param>
        /// <exception cref="ArgumentException">If <b>methodName</b> is null or whitespace</exception>
        public MethodInvokeRequest(string methodName, string payload, TimeSpan? responseTimeout, TimeSpan? connectionTimeout)
        {
            ValidatePayloadIsJson(payload);
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException("Canot be empty", nameof(methodName));
            }

            this.MethodName = methodName;
            this.ResponseTimeout = responseTimeout;
            this.ConnectionTimeout = connectionTimeout;
            this.Payload = new JRaw(payload);
        }

        /// <summary>
        /// Method to run
        /// </summary>
        [JsonProperty("methodName", Required = Required.Always)]
        public string MethodName { get; set; }

        /// <summary>
        /// Method timeout
        /// </summary>
        [JsonIgnore]
        public TimeSpan? ResponseTimeout { get; set; }

        /// <summary>
        /// Timeout for device to come online
        /// </summary>
        [JsonIgnore]
        public TimeSpan? ConnectionTimeout { get; set; }

        /// <summary>
        /// Set payload as json
        /// </summary>
        public void ValidatePayloadIsJson(string json)
        {
            try
            {
                JToken.Parse(json); // @ailn: this is just a check for valid json as JRaw does not do the validation.
            }
            catch (JsonException ex)
            {
                throw new ArgumentException(ex.Message, nameof(json)); // @ailn: here we want to hide the fact we're using Json.net
            }
        }

        /// <summary>
        /// Method timeout in seconds
        /// </summary>
        [JsonProperty("responseTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ResponseTimeoutInSeconds => !this.ResponseTimeout.HasValue || this.ResponseTimeout <= TimeSpan.Zero ? (int?)null : (int)this.ResponseTimeout.Value.TotalSeconds;

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        [JsonProperty("connectTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ConnectionTimeoutInSeconds => !this.ConnectionTimeout.HasValue || this.ConnectionTimeout <= TimeSpan.Zero ? (int?)null : (int)this.ConnectionTimeout.Value.TotalSeconds;

        [JsonProperty("payload")]
        internal JRaw Payload { get; set; }
    }
}