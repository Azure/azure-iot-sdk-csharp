// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A UTF-8 <see cref="ContentEncoder"/> implementation.
    /// </summary>
    public class Utf8ContentEncoder : ContentEncoder
    {
        /// <summary>
        /// A static instance of this class.
        /// </summary>
        public static readonly Utf8ContentEncoder Instance = new Utf8ContentEncoder();

        /// <inheritdoc/>
        public override Encoding ContentEncoding => Encoding.UTF8;

        /// <inheritdoc/>
        public override byte[] EncodeStringToByteArray(string contentPayload)
        {
            return ContentEncoding.GetBytes(contentPayload);
        }
    }
}
