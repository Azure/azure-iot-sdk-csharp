﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A <see cref="Newtonsoft.Json.JsonConvert"/> <see cref="ObjectSerializer"/> implementation.
    /// </summary>
    public class NewtonsoftJsonObjectSerializer : ObjectSerializer
    {
        /// <summary>
        /// The Content Type string.
        /// </summary>
        internal const string ApplicationJson = "application/json";

        /// <summary>
        /// The default instance of this class.
        /// </summary>
        public static readonly NewtonsoftJsonObjectSerializer Instance = new NewtonsoftJsonObjectSerializer();

        /// <inheritdoc/>
        public override string ContentType => ApplicationJson;

        /// <inheritdoc/>
        public override string SerializeToString(object objectToSerialize)
        {
            return JsonConvert.SerializeObject(objectToSerialize);
        }

        /// <inheritdoc/>
        public override T DeserializeToType<T>(string stringToDeserialize)
        {
            return JsonConvert.DeserializeObject<T>(stringToDeserialize);
        }

        /// <inheritdoc/>
        public override T ConvertFromObject<T>(object objectToConvert)
        {
            if (objectToConvert == null)
            {
                return default;
            }
            return ((JObject)objectToConvert).ToObject<T>();
        }
    }
}
