// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Optional data to be included in the registration request.
    /// </summary>
    public class RegistrationRequestPayload
    {
        /// <summary>
        /// Additional (optional) JSON data to be sent to the service.
        /// </summary>
        /// <remarks>
        /// The service supports passing a DTDL model Id, so one supported payload is <see cref="ModelIdPayload"/>.
        /// </remarks>
        [JsonPropertyName("payload")]
        public JsonElement Payload { get; set; }

        /// <summary>
        /// Use if you have a serialized JSON string instead of a serializable object to set on <see cref="Payload"/>.
        /// </summary>
        /// <param name="payloadAsJsonString">A serilized JSON string to set as the payload.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="payloadAsJsonString"/> is null.</exception>
        /// <exception cref="ArgumentException">When <paramref name="payloadAsJsonString"/> is an empty string or white space.</exception>
        public void SetPayload(string payloadAsJsonString)
        {
            Argument.AssertNotNullOrEmpty(payloadAsJsonString, nameof(payloadAsJsonString));
            
            Payload = JsonDocument.Parse(payloadAsJsonString).RootElement;
        }

        /// <summary>
        /// Set the payload as a strongly typed object that can be serialized using System.Text.Json.
        /// </summary>
        /// <param name="serializableObject">The object to be serialized and used as the payload.</param>
        public void SetPayload(object serializableObject)
        {
            Argument.AssertNotNull(serializableObject, nameof(serializableObject));

            Payload = JsonSerializer.SerializeToElement(serializableObject, JsonSerializerSettings.Options);
        }
    }
}
