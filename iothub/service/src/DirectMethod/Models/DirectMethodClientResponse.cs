// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The device/module's response to a direct method invocation.
    /// </summary>
    public class DirectMethodClientResponse
    {
        /// <summary>
        /// This constructor is for deserialization and unit test mocking purposes.
        /// </summary>
        /// <remarks>
        /// To unit test methods that use this type as a response, inherit from this class and give it a constructor
        /// that can set the properties you want.
        /// </remarks>
        protected internal DirectMethodClientResponse()
        { }

        /// <summary>
        /// Gets or sets the status of device method invocation.
        /// </summary>
        /// <remarks>
        /// Can be any status code value (int), but it is recommended to use
        /// HTTP status codes, which are well-known and documented.
        /// </remarks>
        [JsonProperty("status")]
        public int Status { get; protected internal set; }

        /// <summary>
        /// Get the payload as a JSON string.
        /// </summary>
        /// <remarks>
        /// To get the payload as a specified type, use <see cref="TryGetPayload{T}(out T)"/>.
        /// </remarks>
        [JsonIgnore]
        public string PayloadAsString => JsonPayload.Value<string>();

        [JsonIgnore]
        internal byte[] PayloadAsBytes => Encoding.UTF8.GetBytes(PayloadAsString);

        [JsonProperty("payload")]
        internal JRaw JsonPayload { get; set; }

        /// <summary>
        /// Tries to deserialize the payload as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="value">The value of the payload.</param>
        /// <returns>True if converted, otherwise false.</returns>
        /// <example>
        /// <code language="csharp">
        /// DirectMethodClientResponse methodResponse = await client.DirectMethods
        ///     .InvokeAsync(deviceId, directMethodRequest, ct)
        ///     .ConfigureAwait(false);
        ///     
        /// methodResponse.TryGetPayload(out MyCustomType customTypePayload);
        ///     
        /// // deserialize as needed and do work...
        /// </code>
        /// </example>
        public bool TryGetPayload<T>(out T value)
        {
            value = default;

            if (JsonPayload == null)
            {
                return false;
            }

            try
            {
                if (typeof(T) == typeof(byte[]))
                {
                    value = JsonConvert.DeserializeObject<T>(PayloadAsString);
                    return true;
                }

                // If not deserializing into byte[], an extra layer of decoding is needed
                string decodedPayload = MultipleDecode(PayloadAsBytes);
                value = JsonConvert.DeserializeObject<T>(decodedPayload);
                return true;
            }
            catch (JsonSerializationException)
            {
                return false;
            }
        }

        internal static string MultipleDecode(byte[] bytes)
        {
            return Encoding.UTF8.GetString(JsonConvert.DeserializeObject<byte[]>(Encoding.UTF8.GetString(bytes)));
        }
    }
}
