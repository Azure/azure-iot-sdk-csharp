// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Parameters to execute a direct method on the device.
    /// </summary>
    public class DirectMethodRequest
    {
        /// <summary>
        /// The method name to run.
        /// </summary>
        [JsonProperty("methodName", Required = Required.Always)]
        public string MethodName { get; set; }

        /// <summary>
        /// The timeout before the direct method request will fail if the device doesn't respond to the request.
        /// This timeout may happen if the target device is slow in handling the direct method.
        /// </summary>
        /// <remarks>
        /// This value is propagated to the service in terms of seconds, so this value does not have a level of
        /// precision below seconds. For example, setting this value to TimeSpan.FromMilliseconds(500) will result
        /// in this request having a timeout of 0 seconds.
        /// </remarks>
        [JsonIgnore]
        public TimeSpan? ResponseTimeout { get; set; }

        /// <summary>
        /// The timeout before the direct method request will fail if the request takes too long to reach the device.
        /// This timeout may happen if the target device is not connected to the cloud or if the cloud fails to deliver
        /// the request to the target device in time. If this value is set to 0 seconds, then the target device must be online
        /// when this direct method request is made.
        /// </summary>
        /// <remarks>
        /// This value is propagated to the service in terms of seconds, so this value does not have a level of
        /// precision below seconds. For example, setting this value to TimeSpan.FromMilliseconds(500) will result
        /// in this request having a timeout of 0 seconds.
        /// </remarks>
        [JsonIgnore]
        public TimeSpan? ConnectionTimeout { get; set; }

        [JsonProperty("responseTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ResponseTimeoutInSeconds => ResponseTimeout != null ? (int)ResponseTimeout?.TotalSeconds : null;

        [JsonProperty("connectTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ConnectionTimeoutInSeconds => ConnectionTimeout != null ? (int)ConnectionTimeout?.TotalSeconds : null;

        [JsonProperty("payload")]
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
    }
}
