// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices
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
        /// The method name to run.
        /// </summary>
        [JsonProperty("methodName", Required = Required.Always)]
        public string MethodName { get; set; }

        /// <summary>
        /// The amount of time given to the service to connect to the device.
        /// </summary>
        /// <remarks>
        /// A timeout may occur if this value is set to zero and the target device is not connected to
        /// the cloud.
        /// If the value is greater than zero, it may also occur if the cloud fails to deliver the request to
        /// the target device.
        /// <para>
        /// This value is propagated to the service in terms of seconds, so this value does not have a level of
        /// precision below seconds. For example, a value of <c>TimeSpan.FromMilliseconds(500)</c> will be
        /// interpreted as 0 seconds (using <c>ConnectTimeout.TotalSeconds</c>).
        /// </para>
        /// </remarks>
        [JsonIgnore]
        public TimeSpan? ConnectionTimeout { get; set; }

        /// <summary>
        /// The amount of time given to the device to process and respond to the command request.
        /// </summary>
        /// <remarks>
        /// This timeout may happen if the target device is slow in handling the direct method.
        /// <para>
        /// This value is propagated to the service in terms of seconds, so this value does not have a level of
        /// precision below seconds. For example, setting this value to TimeSpan.FromMilliseconds(500) will result
        /// in this request having a timeout of 0 seconds.
        /// </para>
        /// </remarks>
        [JsonIgnore]
        public TimeSpan? ResponseTimeout { get; set; }

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
        [JsonProperty("payload")]
        internal JRaw JsonPayload { get; set; }

        [JsonProperty("responseTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ResponseTimeoutInSeconds => (int?)ResponseTimeout?.TotalSeconds ?? null;

        [JsonProperty("connectTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ConnectionTimeoutInSeconds => (int?)ConnectionTimeout?.TotalSeconds ?? null;
    }
}
