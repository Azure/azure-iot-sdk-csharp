// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The device/module's response to a direct method invocation.
    /// </summary>
    public class DirectMethodResponse
    {
        /// <summary>
        /// Initializes an instance of this class.
        /// </summary>
        public DirectMethodResponse(int status)
        {
            Status = status;
        }

        /// <summary>
        /// The status of direct method response.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// The optional direct method payload.
        /// </summary>
        /// <remarks>
        /// The payload can be null or primitive type (e.g., string, int/array/list/dictionary/custom type)
        /// </remarks>
        public object Payload { get; set; }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        internal string RequestId { get; set; }

        /// <summary>
        /// The convention to use with the direct method payload.
        /// </summary>
        protected internal PayloadConvention PayloadConvention { get; set; }

        /// <summary>
        /// The direct method response payload, deserialized to the specified type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method when the payload type is known and it can be deserialized using the configured
        /// <see cref="PayloadConvention"/>. If it is not JSON or the type is not known, use <see cref="GetPayloadAsBytes"/>.
        /// </para>
        /// <para>
        /// One intended usage of this method is to deserialize the direct method response received by an edge module client
        /// after it invokes a direct method on an edge device or an edge module connected to the same edge hub.
        /// These operations are invoked using the API <see cref="IotHubModuleClient.InvokeMethodAsync(string, EdgeModuleDirectMethodRequest, System.Threading.CancellationToken)"/>
        /// and <see cref="IotHubModuleClient.InvokeMethodAsync(string, string, EdgeModuleDirectMethodRequest, System.Threading.CancellationToken)"/>.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">The type to deserialize the direct method response payload to.</typeparam>
        /// <param name="payload">When this method returns true, this contains the value of the direct method response payload.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if the direct method response payload can be deserialized to type <c>T</c>; otherwise, <c>false</c>.</returns>
        /// <example>
        /// <code language="csharp">
        /// DirectMethodResponse response = await client
        ///     .InvokeMethodAsync(deviceId, moduleId, directMethodRequest, cancellationToken)
        ///     .ConfigureAwait(false);
        /// if (response.TryGetPayload(out MyCustomType customTypePayload))
        /// {
        ///     // do work
        ///     // ...
        /// }
        ///
        /// // ...
        /// </code>
        /// </example>
        public bool TryGetPayload<T>(out T payload)
        {
            payload = default;

            try
            {
                payload = PayloadConvention.GetObject<T>(GetPayloadAsBytes());
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
        /// DirectMethodResponse response = await client
        ///     .InvokeMethodAsync(deviceId, moduleId, directMethodRequest, cancellationToken)
        ///     .ConfigureAwait(false);
        ///
        /// // Get the payload as bytes
        /// byte[] arr = directMethodRequest.GetPayloadAsBytes();
        ///
        /// // deserialize as needed and do work...
        ///
        /// // ...
        /// </code>
        /// </example>
        public byte[] GetPayloadAsBytes()
        {
            return Payload == null
                ? null
                : PayloadConvention.GetObjectBytes(Payload);
        }
    }
}
