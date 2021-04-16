// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public class DefaultPayloadConvention : IPayloadConvention
    {
        /// <summary>
        ///
        /// </summary>
        public static readonly DefaultPayloadConvention Instance = new DefaultPayloadConvention();

        /// <summary>
        ///
        /// </summary>
        public override ISerializer PayloadSerializer => JsonContentSerializer.Instance;

        /// <summary>
        ///
        /// </summary>
        public override IContentEncoder PayloadEncoder => Utf8ContentEncoder.Instance;

        /// <summary>
        ///
        /// </summary>
        /// <param name="objectToSendWithConvention"></param>
        /// <returns></returns>
        public override byte[] GetObjectBytes(object objectToSendWithConvention)
        {
            string serializedString = PayloadSerializer.SerializeToString(objectToSendWithConvention);
            return PayloadEncoder.EncodeStringToByteArray(serializedString);
        }
    }
}
