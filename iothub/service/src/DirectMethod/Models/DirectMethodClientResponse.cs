// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("status")]
        public int Status { get; protected internal set; }

        /// <summary>
        /// The serialized payload.
        /// </summary>
        [JsonIgnore]
        public string PayloadAsString => JsonPayload.HasValue
            ? JsonPayload.Value.GetRawText()
            : null;

        /// <summary>
        /// The deserialized payload.
        /// </summary>
        [JsonPropertyName("payload")]
        protected internal JsonElement? JsonPayload { get; set; }

        /// <summary>
        /// Tries to deserialize the payload as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="value">The value of the payload.</param>
        /// <returns>True if converted, otherwise false.</returns>
        public bool TryGetPayload<T>(out T value)
        {
            value = default;

            if (JsonPayload == null)
            {
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<T>(PayloadAsString);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
