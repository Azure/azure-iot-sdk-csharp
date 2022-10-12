// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A <see cref="PayloadConvention"/> that uses <see cref="SystemTextJsonPayloadSerializer"/>.
    /// </summary>
    public class SystemTextJsonPayloadConvention : PayloadConvention
    {
        /// <summary>
        /// A static instance of this class.
        /// </summary>
        public static readonly SystemTextJsonPayloadConvention Instance = new();

        /// <inheritdoc/>
        public override PayloadSerializer PayloadSerializer { get; } = SystemTextJsonPayloadSerializer.Instance;

        /// <inheritdoc/>
        public override PayloadEncoder PayloadEncoder { get; } = Utf8PayloadEncoder.Instance;
    }
}
