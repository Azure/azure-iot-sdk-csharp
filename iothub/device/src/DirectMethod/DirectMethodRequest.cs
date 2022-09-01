// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Parameters to execute a direct method on a device or module.
    /// </summary>
    public class DirectMethodRequest
    {
        // For serialization
        internal DirectMethodRequest()
        {
        }

        /// <summary>
        /// Initialize and instance of this class.
        /// </summary>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <exception cref="ArgumentException">If methodName is null or whitespace.</exception>
        public DirectMethodRequest(string methodName)
            : this(methodName, (string)null, null, null)
        {
        }

        /// <summary>
        /// Initialize and instance of this class.
        /// </summary>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="payload">The optional payload of the request.</param>
        /// <param name="responseTimeout">The optional response timeout for the request.</param>
        /// <param name="connectionTimeout">The optional connect timeout for the request.</param>
        /// <exception cref="ArgumentException">If methodName is null or whitespace.</exception>
        public DirectMethodRequest(string methodName, byte[] payload = null, TimeSpan? responseTimeout = null, TimeSpan? connectionTimeout = null)
            : this(methodName, Encoding.UTF8.GetString(payload), responseTimeout, connectionTimeout)
        {
        }

        /// <summary>
        /// Initialize and instance of this class.
        /// </summary>
        /// <param name="methodName">Method name.</param>
        /// <param name="payload">The optional payload of the request.</param>
        /// <param name="responseTimeout">The optional response timeout for the request.</param>
        /// <param name="connectionTimeout">The optional connect timeout for the request.</param>
        /// <exception cref="ArgumentException">If methodName is null or whitespace.</exception>
        public DirectMethodRequest(string methodName, string payload = null, TimeSpan? responseTimeout = null, TimeSpan? connectionTimeout = null)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }
            MethodName = methodName;

            if (!string.IsNullOrEmpty(payload))
            {
                ValidatePayloadIsJson(payload);
                JsonPayload = new JRaw(payload);
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
        public int? ResponseTimeoutInSeconds => ResponseTimeout.HasValue && ResponseTimeout > TimeSpan.Zero
            ? (int)ResponseTimeout.Value.TotalSeconds
            : (int?)null;

        /// <summary>
        /// Connection timeout, in seconds.
        /// </summary>
        [JsonProperty("connectTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        public int? ConnectionTimeoutInSeconds => ConnectionTimeout.HasValue && ConnectionTimeout > TimeSpan.Zero
            ? (int)ConnectionTimeout.Value.TotalSeconds
            : (int?)null;

        [JsonProperty("payload", NullValueHandling = NullValueHandling.Include)]
        internal JRaw JsonPayload { get; set; }

        /// <summary>
        /// Get the serialized JSON payload. May be null or empty.
        /// </summary>
        [JsonIgnore]
        public string Payload
        {
            get => (string)JsonPayload.Value;

            set
            {
                if (value == null)
                {
                    Payload = null;
                }
                else
                {
                    try
                    {
                        JToken.Parse(value);
                        JsonPayload = new JRaw(value);
                    }
                    catch (JsonException ex)
                    {
                        throw new ArgumentException(ex.Message, nameof(value));
                    }
                }
            }
        }

        private static void ValidatePayloadIsJson(string json)
        {
            try
            {
                JToken.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException(ex.Message, nameof(json)); // @ailn: here we want to hide the fact we're using Json.net
            }
        }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        internal string RequestId { get; set; }
    }
}
