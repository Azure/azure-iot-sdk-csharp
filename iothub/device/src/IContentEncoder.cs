// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public abstract class IContentEncoder
    {
        /// <summary>
        /// Used by the Message class to specify what encoding to expect
        /// </summary>
        public abstract Encoding ContentEncoding { get; }

        /// <summary>
        /// Outputs an encoded byte array for the Message
        /// </summary>
        /// <param name="contentPayload">The contents of the message payload</param>
        /// <returns></returns>

        public abstract byte[] EncodeStringToByteArray(string contentPayload);
    }
}
