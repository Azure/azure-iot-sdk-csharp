// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// A <see cref="PayloadConvention"/> that uses <see cref="SystemTextJsonPayloadSerializer"/>.
    /// </summary>
    public class SystemTextJsonPayloadConvention : PayloadConvention
    {
        public static readonly SystemTextJsonPayloadConvention Instance = new SystemTextJsonPayloadConvention();

        public override PayloadSerializer PayloadSerializer { get; } = SystemTextJsonPayloadSerializer.Instance;

        public override PayloadEncoder PayloadEncoder { get; } = Utf8PayloadEncoder.Instance;
    }
}