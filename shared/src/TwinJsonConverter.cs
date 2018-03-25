// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.Azure.Devices.Common;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Converts <see cref="Twin"/> to Json
    /// </summary>
    public sealed class TwinJsonConverter : JsonConverter
    {
        const string DeviceIdJsonTag = "deviceId";
#if ENABLE_MODULES_SDK
        const string ModuleIdJsonTag = "moduleId";
        const string ConfigurationsJsonTag = "configurations";
        const string CapabilitiesJsonTag = "capabilities";
        const string IotEdgeName = "iotEdge";
#endif
        const string ETagJsonTag = "etag";
        const string TagsJsonTag = "tags";
        const string PropertiesJsonTag = "properties";
        const string DesiredPropertiesJsonTag = "desired";
        const string ReportedPropertiesJsonTag = "reported";
        const string VersionTag = "version";
        const string StatusTag = "status";
        const string StatusReasonTag = "statusReason";
        const string StatusUpdateTimeTag = "statusUpdateTime";
        const string ConnectionStateTag = "connectionState";
        const string LastActivityTimeTag = "lastActivityTime";
        const string CloudToDeviceMessageCountTag = "cloudToDeviceMessageCount";
        const string AuthenticationTypeTag = "authenticationType";
        const string X509ThumbprintTag = "x509Thumbprint";

        /// <summary>
        /// Converts <see cref="Twin"/> to its equivalent Json representation.
        /// </summary>
        /// <param name="writer">the Json writer.</param>
        /// <param name="value">the <see cref="Twin"/> to convert.</param>
        /// <param name="serializer">the Json serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                return;
            }

            Twin twin = value as Twin;

            if (twin == null)
            {
                throw new InvalidOperationException("Object passed is not of type Twin.");
            }

            writer.WriteStartObject();

            writer.WritePropertyName(DeviceIdJsonTag);
            writer.WriteValue(twin.DeviceId);

#if ENABLE_MODULES_SDK
            if (!string.IsNullOrEmpty(twin.ModuleId))
            {
                writer.WritePropertyName(ModuleIdJsonTag);
                writer.WriteValue(twin.ModuleId);
            }
#endif

            writer.WritePropertyName(ETagJsonTag);
            writer.WriteValue(twin.ETag);

            writer.WritePropertyName(VersionTag);
            writer.WriteValue(twin.Version);

            if (twin.Status != null)
            {
                writer.WritePropertyName(StatusTag);
                writer.WriteRawValue(JsonConvert.SerializeObject(twin.Status));
            }
            
            if (!string.IsNullOrEmpty(twin.StatusReason))
            {
                writer.WritePropertyName(StatusReasonTag);
                writer.WriteValue(twin.StatusReason);
            }

            if (twin.StatusUpdatedTime != null)
            {
                writer.WritePropertyName(StatusUpdateTimeTag);
                writer.WriteValue(twin.StatusUpdatedTime);
            }

            if (twin.ConnectionState != null)
            {
                writer.WritePropertyName(ConnectionStateTag);
                writer.WriteRawValue(JsonConvert.SerializeObject(twin.ConnectionState, new StringEnumConverter()));
            }

            if (twin.LastActivityTime != null)
            {
                writer.WritePropertyName(LastActivityTimeTag);
                writer.WriteValue(twin.LastActivityTime);
            }

            if (twin.CloudToDeviceMessageCount != null)
            {
                writer.WritePropertyName(CloudToDeviceMessageCountTag);
                writer.WriteValue(twin.CloudToDeviceMessageCount);
            }

            if (twin.AuthenticationType != null)
            {
                writer.WritePropertyName(AuthenticationTypeTag);
                writer.WriteRawValue(JsonConvert.SerializeObject(twin.AuthenticationType));
            }

            if (twin.X509Thumbprint != null)
            {
                writer.WritePropertyName(X509ThumbprintTag);
                serializer.Serialize(writer, twin.X509Thumbprint);
            }
 
#if ENABLE_MODULES_SDK
            if (twin.Configurations != null)
            {
                writer.WritePropertyName(ConfigurationsJsonTag);
                serializer.Serialize(writer, twin.Configurations, typeof(IDictionary<string, ConfigurationInfo>));
            }
