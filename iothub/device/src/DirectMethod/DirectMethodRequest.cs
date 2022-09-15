// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Parameters to execute a direct method on a device or module.
    /// </summary>
    public class DirectMethodRequest
    {
        /// <summary>
        /// Initialize an instance of this class.
        /// </summary>
        public DirectMethodRequest()
        {
        }

        /// <summary>
        /// Returns JSON payload in a custom type.
        /// </summary>
        /// <typeparam name="T">The custom type into which the JSON payload can be deserialized.</typeparam>
        /// <returns>The JSON payload in custom type.</returns>
        public T GetPayload<T>()
        {
            return JsonConvert.DeserializeObject<T>(PayloadAsJsonString);
        }

        /// <summary>
        /// Method to invoke.
        /// </summary>
        [JsonProperty("methodName", Required = Required.Always)]
        public string MethodName { get; set; }

        /// <summary>
        /// Method timeout.
        /// </summary>
        [JsonIgnore]
        public TimeSpan? ResponseTimeout { get; set; }

        /// <summary>
        /// Timeout for device to come online.
        /// </summary>
        [JsonIgnore]
        public TimeSpan? ConnectionTimeout { get; set; }

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

        /// <summary>
        /// Get the payload object. May be null or empty.
        /// </summary>
        [JsonIgnore]
        public object Payload
        {
            get => _payload;

            set
            {
                _payload = value;
                if (value != null)
                {
                    PayloadAsJsonString = JsonConvert.SerializeObject(value);
                    JsonPayload = new JRaw(PayloadAsJsonString);
                }
            }
        }

        /// <summary>
        /// Get the serialized JSON payload. May be null or empty.
        /// </summary>
        [JsonIgnore]
        public string PayloadAsJsonString { get; internal set; }

        /// <summary>
        /// Get the JSON payload in JRaw type.
        /// </summary>
        [JsonProperty("payload", NullValueHandling = NullValueHandling.Include)]
        public JRaw JsonPayload { get; internal set; }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        /// <remarks>
        /// This value is not part of the Json payload. It is received as topic string parameter over MQTT and as a
        /// property over AMQP.
        /// </remarks>
        [JsonIgnore]
        internal string RequestId { get; set; }

        private object _payload;
    }
}
