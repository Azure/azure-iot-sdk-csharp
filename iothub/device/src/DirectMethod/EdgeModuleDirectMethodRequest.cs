// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Parameters to execute a direct method on an edge device or an edge module by an <see cref="IotHubModuleClient"/>.
    /// </summary>
    public class EdgeModuleDirectMethodRequest
    {
        /// <summary>
        /// A direct method request to be initialized by the client application when using an <see cref="IotHubModuleClient"/> for invoking
        /// a direct method on an edge device or an edge module connected to the same edge hub.
        /// </summary>
        /// <param name="methodName">The method name to invoke.</param>
        public EdgeModuleDirectMethodRequest(string methodName)
            : this(methodName, null)
        {
        }

        /// <summary>
        /// A direct method request to be initialized by the client application when using an <see cref="IotHubModuleClient"/> for invoking
        /// a direct method on an edge device or an edge module connected to the same edge hub.
        /// </summary>
        /// <param name="methodName">The method name to invoke.</param>
        /// <param name="payload">The direct method payload that will be serialized using <see cref="DefaultPayloadConvention"/>.</param>
        public EdgeModuleDirectMethodRequest(string methodName, byte[] payload)
        {
            MethodName = methodName;
            Payload = payload;
        }

        /// <summary>
        /// The method name to invoke.
        /// </summary>
        [JsonProperty("methodName")]
        public string MethodName { get; }

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
        /// The convention to use with the direct method payload.
        /// </summary>
        [JsonIgnore]
        internal PayloadConvention PayloadConvention { get; set; }

        /// <summary>
        /// The direct method payload.
        /// </summary>
        [JsonProperty("payload", NullValueHandling = NullValueHandling.Include)]
        internal byte[] Payload { get; }

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
            catch (Exception ex) when (Logging.IsEnabled)
            {
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