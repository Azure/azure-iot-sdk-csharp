// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The payload convention class.
    /// </summary>
    /// <remarks>The payload convention is used to define a specific serializer as well as a specific content encoding. For example, IoT has a <see href="https://docs.microsoft.com/en-us/azure/iot-pnp/concepts-convention">convention</see> that is designed to make it easier to get started with products that use specific conventions by default.</remarks>
    public abstract class IPayloadConvention
    {
        /// <summary>
        /// The serializer used for the payload of the <see cref="Message"/>.
        /// </summary>
        public abstract ISerializer PayloadSerializer { get; set; }

        /// <summary>
        /// The encoder used for the payload of the <see cref="Message"/>.
        /// </summary>
        public abstract IContentEncoder PayloadEncoder { get; set; }

        /// <summary>
        /// Returns the byte array for the convention based message.
        /// </summary>
        /// <param name="objectToSendWithConvention"></param>
        /// <returns></returns>
        public abstract byte[] GetObjectBytes(object objectToSendWithConvention);
    }
}
