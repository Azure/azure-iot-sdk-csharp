// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public class Utf8ContentEncoder : IContentEncoder
    {
        /// <summary>
        ///
        /// </summary>
        public static readonly Utf8ContentEncoder Instance = new Utf8ContentEncoder();

        /// <summary>
        ///
        /// </summary>
        public Encoding ContentEncoding => Encoding.UTF8;

        /// <summary>
        ///
        /// </summary>
        /// <param name="contentPayload"></param>
        /// <returns></returns>
        public byte[] EncodeStringToByteArray(string contentPayload)
        {
            return ContentEncoding.GetBytes(contentPayload);
        }
    }
}
