// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
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
        /// strings to a date type, which drops trailing zeros in the microseconds portion. By
        /// specifying DateParseHandling with None, the original string will be read as-is. For more details
        /// about the known issue, see https://github.com/JamesNK/Newtonsoft.Json/issues/1511.
        /// </remarks>
        private static readonly JsonSerializerSettings s_settings = new()
        {
            DateParseHandling = DateParseHandling.None
        };

        /// <summary>
        /// Returns JsonSerializerSettings Func delegate
        /// </summary>
        internal static Func<JsonSerializerSettings> GetJsonSerializerSettingsDelegate()
        {
            return () => s_settings;
        }
    }
}
