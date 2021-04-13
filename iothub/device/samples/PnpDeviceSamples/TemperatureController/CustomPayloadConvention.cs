// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class CustomPayloadConvention : IPayloadConvention
    {
        public ISerializer PayloadSerializer => new CustomJsonSerializer();

        public IContentEncoder PayloadEncoder => Utf8ContentEncoder.Instance;

        public byte[] GetObjectBytes(object objectToSendWithConvention)
        {
            string serializedString = PayloadSerializer.SerializeToString(objectToSendWithConvention);
            return PayloadEncoder.EncodeStringToByteArray(serializedString);
        }
    }
}
