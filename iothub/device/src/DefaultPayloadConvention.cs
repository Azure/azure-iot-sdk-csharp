// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The default implementation of the <see cref="PayloadConvention"/> class.
    /// </summary>
    /// <remarks>
    /// This class is the default <see cref="PayloadConvention"/> that will be used for all <see cref="PayloadCollection"/> implementations. This class makes use of the <see cref="NewtonsoftJsonObjectSerializer"/> serializer and the <see cref="Utf8ContentEncoder"/>.
    /// </remarks>
    public sealed class DefaultPayloadConvention : PayloadConvention
    {
        /// <summary>
        /// A static instance of this class.
        /// </summary>
        public static readonly DefaultPayloadConvention Instance = new DefaultPayloadConvention();

        /// <inheritdoc/>
        public override ObjectSerializer PayloadSerializer { get; } = NewtonsoftJsonObjectSerializer.Instance;

        /// <inheritdoc/>
        public override ContentEncoder PayloadEncoder { get; } = Utf8ContentEncoder.Instance;

        /// <inheritdoc/>
        public override IWritablePropertyResponse CreateWritablePropertyResponse(object value, int statusCode, long version, string description = null)
        {
            return new NewtonsoftJsonWritablePropertyResponse(value, statusCode, version, description);
        }
    }
}
