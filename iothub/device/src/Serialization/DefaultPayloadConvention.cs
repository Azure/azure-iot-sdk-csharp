// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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

        internal static Encoding Encoding { get; } = Encoding.UTF8;

        /// <summary>
        /// A static instance of this class.
        /// </summary>
        public static DefaultPayloadConvention Instance { get; } = new();

        /// <inheritdoc/>
        public override string ContentType => "application/json";

        /// <inheritdoc/>
        public override string ContentEncoding => Encoding.WebName;

        /// <inheritdoc/>
        public override byte[] GetObjectBytes(object objectToSendWithConvention)
        {
            string payloadString = Serialize(objectToSendWithConvention);
            return Encoding.GetBytes(payloadString);
        }

        /// <inheritdoc/>
        public override T GetObject<T>(byte[] objectToConvert)
        {
            if (objectToConvert is null)
            {
                return default;
            }

            string payloadString = Encoding.GetString(objectToConvert);
            return GetObject<T>(payloadString);
        }

        internal T GetObject<T>(Stream streamToConvert)
        {
            using var sw = new StreamReader(streamToConvert, Encoding);
            string body = sw.ReadToEnd();

            return GetObject<T>(body);
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Method must be instance to benefit from s_settings.")]
        internal T GetObject<T>(string jsonObjectAsText)
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
