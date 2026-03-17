// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The default implementation of the <see cref="PayloadConvention"/> class for Newtonsoft.Json.
    /// </summary>
    /// <remarks>
    /// This class makes use of the <see cref="JsonSerializer"/> serializer and the <see cref="Encoding.UTF8"/> encoder.
    /// </remarks>
    public sealed class DefaultPayloadConvention : PayloadConvention
    {
        private DefaultPayloadConvention()
        {
        }

        internal static Encoding Encoding => Encoding.UTF8;

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
            T deserialized;

            try
            {
                deserialized = JsonSerializer.Deserialize<T>(jsonObjectAsText, JsonSerializerSettings.Options);
            }
            catch (JsonException)
            {
                // T should always be of type byte[] here, so this is basically a no-op.
                // However, .NET does not allow us to simply return "payload as T" or other basic casts.
                deserialized = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(Encoding.UTF8.GetBytes(jsonObjectAsText), JsonSerializerSettings.Options));
            }
            return deserialized;
        }

        // For internal and unit testing use
        internal static string Serialize(object objectToSerialize)
        {
            return JsonSerializer.Serialize(objectToSerialize, JsonSerializerSettings.Options);
        }
    }
}
