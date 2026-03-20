// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// An optional, helper class for deserializing a digital twin.
    /// </summary>
    public class BasicDigitalTwin
    {
        /// <summary>
        /// The unique Id of the digital twin.
        /// </summary>
        /// <remarks>
        /// This is present at the root of every digital twin.
        /// </remarks>
        [JsonPropertyName("$dtId")]
        public string Id { get; set; }

        /// <summary>
        /// Information about the model a digital twin conforms to.
        /// </summary>
        /// <remarks>
        /// This field is present on every digital twin.
        /// </remarks>
        [JsonPropertyName("$metadata")]
        public DigitalTwinMetadata Metadata { get; set; }

        /// <summary>
        /// Additional properties of the digital twin.
        /// </summary>
        /// <remarks>
        /// This field will contain any properties of the digital twin that are not already defined by the other strong types of this class.
        /// </remarks>
        [JsonExtensionData]
        public JsonDictionary CustomProperties { get; set; } = new();

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public int Count => CustomProperties.Count;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public dynamic this[string propertyName]
        {
            get => CustomProperties[propertyName];
            set => CustomProperties[propertyName] = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public bool ContainsKey(string propertyName)
        {
            return CustomProperties.ContainsKey(propertyName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        public bool TryGetValue<T>(string propertyName, out T propertyValue)
        {
            return CustomProperties.TryGetValue<T>(propertyName, out propertyValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator() => CustomProperties.GetEnumerator();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetPropertiesAsJson()
        {
            return JsonSerializer.Serialize(CustomProperties, JsonSerializerSettings.Options);
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
            return CustomProperties.TryGetAndDeserializeValue<T>(propertyName, out propertyValue);
        }
    }
}
