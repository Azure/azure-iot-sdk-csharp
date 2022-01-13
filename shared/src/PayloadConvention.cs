// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The payload convention class.
    /// </summary>
    /// <remarks>
    /// The payload convention is used to define a specific serializer as well as a specific content encoding.
    /// For example, Azure IoT has a <see href="https://docs.microsoft.com/azure/iot-pnp/concepts-convention">Plug and Play convention</see>
    /// that is designed to make it easier to get started with products that use specific conventions by default.
    /// </remarks>
    public abstract class PayloadConvention
    {
        /// <summary>
        /// Gets the serializer used for the payload.
        /// </summary>
        /// <value>A serializer that will be used to convert the payload object to a string.</value>
        public abstract PayloadSerializer PayloadSerializer { get; }

        /// <summary>
        /// Gets the encoder used for the payload to be serialized.
        /// </summary>
        /// <value>An encoder that will be used to convert the serialized string to a byte array.</value>
        public abstract PayloadEncoder PayloadEncoder { get; }

        /// <summary>
        /// Returns the byte array for the convention-based message.
        /// </summary>
        /// <remarks>This will use the <see cref="PayloadSerializer"/> and <see cref="PayloadEncoder"/> to create the byte array.</remarks>
        /// <param name="objectToSendWithConvention">The convention-based message to be sent.</param>
        /// <returns>The correctly encoded object for this convention.</returns>
        public virtual byte[] GetObjectBytes(object objectToSendWithConvention)
        {
            string serializedString = PayloadSerializer.SerializeToString(objectToSendWithConvention);
            return PayloadEncoder.EncodeStringToByteArray(serializedString);
        }
    }
}
