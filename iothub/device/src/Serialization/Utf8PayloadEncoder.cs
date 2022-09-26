// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A UTF-8 PayloadEncoder implementation.
    /// </summary>
    public class Utf8PayloadEncoder : PayloadEncoder
    {
        private Utf8PayloadEncoder()
        {

        }

        /// <summary>
        /// The default instance of this class.
        /// </summary>
        public static Utf8PayloadEncoder Instance { get; } = new Utf8PayloadEncoder();

        /// <inheritdoc/>
        public override Encoding ContentEncoding => Encoding.UTF8;

        /// <inheritdoc/>
        public override byte[] EncodeStringToByteArray(string contentPayload)
        {
            return ContentEncoding.GetBytes(contentPayload);
        }
    }
}
