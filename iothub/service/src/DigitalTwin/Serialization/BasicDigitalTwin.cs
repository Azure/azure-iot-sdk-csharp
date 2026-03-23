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
        /// The number of custom properties
        /// </summary>
        [JsonIgnore]
        public int Count => CustomProperties.Count;

        /// <summary>
        /// The setter/getter override for setting/getting from the custom properties.
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
        /// Returns true if the custom properties contains the provided key.
        /// </summary>
        /// <param name="propertyName">The provided key</param>
        /// <returns>True if the custom properties contains the provided key. False otherwise.</returns>
        public bool ContainsKey(string propertyName)
        {
            return CustomProperties.ContainsKey(propertyName);
        }

        /// <summary>
        /// Try to retrieve and cast a given property.
        /// </summary>
        /// <typeparam name="T">The type to try to cast the property value to.</typeparam>
        /// <param name="propertyName">The name of the property to try and retrieve and cast.</param>
        /// <param name="propertyValue">The property value if a property with the provided name exists and the value is the provided type</param>
        /// <returns>True if the property is present and its value can be cast as the provided type.</returns>
        public bool TryGetValue<T>(string propertyName, out T propertyValue)
        {
            return CustomProperties.TryGetValue<T>(propertyName, out propertyValue);
        }

        /// <summary>
        /// Get the enumerator for the custom properties.
        /// </summary>
        /// <returns>The enumerator for the custom properties</returns>
        public IEnumerator GetEnumerator() => CustomProperties.GetEnumerator();

        /// <summary>
        /// Get the Json string representation of these custom properties.
        /// </summary>
        /// <returns>The Json string representation of these custom properties.</returns>
        public string GetPropertiesAsJson()
        {
            return JsonSerializer.Serialize(CustomProperties, JsonSerializerSettings.Options);
        }

        /// <summary>
        /// Try to retrieve and deserialize a given property.
        /// </summary>
        /// <typeparam name="T">The type to try to deserialize the property value to, if it is present.</typeparam>
        /// <param name="propertyName">The name of the property to check.</param>
        /// <param name="propertyValue">The value of the property if it is present and can be deserialized into the provided type.</param>
        /// <returns>True if the given property is present and its value can be deserialized into the given type.</returns>
        /// <remarks>
        /// This method uses System.Text.Json for deserializing the values.
        /// </remarks>
        public bool TryGetAndDeserializeValue<T>(string propertyName, out T propertyValue)
        {
            return CustomProperties.TryGetAndDeserializeValue<T>(propertyName, out propertyValue);
        }
    }
}
