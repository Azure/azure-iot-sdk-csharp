// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The default implementation of the <see cref="IPayloadConvention"/> class.
    /// </summary>
    /// <remarks>
    /// This class is the default <see cref="IPayloadConvention"/> that will be used for all <see cref="PayloadCollection"/> implementations. This class makes use of the <see cref="JsonContentSerializer"/> serializer and the <see cref="Utf8ContentEncoder"/> unless another implementation is specified for these.
    /// </remarks>
    /// <example>
    /// You can overrride the default instances of both the <see cref="PayloadSerializer"/> and <see cref="PayloadEncoder"/> classes.
    /// <code lang='C#'>DefaultPayloadConvention.Instance.PayloadEncoder = new CustomPayloadEncoder();
    /// DefaultPayloadConvention.Instance.PayloadSerializer = new CustomPayloadSerializer();
    /// </code>
    /// </example>
    public class DefaultPayloadConvention : IPayloadConvention
    {
        /// <summary>
        /// A static instance of this class.
        /// </summary>
        public static readonly DefaultPayloadConvention Instance = new DefaultPayloadConvention();

        /// <summary>
        /// The payload serializer.
        /// </summary>
        public override ISerializer PayloadSerializer { get; set; }

        /// <summary>
        /// The payload encoder.
        /// </summary>
        public override IContentEncoder PayloadEncoder { get; set; }

       /// <inheritdoc/>
        public override byte[] GetObjectBytes(object objectToSendWithConvention)
        {
            string serializedString = PayloadSerializer.SerializeToString(objectToSendWithConvention);
            return PayloadEncoder.EncodeStringToByteArray(serializedString);
        }
    }
}
