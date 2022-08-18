// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Converts a <see cref="Twin"/> to Json.
    /// </summary>
    public sealed class TwinJsonConverter : JsonConverter
    {
        private const string DeviceIdJsonTag = "deviceId";
        private const string ModuleIdJsonTag = "moduleId";
        private const string ConfigurationsJsonTag = "configurations";
        private const string CapabilitiesJsonTag = "capabilities";
        private const string IotEdgeName = "iotEdge";
        private const string ETagJsonTag = "etag";
        private const string TagsJsonTag = "tags";
        private const string PropertiesJsonTag = "properties";
        private const string DesiredPropertiesJsonTag = "desired";
        private const string ReportedPropertiesJsonTag = "reported";
        private const string VersionTag = "version";
        private const string StatusTag = "status";
        private const string StatusReasonTag = "statusReason";
        private const string StatusUpdateTimeTag = "statusUpdateTime";
        private const string ConnectionStatusTag = "connectionStatus";
        private const string LastActivityTimeTag = "lastActivityTime";
        private const string CloudToDeviceMessageCountTag = "cloudToDeviceMessageCount";
        private const string AuthenticationTypeTag = "authenticationType";
        private const string X509ThumbprintTag = "x509Thumbprint";
        private const string ModelId = "modelId";
        private const string DeviceScope = "deviceScope";
        private const string ParentScopes = "parentScopes";

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

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            var twin = value as Twin;

            if (twin == null)
            {
                throw new InvalidOperationException("Object passed is not of type Twin.");
            }

            writer.WriteStartObject();

            if (!string.IsNullOrWhiteSpace(twin.ModelId))
            {
                writer.WritePropertyName(ModelId);
                writer.WriteValue(twin.ModelId);
            }

            writer.WritePropertyName(DeviceIdJsonTag);
            writer.WriteValue(twin.DeviceId);

            if (!string.IsNullOrWhiteSpace(twin.ModuleId))
            {
                writer.WritePropertyName(ModuleIdJsonTag);
                writer.WriteValue(twin.ModuleId);
            }

            writer.WritePropertyName(ETagJsonTag);
            writer.WriteValue(twin.ETag);

            writer.WritePropertyName(VersionTag);
            writer.WriteValue(twin.Version);

            if (twin.Status != null)
            {
                writer.WritePropertyName(StatusTag);
                writer.WriteRawValue(JsonConvert.SerializeObject(twin.Status));
            }

            if (!string.IsNullOrWhiteSpace(twin.StatusReason))
            {
                writer.WritePropertyName(StatusReasonTag);
                writer.WriteValue(twin.StatusReason);
            }

            if (twin.StatusUpdatedTime != null)
            {
                writer.WritePropertyName(StatusUpdateTimeTag);
                writer.WriteValue(twin.StatusUpdatedTime);
            }

            if (twin.ConnectionStatus != null)
            {
                writer.WritePropertyName(ConnectionStatusTag);
                writer.WriteRawValue(JsonConvert.SerializeObject(twin.ConnectionStatus, new StringEnumConverter()));
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

            if (twin.Configurations != null)
            {
                writer.WritePropertyName(ConfigurationsJsonTag);
                serializer.Serialize(writer, twin.Configurations, typeof(IDictionary<string, ConfigurationInfo>));
            }

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

            if (twin.DeviceScope != null)
            {
                writer.WritePropertyName(DeviceScope);
                serializer.Serialize(writer, twin.DeviceScope, typeof(string));
            }

            if (twin.ParentScopes != null && twin.ParentScopes.Any())
            {
                writer.WritePropertyName(ParentScopes);
                serializer.Serialize(writer, twin.ParentScopes, typeof(IList<string>));
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
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

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

                    case ModelId:
                        twin.ModelId = reader.Value as string;
                        break;

                    case ModuleIdJsonTag:
                        twin.ModuleId = reader.Value as string;
                        break;

                    case ConfigurationsJsonTag:
                        twin.Configurations = serializer.Deserialize<Dictionary<string, ConfigurationInfo>>(reader);
                        break;

                    case CapabilitiesJsonTag:
                        Dictionary<string, object> capabilitiesDictionary = serializer.Deserialize<Dictionary<string, object>>(reader);
                        twin.Capabilities = new DeviceCapabilities
                        {
                            IotEdge = capabilitiesDictionary.ContainsKey(IotEdgeName) && (bool)capabilitiesDictionary[IotEdgeName]
                        };
                        break;

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
                        PopulatePropertiesForTwin(twin, reader);
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
                        twin.StatusUpdatedTime = ConvertToDateTime(reader.Value);
                        break;

                    case ConnectionStatusTag:
                        string connectionStatus = reader.Value as string;
                        twin.ConnectionStatus = connectionStatus?[0] == '\"'
                            ? JsonConvert.DeserializeObject<DeviceConnectionStatus>(connectionStatus)
                            : serializer.Deserialize<DeviceConnectionStatus>(reader);
                        break;

                    case LastActivityTimeTag:
                        twin.LastActivityTime = ConvertToDateTime(reader.Value);
                        break;

                    case CloudToDeviceMessageCountTag:
                        twin.CloudToDeviceMessageCount = serializer.Deserialize<int>(reader);
                        break;

                    case AuthenticationTypeTag:
                        string authenticationType = reader.Value as string;
                        twin.AuthenticationType = authenticationType?[0] == '\"'
                            ? JsonConvert.DeserializeObject<AuthenticationType>(authenticationType)
                            : serializer.Deserialize<AuthenticationType>(reader);
                        break;

                    case X509ThumbprintTag:
                        twin.X509Thumbprint = serializer.Deserialize<X509Thumbprint>(reader);
                        break;

                    case DeviceScope:
                        twin.DeviceScope = serializer.Deserialize<string>(reader);
                        break;

                    case ParentScopes:
                        twin.ParentScopes = serializer.Deserialize<IReadOnlyList<string>>(reader);
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

            var dict = new Dictionary<string, object>();
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

        private static DateTime? ConvertToDateTime(object obj)
        {
            if (obj is DateTime time)
            {
                return time.ToUniversalTime();
            }

            if (obj is DateTimeOffset offset)
            {
                return offset.UtcDateTime;
            }

            return ParseToDateTime(obj as string);
        }

        private static DateTime? ParseToDateTime(string value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime)
                ? dateTime.ToUniversalTime()
                : (DateTime?)null;
        }

        private static void PopulatePropertiesForTwin(Twin twin, JsonReader reader)
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
