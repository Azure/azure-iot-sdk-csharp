// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public object Payload { get; set; }

        /// <summary>
        /// Use if you have a serialized JSON string instead of a serializable object to set on <see cref="Payload"/>.
        /// </summary>
        /// <param name="payloadAsJsonString">A serilized JSON string to set as the payload.</param>
        public void SetPayload(string payloadAsJsonString)
        {
            using var jd = JsonDocument.Parse(payloadAsJsonString);

            // JsonDocument is IDisposable. Since the SDK needs to work with a particular element within the JSON document,
            // we need to return the clone of that JsonElement.
            // If we return the RootElement or a sub-element directly without making a clone,
            // the caller won't be able to access the returned JsonElement after the JsonDocument that owns it is disposed.
            // https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/use-dom-utf8jsonreader-utf8jsonwriter?pivots=dotnet-6-0#jsondocument-is-idisposable
            Payload = jd.RootElement.Clone();
        }
    }
}
