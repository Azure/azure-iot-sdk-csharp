// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// A class to initialize JsonSerializerSettings which can be applied to the project.
    /// </summary>
    internal static class JsonSerializerSettingsInitializer
    {
        /// <summary>
        /// A static instance of JsonSerializerSettings which sets DateParseHandling to None.
        /// </summary>
        /// <remarks>
        /// By default, serializing/deserializing with Newtonsoft.Json will try to parse date-formatted
        /// strings to a date type, which drops trailing zeros in the microseconds date portion. By
        /// specifying DateParseHandling with None, the original string will be read as-is. For more details
        /// about the known issue, see https://github.com/JamesNK/Newtonsoft.Json/issues/1511.
        /// </remarks>
        private static readonly JsonSerializerSettings s_settings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None
        };

        /// <summary>
        /// Settings that serialize preview-only properties (marked with <see cref="PreviewApiOnlyAttribute"/>).
        /// </summary>
        private static readonly JsonSerializerSettings s_previewSettings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            ContractResolver = new PreviewApiContractResolver(includePreview: true)
        };

        /// <summary>
        /// Settings that suppress serialization of preview-only properties for stable API versions.
        /// </summary>
        private static readonly JsonSerializerSettings s_stableSettings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            ContractResolver = new PreviewApiContractResolver(includePreview: false)
        };

        /// <summary>
        /// Returns the default JsonSerializerSettings. Used for deserialization, query specifications,
        /// attestation payloads and diagnostics, all of which are version agnostic.
        /// </summary>
        internal static JsonSerializerSettings GetJsonSerializerSettings()
        {
            return s_settings;
        }

        /// <summary>
        /// Returns JsonSerializerSettings that serialize the payload for the given service API version.
        /// </summary>
        /// <remarks>
        /// Preview-only properties are only serialized when a preview <see cref="ServiceVersion"/> is selected.
        /// </remarks>
        internal static JsonSerializerSettings GetJsonSerializerSettings(ServiceVersion serviceVersion)
        {
            return serviceVersion == ServiceVersion.V2026_11_02_Preview
                ? s_previewSettings
                : s_stableSettings;
        }

        /// <summary>
        /// A contract resolver that suppresses serialization of <see cref="PreviewApiOnlyAttribute"/> members
        /// when preview features are not enabled. Property names are otherwise resolved exactly as the default
        /// resolver, so explicit <c>[JsonProperty]</c> names are honored unchanged.
        /// </summary>
        private sealed class PreviewApiContractResolver : DefaultContractResolver
        {
            private readonly bool _includePreview;

            internal PreviewApiContractResolver(bool includePreview)
            {
                _includePreview = includePreview;
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                if (!_includePreview
                    && member.GetCustomAttribute<PreviewApiOnlyAttribute>() != null)
                {
                    property.ShouldSerialize = _ => false;
                }

                return property;
            }
        }
    }
}
