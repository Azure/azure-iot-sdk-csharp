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
        private object _payload;

        /// <summary>
        /// Initialize an instance of this class.
        /// </summary>
        public DirectMethodRequest()
        {
            // This constructor is public because it can be used by
            // users invoking InvokeMethodAsync on an IotHubModuleClient.
        }

        /// <summary>
        /// Method to invoke.
        /// </summary>
        [JsonProperty("methodName", Required = Required.Always)]
        public string MethodName { get; set; }

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
        /// The JSON payload in JRaw type.
        /// </summary>
        [JsonProperty("payload", NullValueHandling = NullValueHandling.Include)]
        internal JRaw JsonPayload { get; set; }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        /// <remarks>
        /// This value is not part of the Json payload. It is received as topic string parameter over MQTT and as a
        /// property over AMQP.
        /// </remarks>
        [JsonIgnore]
        internal string RequestId { get; set; }

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
        /// The direct method request payload, deserialized to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the direct method request payload to.</typeparam>
        /// <param name="payload">When this method returns true, this contains the value of the direct method request payload.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if the direct method request payload can be deserialized to type <c>T</c>; otherwise, <c>false</c>.</returns>
        public bool TryGetPayload<T>(out T payload)
        {
            payload = default;
            string jsonPayload = PayloadAsJsonString;

            try
            {
                if (jsonPayload == null)
                {
                    return false;
                }

                payload = JsonConvert.DeserializeObject<T>(jsonPayload);
                return true;
            }
            catch (Exception)
            {
                // In case the value cannot be converted using the serializer,
                // then return false with the default value of the type <T> passed in.
            }

            return false;
        }
    }
}
