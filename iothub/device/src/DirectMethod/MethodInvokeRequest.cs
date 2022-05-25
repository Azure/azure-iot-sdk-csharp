// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Parameters to execute a direct method on the device or module.
    /// </summary>
    internal class MethodInvokeRequest
    {
        // For serialization
        internal MethodInvokeRequest()
        {
        }

        /// <summary>
        /// Creates an instance of DirectMethodRequest type.
        /// </summary>
        /// <param name="methodName">Method name.</param>
        /// <param name="payload">Method invocation payload.</param>
        /// <param name="responseTimeout">Method timeout.</param>
        /// <param name="connectionTimeout">Device connection timeout.</param>
        /// <exception cref="ArgumentException">If <b>methodName</b> is null or whitespace.</exception>
        public MethodInvokeRequest(string methodName, string payload, TimeSpan? responseTimeout, TimeSpan? connectionTimeout)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }
            MethodName = methodName;

            if (!string.IsNullOrEmpty(payload))
            {
                ValidatePayloadIsJson(payload);
                Payload = new JRaw(payload);
            }

            ResponseTimeout = responseTimeout;
            ConnectionTimeout = connectionTimeout;
        }

        /// <summary>
        /// Method to invoke.
        /// </summary>
        [JsonProperty("methodName", Required = Required.Always)]
        public string MethodName { get; private set; }

        /// <summary>
        /// Method timeout.
        /// </summary>
        [JsonIgnore]
        public TimeSpan? ResponseTimeout { get; private set; }

        /// <summary>
        /// Timeout for device to come online.
        /// </summary>
        [JsonIgnore]
        public TimeSpan? ConnectionTimeout { get; private set; }

        /// <summary>
        /// Method timeout, in seconds.
        /// </summary>
        [JsonProperty("responseTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ResponseTimeoutInSeconds => ResponseTimeout.HasValue && ResponseTimeout > TimeSpan.Zero
            ? (int)ResponseTimeout.Value.TotalSeconds
            : (int?)null;

        /// <summary>
        /// Connection timeout, in seconds.
        /// </summary>
        [JsonProperty("connectTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ConnectionTimeoutInSeconds => ConnectionTimeout.HasValue && ConnectionTimeout > TimeSpan.Zero
            ? (int)ConnectionTimeout.Value.TotalSeconds
            : (int?)null;

        [JsonProperty("payload", NullValueHandling = NullValueHandling.Include)]
        internal JRaw Payload { get; set; }

        private static void ValidatePayloadIsJson(string json)
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
    }
}
