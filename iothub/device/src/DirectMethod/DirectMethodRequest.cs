﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
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
        private readonly byte[] _payload;

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
        /// <param name="payload">The direct method payload.</param>
        protected internal DirectMethodRequest(string methodName, byte[] payload = default)
        {
            MethodName = methodName;
            _payload = payload;
        }

        /// <summary>
        /// The method name to invoke.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        /// <remarks>
        /// This value is not part of the JSON payload. It is received as topic string parameter over MQTT and as a
        /// property over AMQP, and would likely only be interesting to a device app for diagnostics.
        /// </remarks>
        public string RequestId { get; protected internal set; }

        /// <summary>
        /// The convention to use with the direct method payload.
        /// </summary>
        protected internal PayloadConvention PayloadConvention { get; set; }

        /// <summary>
        /// The direct method request payload, deserialized to the specified type.
        /// </summary>
        /// <remarks>
        /// Use this method when the payload type is known and it can be deserialized using the configured
        /// <see cref="PayloadConvention"/>. If it is not JSON or the type is not known, use <see cref="GetPayload"/>.
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
                if (typeof(T) == typeof(byte[]))
                {
                    payload = PayloadConvention.GetObject<T>(_payload);
                    return true;
                }

                // If not deserializing into byte[], an extra layer of decoding is needed
                payload = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(PayloadConvention.GetObject<byte[]>(_payload)));
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
        /// Get the payload of the direct method.
        /// </summary>
        /// <remarks>
        /// This method returns the payload decoded into a byte array to then be further deserialized.
        /// </remarks>
        /// <returns>The payload decoded into a byte array or the raw bytes themselves if not possible.</returns>
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
        public byte[] GetPayload()
        {
            return _payload == null || _payload.Length == 0 ? null : PayloadConvention.GetObject<byte[]>(_payload);
        }
    }
}