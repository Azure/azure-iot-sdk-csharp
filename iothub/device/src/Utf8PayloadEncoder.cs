// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A UTF-8 <see cref="PayloadEncoder"/> implementation.
    /// </summary>
    public class Utf8PayloadEncoder : PayloadEncoder
    {
        /// <summary>
        /// A static instance of this class.
        /// </summary>
        public static readonly Utf8PayloadEncoder Instance = new Utf8PayloadEncoder();

        /// <inheritdoc/>
        public override Encoding ContentEncoding => Encoding.UTF8;

        /// <inheritdoc/>
        public override byte[] EncodeStringToByteArray(string contentPayload)
        {
            return ContentEncoding.GetBytes(contentPayload);
        }
    }
}
