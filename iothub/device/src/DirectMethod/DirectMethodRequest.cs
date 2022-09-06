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

        [JsonProperty("payload", NullValueHandling = NullValueHandling.Include)]
        internal JRaw JsonPayload { get; set; }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        [JsonIgnore] // not part of the Json payload
        internal string RequestId { get; set; }
    }
}
