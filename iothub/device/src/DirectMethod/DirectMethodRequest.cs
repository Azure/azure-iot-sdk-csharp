// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Parameters to execute a direct method on a device or module.
    /// </summary>
    /// <remarks>
    /// A direct method request can only be made by the service or a module.
    /// </remarks>
    public class DirectMethodRequest
    {
        /// <summary>
        /// For serialization.
        /// </summary>
        internal DirectMethodRequest()
        {
        }

        /// <summary>
        /// Initialize an instance of this class.
        /// </summary>
        /// <remarks>
        /// This class can be inherited from and set by unit tests for mocking purposes.
        /// </remarks>
        /// <param name="methodName">The method name to invoke.</param>
        protected internal DirectMethodRequest(string methodName)
        {
            MethodName = methodName;
        }

        /// <summary>
        /// The method name to invoke.
        /// </summary>
        [JsonProperty("methodName")]
        public string MethodName { get; private set; }

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
        /// The request Id for the transport layer.
        /// </summary>
        /// <remarks>
        /// This value is not part of the JSON payload. It is received as topic string parameter over MQTT and as a
        /// property over AMQP, and would likely only be interesting to a device app for diagnostics.
        /// </remarks>
        [JsonIgnore]
        public string RequestId { get; protected internal set; }

        /// <summary>
        /// The direct method payload.
        /// </summary>
        [JsonProperty("payload", NullValueHandling = NullValueHandling.Include)]
        protected internal byte[] Payload { get; set; }

        /// <summary>
        /// The convention to use with the direct method payload.
        /// </summary>
        [JsonIgnore]
        protected internal PayloadConvention PayloadConvention { get; set; }

        /// <summary>
        /// Method timeout, in seconds.
        /// </summary>
        [JsonProperty("responseTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ResponseTimeoutInSeconds => ResponseTimeout.HasValue && ResponseTimeout > TimeSpan.Zero
            ? (int)ResponseTimeout.Value.TotalSeconds
            : null;

        /// <summary>
        /// Connection timeout, in seconds.
        /// </summary>
        [JsonProperty("connectTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ConnectionTimeoutInSeconds => ConnectionTimeout.HasValue && ConnectionTimeout > TimeSpan.Zero
            ? (int)ConnectionTimeout.Value.TotalSeconds
            : null;

        /// <summary>
        /// The direct method request payload, deserialized to the specified type.
        /// </summary>
        /// <remarks>
        /// Use this method when the payload type is known and it can be deserialized using the configured
        /// <see cref="PayloadConvention"/>. If it is not JSON or the type is not known, use <see cref="GetPayloadAsBytes"/>.
        /// </remarks>
        /// <typeparam name="T">The type to deserialize the direct method request payload to.</typeparam>
        /// <param name="payload">When this method returns true, this contains the value of the direct method request payload.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if the direct method request payload can be deserialized to type <c>T</c>; otherwise, <c>false</c>.</returns>
        /// <example>
        /// <code language="csharp">
        /// await client.SetDirectMethodCallbackAsync((directMethodRequest) =>
        /// {
        ///     if (directMethodRequest.TryGetPayload(out MyCustomType customTypePayload))
        ///     {
        ///         // do work
        ///         // ...
        ///         
        ///         // Acknowlege the direct method call with the status code 200.  
        ///         return Task.FromResult(new DirectMethodResponse(200));
        ///     }
        ///     else
        ///     {
        ///         // Acknowlege the direct method call the status code 400.
        ///         return Task.FromResult(new DirectMethodResponse(400));
        ///     }
        ///     
        ///     
        ///     // ...
        /// },
        /// cancellationToken);
        /// </code>
        /// </example>
        public bool TryGetPayload<T>(out T payload)
        {
            payload = default;

            try
            {
                payload = PayloadConvention.GetObject<T>(Payload);
                return true;
            }
            catch (Exception ex)
            {
                // In case the value cannot be converted using the serializer,
                // then return false with the default value of the type <T> passed in.
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Unable to convert payload to {typeof(T)} due to {ex}", nameof(TryGetPayload));
            }

            return false;
        }

        /// <summary>
        /// Get the raw payload bytes.
        /// </summary>
        /// <remarks>
        /// Use this method when the payload is not JSON or the type is not known or the type cannot be deserialized
        /// using the configured <see cref="PayloadConvention"/>. Otherwise, use <see cref="TryGetPayload{T}(out T)"/>.
        /// </remarks>
        /// <returns>A copy of the raw payload as a byte array.</returns>
        /// <example>
        /// <code language="csharp">
        /// await client.SetDirectMethodCallbackAsync((directMethodRequest) =>
        /// {
        ///     byte[] arr = directMethodRequest.GetPayloadAsBytes();
        ///     // deserialize as needed and do work...
        ///     
        ///     // Acknowlege the direct method call with the status code 200. 
        ///     return Task.FromResult(new DirectMethodResponse(200));
        /// },
        /// cancellationToken);
        /// </code>
        /// </example>
        public byte[] GetPayloadAsBytes()
        {
            return Payload == null || Payload.Length == 0 ? null : (byte[])Payload.Clone();
        }
    }
}
