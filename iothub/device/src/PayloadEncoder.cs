// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// This class specifies the byte encoding for the the payload.
    /// </summary>
    /// <remarks>
    /// The encoder is responsible for encoding all of your objects into the corrent bytes for the <see cref="PayloadConvention"/> that uses it.
    /// <para>
    /// By default we have implemented the <see cref="Utf8PayloadEncoder"/> class that uses <see cref="System.Text.Encoding.UTF8"/>
    /// to handle the encoding for the <see cref="DefaultPayloadConvention"/> class.
    /// </para>
    /// </remarks>
    public abstract class PayloadEncoder
    {
        /// <summary>
        /// The <see cref="Encoding"/> used for the payload.
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
