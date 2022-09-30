﻿// Copyright (c) Microsoft. All rights reserved.
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
            // This constructor is public because it can be used by
            // users invoking InvokeMethodAsync on an IotHubModuleClient.
        }

        /// <summary>
        /// The method name to invoke.
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
        /// The JSON payload in JRaw type.
        /// </summary>
        [JsonProperty("payload", NullValueHandling = NullValueHandling.Include)]
        protected internal JRaw JsonPayload { get; set; }

        /// <summary>
        /// The direct method payload.
        /// </summary>
        protected internal byte[] Payload { get; set; }

        /// <summary>
        /// Method timeout, in seconds.
        /// </summary>
        [JsonProperty("responseTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        protected internal int? ResponseTimeoutInSeconds => ResponseTimeout.HasValue && ResponseTimeout > TimeSpan.Zero
            ? (int)ResponseTimeout.Value.TotalSeconds
            : null;

        /// <summary>
        /// Connection timeout, in seconds.
        /// </summary>
        [JsonProperty("connectTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        protected internal int? ConnectionTimeoutInSeconds => ConnectionTimeout.HasValue && ConnectionTimeout > TimeSpan.Zero
            ? (int)ConnectionTimeout.Value.TotalSeconds
            : null;

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        /// <remarks>
        /// This value is not part of the Json payload. It is received as topic string parameter over MQTT and as a
        /// property over AMQP.
        /// </remarks>
        [JsonIgnore]
        protected internal string RequestId { get; set; }

        /// <summary>
        /// The convention to use with the direct method payload.
        /// </summary>
        protected internal PayloadConvention PayloadConvention { get; set; }

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

            // If the type to cast payload to is byte[] then return the payload as-is.
            if (Payload is T payloadRef)
            {
                payload = payloadRef;
                return true;
            }

            try
            {
                // If the type to cast payload to is string then return the payload string.
                // Else, deserialize it into the specified type

                payload = typeof(T) == typeof(string)
                    ? (T)(object)GetPayloadAsJsonString()
                    : PayloadConvention.PayloadSerializer.DeserializeToType<T>(GetPayloadAsJsonString());

                return true;
            }
            catch (Exception)
            {
                // In case the value cannot be converted using the serializer,
                // then return false with the default value of the type <T> passed in.
            }

            return false;
        }

        /// <summary>
        /// The command payload as a JSON string.
        /// </summary>
        public string GetPayloadAsJsonString()
        {
            return Payload.Length == 0
                ? null
                : PayloadConvention.PayloadEncoder.ContentEncoding.GetString(Payload);
        }
    }
}
