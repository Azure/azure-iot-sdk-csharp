// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The payload convention class intended for serializing and deserializing JSON payloads.
    /// </summary>
    /// <remarks>
    /// For binary payloads for operations that support them, the client app should instead use
    /// <see cref="IncomingMessage.GetPayloadAsBytes"/> and <see cref="DirectMethodRequest.GetPayloadAsBytes"/>.
    /// </remarks>
    public abstract class PayloadConvention
    {
        /// <summary>
        /// Used to specify what type of content will be in the payload.
        /// </summary>
        /// <remarks>
        /// This can be free-form but should adhere to standard <see href="https://docs.w3cub.com/http/basics_of_http/mime_types.html">MIME types</see>,
        /// for example: "application/json".
        /// </remarks>
        /// <value>A string representing the content type to use when sending a payload.</value>
        public abstract string ContentType { get; }

        /// <summary>
        /// The encoding used for the payload.
        /// </summary>
        public abstract string ContentEncoding { get; }

        /// <summary>
        /// Returns the byte array for the convention-based serialized/encoded message.
        /// </summary>
        /// <remarks>
        /// Used by the client to take an object provided by the user and serialize to bytes for transport for operations like telemetry,
        /// direct method response, and reported properties.
        /// </remarks>
        /// <returns>The correctly encoded object for this convention.</returns>
        public abstract byte[] GetObjectBytes(object objectToSendWithConvention);

        /// <summary>
        /// Returns the object as the specified type.
        /// </summary>
        /// <remarks>
        /// Used by the client to deserialize the payload bytes from transport for operations like C2D messaging,
        /// direct method requests, and twin properties.
        /// <para>
        /// Used by the client to parse the entire twin document over MQTT; required only for <see cref="DefaultPayloadConvention"/>.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="objectToConvert">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public abstract T GetObject<T>(byte[] objectToConvert);
    }
}
