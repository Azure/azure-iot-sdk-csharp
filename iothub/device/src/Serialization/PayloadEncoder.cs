// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Specifies the byte encoding for the payload.
    /// </summary>
    /// <remarks>
    /// The encoder is responsible for encoding all objects into the correct bytes for the <see cref="PayloadConvention"/> that uses it.
    /// <para>
    /// By default, there are implementations of the <see cref="Utf8PayloadEncoder"/> class that uses <see cref="Encoding.UTF8"/>
    /// to handle the encoding for the <see cref="DefaultPayloadConvention"/> class.
    /// </para>
    /// </remarks>
    public abstract class PayloadEncoder
    {
        /// <summary>
        /// The encoding used for the payload.
        /// </summary>
        public abstract Encoding ContentEncoding { get; }

        /// <summary>
        /// Outputs an encoded byte array for the specified payload string.
        /// </summary>
        /// <param name="contentPayload">The contents of the message payload.</param>
        /// <returns>An encoded byte array.</returns>
        public abstract byte[] EncodeStringToByteArray(string contentPayload);
    }
}
