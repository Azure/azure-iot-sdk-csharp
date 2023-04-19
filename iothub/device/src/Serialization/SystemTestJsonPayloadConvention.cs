// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// An implementation of the <see cref="PayloadConvention"/> class for System.Text.Json".
    /// </summary>
    /// <remarks>
    /// This class makes use of the <see cref="JsonSerializer"/> serializer and the <see cref="Encoding.UTF8"/> encoder.
    /// </remarks>
    public class SystemTextJsonPayloadConvention : PayloadConvention
    {
        private SystemTextJsonPayloadConvention() { }

        internal static Encoding Encoding => Encoding.UTF8;

        /// <summary>
        /// A static instance of this class.
        /// </summary>
        public static SystemTextJsonPayloadConvention Instance => new();

        /// <inheritdoc/>
        public override string ContentType => "application/json";

        /// <inheritdoc/>
        public override string ContentEncoding => Encoding.WebName;

        /// <inheritdoc/>
        public override byte[] GetObjectBytes(object objectToSendWithConvention)
        {
            return JsonSerializer.SerializeToUtf8Bytes(objectToSendWithConvention);
        }

        /// <inheritdoc/>
        public override T GetObject<T>(byte[] objectToConvert)
        {
            return JsonSerializer.Deserialize<T>(objectToConvert);
        }
    }
}
