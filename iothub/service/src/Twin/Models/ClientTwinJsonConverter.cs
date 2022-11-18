// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Azure;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Converts a twin to JSON.
    /// </summary>
    internal sealed class ClientTwinJsonConverter : JsonConverter<ClientTwin>
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
        private const string ConnectionStateTag = "connectionState";
        private const string LastActivityTimeTag = "lastActivityTime";
        private const string CloudToDeviceMessageCountTag = "cloudToDeviceMessageCount";
        private const string AuthenticationTypeTag = "authenticationType";
        private const string X509ThumbprintTag = "x509Thumbprint";
        private const string ModelId = "modelId";
        private const string DeviceScope = "deviceScope";

        /// <inheritdoc/>
        public override ClientTwin? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            var twin = new ClientTwin();

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
                        twin.Capabilities = new ClientCapabilities
                        {
                            IsIotEdge = capabilitiesDictionary.ContainsKey(IotEdgeName) && (bool)capabilitiesDictionary[IotEdgeName]
                        };
                        break;

                    case ETagJsonTag:
                        twin.ETag = new ETag(reader.Value as string);
                        break;

                    case TagsJsonTag:
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            throw new InvalidOperationException("Tags Json not a Dictionary.");
                        }
                        twin.Tags = serializer.Deserialize<Dictionary<string, object>>(reader);
                        break;

                    case PropertiesJsonTag:
                        PopulatePropertiesForTwin(twin, reader);
                        break;

                    case VersionTag:
                        twin.Version = (long?)reader.Value;
                        break;

                    case StatusTag:
                        string status = reader.Value as string;
                        twin.Status = status?[0] == '\"' ? JsonSerializer.Deserialize<ClientStatus>(status) : serializer.Deserialize<ClientStatus>(reader);
                        break;

                    case StatusReasonTag:
                        twin.StatusReason = reader.Value as string;
                        break;

                    case StatusUpdateTimeTag:
                        twin.StatusUpdatedOnUtc = ConvertToDateTime(reader.Value);
                        break;

                    case ConnectionStateTag:
                        string connectionState = reader.Value as string;
                        twin.ConnectionState = connectionState?[0] == '\"'
                            ? JsonSerializer.Deserialize<ClientConnectionState>(connectionState)
                            : serializer.Deserialize<ClientConnectionState>(reader);
                        break;

                    case LastActivityTimeTag:
                        twin.LastActiveOnUtc = ConvertToDateTime(reader.Value);
                        break;

                    case CloudToDeviceMessageCountTag:
                        twin.CloudToDeviceMessageCount = serializer.Deserialize<int>(reader);
                        break;

                    case AuthenticationTypeTag:
                        string authenticationType = reader.Value as string;
                        twin.AuthenticationType = authenticationType?[0] == '\"'
                            ? JsonSerializer.Deserialize<ClientAuthenticationType>(authenticationType)
                            : serializer.Deserialize<ClientAuthenticationType>(reader);
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

        /// <inheritdoc/>

        public override void Write(Utf8JsonWriter writer, ClientTwin clientTwinValue, JsonSerializerOptions options)
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

            var twin = value as ClientTwin;

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
            writer.WriteValue(twin.ETag.ToString());

            writer.WritePropertyName(VersionTag);
            writer.WriteValue(twin.Version);

            if (twin.Status != null)
            {
                writer.WritePropertyName(StatusTag);
                writer.WriteRawValue(JsonSerializer.Serialize(twin.Status));
            }

            if (!string.IsNullOrWhiteSpace(twin.StatusReason))
            {
                writer.WritePropertyName(StatusReasonTag);
                writer.WriteValue(twin.StatusReason);
            }

            if (twin.StatusUpdatedOnUtc != null)
            {
                writer.WritePropertyName(StatusUpdateTimeTag);
                writer.WriteValue(twin.StatusUpdatedOnUtc);
            }

            if (twin.ConnectionState != null)
            {
                writer.WritePropertyName(ConnectionStateTag);
                writer.WriteRawValue(JsonSerializer.Serialize(twin.ConnectionState, new JsonStringEnumConverter()));
            }

            if (twin.LastActiveOnUtc != null)
            {
                writer.WritePropertyName(LastActivityTimeTag);
                writer.WriteValue(twin.LastActiveOnUtc);
            }

            if (twin.CloudToDeviceMessageCount != null)
            {
                writer.WritePropertyName(CloudToDeviceMessageCountTag);
                writer.WriteValue(twin.CloudToDeviceMessageCount);
            }

            if (twin.AuthenticationType != null)
            {
                writer.WritePropertyName(AuthenticationTypeTag);
                writer.WriteRawValue(JsonSerializer.Serialize(twin.AuthenticationType));
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
                    serializer.Serialize(writer, twin.Properties.Desired, typeof(ClientTwinProperties));
                }

                if (twin.Properties.Reported != null)
                {
                    writer.WritePropertyName(ReportedPropertiesJsonTag);
                    serializer.Serialize(writer, twin.Properties.Reported, typeof(ClientTwinProperties));
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
        /// Value indicating whether this TwinJsonConverter can write JSON
        /// </summary>
        public override bool CanConvert(Type objectType) => typeof(ClientTwin).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());

        private static Dictionary<string, object> GetTagsForTwin(ref Utf8JsonReader reader)
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

        private static DateTimeOffset? ConvertToDateTime(object obj)
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

        private static DateTimeOffset? ParseToDateTime(string value)
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dateTimeOffset)
                ? dateTimeOffset.UtcDateTime
                : null;
        }

        private static void PopulatePropertiesForTwin(ClientTwin twin, Utf8JsonReader reader)
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
                        twin.Properties.Desired = new ClientTwinProperties(JToken.ReadFrom(reader) as JObject);
                        break;

                    case ReportedPropertiesJsonTag:
                        twin.Properties.Reported = new ClientTwinProperties(JToken.ReadFrom(reader) as JObject);
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
