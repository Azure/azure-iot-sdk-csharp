// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The default implementation of the <see cref="IPayloadConvention"/> class.
    /// </summary>
    /// <remarks>
    /// This class is the default <see cref="IPayloadConvention"/> that will be used for all <see cref="PayloadCollection"/> implementations. This class makes use of the <see cref="JsonContentSerializer"/> serializer and the <see cref="Utf8ContentEncoder"/>.
    /// </remarks>
    public sealed class DefaultPayloadConvention : IPayloadConvention
    {
        /// <summary>
        /// A static instance of this class.
        /// </summary>
        public static readonly DefaultPayloadConvention Instance = new DefaultPayloadConvention();

        /// <summary>
        /// The payload serializer.
        /// </summary>
        public override ISerializer PayloadSerializer { get; } = JsonContentSerializer.Instance;

        /// <summary>
        /// The payload encoder.
        /// </summary>
        public override IContentEncoder PayloadEncoder { get; } = Utf8ContentEncoder.Instance;
    }
}
