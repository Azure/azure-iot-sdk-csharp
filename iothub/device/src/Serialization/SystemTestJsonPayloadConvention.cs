// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        /// <summary>
        /// A static instance of this class.
        /// </summary>
        public static readonly SystemTextJsonPayloadConvention Instance = new();

        /// <inheritdoc/>
        public override string ContentType => "application/json";

        /// <inheritdoc/>
        public override Encoding ContentEncoding => Encoding.UTF8;

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

        /// <inheritdoc/>
        public async override Task<T> GetObjectAsync<T>(Stream streamToConvert)
        {
            return await JsonSerializer.DeserializeAsync<T>(streamToConvert).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override T GetObject<T>(string jsonObjectAsText)
        {
            return JsonSerializer.Deserialize<T>(jsonObjectAsText);
        }
    }
}
