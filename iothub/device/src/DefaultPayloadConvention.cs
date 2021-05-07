// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The default implementation of the <see cref="PayloadConvention"/> class.
    /// </summary>
    /// <remarks>
    /// This class is the default <see cref="PayloadConvention"/> that will be used for all <see cref="PayloadCollection"/> implementations.
    /// This class makes use of the <see cref="NewtonsoftJsonPayloadSerializer"/> serializer and the <see cref="Utf8PayloadEncoder"/>.
    /// </remarks>
    public sealed class DefaultPayloadConvention : PayloadConvention
    {
        /// <summary>
        /// A static instance of this class.
        /// </summary>
        public static readonly DefaultPayloadConvention Instance = new DefaultPayloadConvention();

        /// <inheritdoc/>
        public override PayloadSerializer PayloadSerializer { get; } = NewtonsoftJsonPayloadSerializer.Instance;

        /// <inheritdoc/>
        public override PayloadEncoder PayloadEncoder { get; } = Utf8PayloadEncoder.Instance;
    }
}
