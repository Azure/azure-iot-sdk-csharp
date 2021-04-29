﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The payload convention class.
    /// </summary>
    /// <remarks>The payload convention is used to define a specific serializer as well as a specific content encoding. For example, IoT has a <see href="https://docs.microsoft.com/en-us/azure/iot-pnp/concepts-convention">convention</see> that is designed to make it easier to get started with products that use specific conventions by default.</remarks>
    public abstract class PayloadConvention
    {
        /// <summary>
        /// Gets the serializer used for the payload.
        /// </summary>
        /// <value>A serializer that will be used to convert the payload object to a string.</value>
        public abstract ObjectSerializer PayloadSerializer { get; }

        /// <summary>
        /// Gets the encoder used for the payload to be serialized.
        /// </summary>
        /// <value>An encoder that will be used to convert the serialized string to a byte array.</value>
        public abstract ContentEncoder PayloadEncoder { get; }

        /// <summary>
        /// Returns the byte array for the convention based message.
        /// </summary>
        /// <remarks>This base class will use the <see cref="ObjectSerializer"/> and <see cref="ContentEncoder"/> to create this byte array.</remarks>
        /// <param name="objectToSendWithConvention"></param>
        /// <returns>The correctly encoded object for this convention.</returns>
        public virtual byte[] GetObjectBytes(object objectToSendWithConvention)
        {
            string serializedString = PayloadSerializer.SerializeToString(objectToSendWithConvention);
            return PayloadEncoder.EncodeStringToByteArray(serializedString);
        }

        /// <summary>
        /// Creates the correct <see cref="IWritablePropertyResponse"/> to be used with this serializer
        /// </summary>
        /// <param name="value">The value of the property.</param>
        /// <param name="statusCode">The status code of the write operation.</param>
        /// <param name="version">The version the property is responding to.</param>
        /// <param name="description">An optional description of the writable property response.</param>
        /// <returns></returns>
        public abstract IWritablePropertyResponse CreateWritablePropertyResponse(object value, int statusCode, long version, string description = default);
    }
}
