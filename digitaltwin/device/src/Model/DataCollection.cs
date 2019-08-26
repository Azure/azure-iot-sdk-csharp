// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.IO;

using Azure.Iot.DigitalTwin.Device.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    /// <summary>
    /// A collection of key value data pairs. This collection doesn't have meta data for properties, only actual data values.
    /// </summary>
    [JsonConverter(typeof(DataCollectionJsonConverter))]
    internal class DataCollection : IEnumerable<KeyValuePair<string, object>>
    {
        private JObject properties;

        /// <summary>
        /// Creates an instance of <see cref="DataCollection"/>.
        /// </summary>
        public DataCollection()
            : this(new JObject())
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DataCollection"/> with given json properties.
        /// </summary>
        /// <param name="propertiesJson"></param>
        public DataCollection(string propertiesJson)
            : this(ParseStringIntoJObject(propertiesJson))
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(propertiesJson, nameof(propertiesJson));
        }

        private DataCollection(JObject properties)
        {
            this.properties = properties;
        }

        /// <summary>
        /// Set value of an property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object this[string propertyName]
        {
            get
            {
                JToken jtoken;
                if (this.properties.TryGetValue(propertyName, out jtoken))
                {
                    return jtoken;
                }

                return string.Empty;
            }

            set
            {
                JToken valueJToken = value == null ? null : JToken.FromObject(value, new JsonSerializer() { DateParseHandling = DateParseHandling.None });
                JToken ignored;
                if (this.properties.TryGetValue(propertyName, out ignored))
                {
                    this.properties[propertyName] = valueJToken;
                }
                else
                {
                    this.properties.Add(propertyName, valueJToken);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="DataCollection"/> as a JSON string
        /// </summary>
        /// <param name="formatting">Optional. Formatting for the output JSON string.</param>
        /// <returns>JSON string</returns>
        public string ToJson(Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(this.properties, formatting);
        }

        /// <summary>
        /// Get enumerator list of key value pairs.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (KeyValuePair<string, JToken> kvp in this.properties)
            {
                yield return new KeyValuePair<string, dynamic>(kvp.Key, this[kvp.Key]);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Convert the properties to a json string.
        /// </summary>
        /// <returns>a json string of the properties.</returns>
        public override string ToString()
        {
            return this.properties.ToString();
        }

        internal JObject JObject => this.properties;

        /// <summary>
        /// This is needed because JObject.Parse doesn't support DateParseHandling settings,
        /// The code bellow is effectively the same as JObject but with the DateParseHandling option.
        /// </summary>
        /// <param name="jsonString">The json string.</param>
        /// <returns></returns>
        private static JObject ParseStringIntoJObject(string jsonString)
        {
            using (JsonReader reader = (JsonReader)new JsonTextReader((TextReader)new StringReader(jsonString)))
            {
                reader.DateParseHandling = DateParseHandling.None;
                JObject jsonObject = JObject.Load(reader, (JsonLoadSettings)null);
                return jsonObject;
            }
        }
    }
}
