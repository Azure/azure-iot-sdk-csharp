// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Gets and sets the twin desired properties.
    /// </summary>
    public class ClientTwinProperties
    {
        /// <summary>
        /// Gets the version of the collection.
        /// </summary>
        [JsonPropertyName("$version")]
        public long? Version { get; set; }

        /// <summary>
        /// Metadata about the properties.
        /// </summary>
        [JsonPropertyName("$metadata")]
        public ClientTwinMetadata Metadata { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonExtensionData]
        public JsonDictionary Properties { get; set; } = new();

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public int Count => Properties.Count;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public dynamic this[string propertyName]
        {
            get => Properties[propertyName];
            set => Properties[propertyName] = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public bool ContainsKey(string propertyName)
        {
            return Properties.ContainsKey(propertyName);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        public bool TryGetValue<T>(string propertyName, out T propertyValue)
        {
            return Properties.TryGetValue<T>(propertyName, out propertyValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator() => Properties.GetEnumerator();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetPropertiesAsJson()
        {
            return JsonSerializer.Serialize(Properties, JsonSerializerSettings.Options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        public bool TryGetAndDeserializeValue<T>(string propertyName, out T propertyValue)
        {
            return Properties.TryGetAndDeserializeValue<T>(propertyName, out propertyValue);
        }
    }
}