#endif

            if (twin.Tags != null && twin.Tags.Count > 0)
            {
                writer.WritePropertyName(TagsJsonTag);
                serializer.Serialize(writer, twin.Tags, typeof(IDictionary<string, object>));
            }

            if (twin.Properties?.Desired != null || twin.Properties?.Reported != null)
            {
                writer.WritePropertyName(PropertiesJsonTag);
                writer.WriteStartObject();
                if (twin.Properties.Desired != null)
                {
                    writer.WritePropertyName(DesiredPropertiesJsonTag);
                    serializer.Serialize(writer, twin.Properties.Desired, typeof(TwinCollection));
                }

                if (twin.Properties.Reported != null)
                {
                    writer.WritePropertyName(ReportedPropertiesJsonTag);
                    serializer.Serialize(writer, twin.Properties.Reported, typeof(TwinCollection));
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Converts Json to its equivalent <see cref="Twin"/> representation.
        /// </summary>
        /// <param name="reader">the Json reader.</param>
        /// <param name="objectType">object type</param>
        /// <param name="existingValue">exisiting value</param>
        /// <param name="serializer">the Json serializer.</param>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var twin = new Twin();

            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonToken.PropertyName)
                {
                    // TODO: validate that this code is not reached.
                    continue;
                }

                string propertyName = reader.Value as string;
                reader.Read();

                switch (propertyName)
                {
                    case DeviceIdJsonTag:
                        twin.DeviceId = reader.Value as string;
                        break;
#if ENABLE_MODULES_SDK
                    case ModuleIdJsonTag:
                        twin.ModuleId = reader.Value as string;
                        break;
                    case ConfigurationsJsonTag:
                        twin.Configurations = serializer.Deserialize<Dictionary<string, ConfigurationInfo>>(reader);
                        break;
                    case CapabilitiesJsonTag:
                        var capabilitiesDictionary = serializer.Deserialize<Dictionary<string, object>>(reader);
                        twin.Capabilities = new DeviceCapabilities
                        {
                            IotEdge = capabilitiesDictionary.ContainsKey(IotEdgeName) && (bool)capabilitiesDictionary[IotEdgeName]
                        };
                        break;
#endif
                    case ETagJsonTag:
                        twin.ETag = reader.Value as string;
                        break;
                    case TagsJsonTag:
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            throw new InvalidOperationException("Tags Json not a Dictionary.");
                        }
                        twin.Tags = new TwinCollection(JToken.ReadFrom(reader) as JObject);
                        break;
                    case PropertiesJsonTag:
                        PopulatePropertiesForTwin(twin, reader, serializer);
                        break;
                    case VersionTag:
                        twin.Version = (long?)reader.Value;
                        break;
                    case StatusTag:
                        string status = reader.Value as string;
                        twin.Status = status?[0] == '\"' ? JsonConvert.DeserializeObject<DeviceStatus>(status) : serializer.Deserialize<DeviceStatus>(reader);
                        break;
                    case StatusReasonTag:
                        twin.StatusReason = reader.Value as string;
                        break;
                    case StatusUpdateTimeTag:
                        twin.StatusUpdatedTime = (DateTime)reader.Value;
                        break;
                    case ConnectionStateTag:
                        string connectionState = reader.Value as string;
                        twin.ConnectionState = connectionState?[0] == '\"' ? JsonConvert.DeserializeObject<DeviceConnectionState>(connectionState) : serializer.Deserialize<DeviceConnectionState>(reader);
                        break;
                    case LastActivityTimeTag:
                        twin.LastActivityTime = (DateTime)reader.Value;
                        break;
                    case CloudToDeviceMessageCountTag:
                        twin.CloudToDeviceMessageCount = serializer.Deserialize<int>(reader);
                        break;
                    case AuthenticationTypeTag:
                        string authenticationType = reader.Value as string;
                        twin.AuthenticationType = authenticationType?[0] == '\"' ? JsonConvert.DeserializeObject<AuthenticationType>(authenticationType) : serializer.Deserialize<AuthenticationType>(reader);
                        break;
                    case X509ThumbprintTag:
                        twin.X509Thumbprint = serializer.Deserialize<X509Thumbprint>(reader);
                        break;
                    default:
                        // Ignore unknown fields
                        reader.Skip();
                        break;
                }
            }

            return twin;
        }

        /// <summary>
        /// Converter Can Read flag
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Value indicating whether this TwinJsonConverter can read JSON
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// Value indicating whether this TwinJsonConverter can write JSON
        /// </summary>
        public override bool CanConvert(Type objectType) => typeof(Twin).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());


        private static Dictionary<string, object> GetTagsForTwin(JsonReader reader)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            Dictionary<string, object> dict = new Dictionary<string, object>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }

                string propertyName = reader.Value as string;
                reader.Read();

                if (reader.TokenType != JsonToken.StartObject)
                {
                    dict.Add(propertyName, reader.Value);
                }
                else
                {
                    dict.Add(propertyName, GetTagsForTwin(reader));
                }
            }

            return dict;
        }

        private static void PopulatePropertiesForTwin(Twin twin, JsonReader reader, JsonSerializer serializer)
        {
            if (twin == null)
            {
                throw new InvalidOperationException("Twin object null.");
            }

            if (reader.TokenType != JsonToken.StartObject)
            {
                twin.Properties.Desired = null;
                twin.Properties.Reported = null;
                return;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }

                string propertyName = reader.Value as string;
                reader.Read();

                switch (propertyName)
                {
                    case DesiredPropertiesJsonTag:
                        twin.Properties.Desired = new TwinCollection(JToken.ReadFrom(reader) as JObject);
                        break;
                    case ReportedPropertiesJsonTag:
                        twin.Properties.Reported = new TwinCollection(JToken.ReadFrom(reader) as JObject);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
