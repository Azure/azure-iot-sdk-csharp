// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A <see cref="System.Text.Json"/> <see cref="PayloadSerializer"/> implementation.
    /// </summary>
    public class SystemTextJsonPayloadSerializer : PayloadSerializer
    {
        /// <summary>
        /// The Content Type string.
        /// </summary>
        internal const string ApplicationJson = "application/json";

        /// <summary>
        /// The default instance of this class.
        /// </summary>
        public static readonly SystemTextJsonPayloadSerializer Instance = new();

        /// <inheritdoc/>
        public override string ContentType => ApplicationJson;

        /// <inheritdoc/>
        public override string SerializeToString(object objectToSerialize)
        {
            return JsonSerializer.Serialize(objectToSerialize);
        }

        /// <inheritdoc/>
        public override T DeserializeToType<T>(string stringToDeserialize)
        {
            return JsonSerializer.Deserialize<T>(stringToDeserialize);
        }

        /// <inheritdoc/>
        public override T ConvertFromJsonObject<T>(object objectToConvert)
        {
            return DeserializeToType<T>(SerializeToString(objectToConvert));
        }
    }
}
