// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The default implementation of the <see cref="PayloadConvention"/> class for Newtonsoft.Json.
    /// </summary>
    /// <remarks>
    /// This class makes use of the <see cref="JsonConvert"/> serializer and the <see cref="Encoding.UTF8"/> encoder.
    /// </remarks>
    public sealed class DefaultPayloadConvention : PayloadConvention
    {
        private static readonly JsonSerializer s_jsonSerializer = new();
        internal static readonly Encoding s_encoding = Encoding.UTF8;

        /// <summary>
        /// A static instance of JsonSerializerSettings which sets DateParseHandling to None.
        /// </summary>
        /// <remarks>
        /// By default, serializing/deserializing with Newtonsoft.Json will try to parse date-formatted
        /// strings to a date type, which drops trailing zeros in the microseconds portion. By
        /// specifying DateParseHandling with None, the original string will be read as-is. For more details
        /// about the known issue, see https://github.com/JamesNK/Newtonsoft.Json/issues/1511.
        /// </remarks>
        private static readonly JsonSerializerSettings s_settings = new()
        {
            DateParseHandling = DateParseHandling.None,
        };

        private DefaultPayloadConvention()
        {
            JsonConvert.DefaultSettings = () => s_settings;
        }

        /// <summary>
        /// A static instance of this class.
        /// </summary>
        public static DefaultPayloadConvention Instance { get; } = new DefaultPayloadConvention();

        /// <inheritdoc/>
        public override string ContentType => "application/json";

        /// <inheritdoc/>
        public override string ContentEncoding => s_encoding.WebName;

        /// <inheritdoc/>
        public override byte[] GetObjectBytes(object objectToSendWithConvention)
        {
            string payloadString = Serialize(objectToSendWithConvention);
            return s_encoding.GetBytes(payloadString);
        }

        /// <inheritdoc/>
        public override T GetObject<T>(byte[] objectToConvert)
        {
            if (objectToConvert is null)
            {
                return default;
            }

            string payloadString = s_encoding.GetString(objectToConvert);
            return GetObject<T>(payloadString);
        }

        /// <inheritdoc/>
        public override T GetObject<T>(Stream streamToConvert)
        {
            using var sw = new StreamReader(streamToConvert, s_encoding);
            string body = sw.ReadToEnd();

            return GetObject<T>(body);
        }

        /// <inheritdoc/>
        public override T GetObject<T>(string jsonObjectAsText)
        {
            return JsonConvert.DeserializeObject<T>(jsonObjectAsText);
        }

        // For internal and unit testing use
        internal static string Serialize(object objectToSerialize)
        {
            return JsonConvert.SerializeObject(objectToSerialize);
        }
    }
}
