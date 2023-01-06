// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// A class to initialize JsonSerializerSettings with the default MaxDepth of 128 for the project
    /// </summary>
    public static class JsonSerializerSettingsInitializer
    {
        /// <summary>
        /// A static instance of JsonSerializerSettings which sets MaxDepth to 128.
        /// </summary>
        public static readonly JsonSerializerSettings SettingsInstance = new JsonSerializerSettings
        {
            MaxDepth = 128
        };

        /// <summary>
        /// Returns DefaultJsonSerializerSettings Func delegate
        /// </summary>
        public static Func<JsonSerializerSettings> GetDefaultJsonSerializerSettingsDelegate()
        {
            return () => SettingsInstance;
        }

        /// <summary>
        /// For testing only, returns DefaultJsonSerializerSettings
        /// </summary>
        public static JsonSerializerSettings GetDefaultJsonSerializerSettings()
        {
            return JsonConvert.DefaultSettings();
        }
    }
}
