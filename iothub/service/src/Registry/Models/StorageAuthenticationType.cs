// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies authentication type being used for connecting to storage account.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StorageAuthenticationType
    {
        /// <summary>
        /// Use a shared access key for authentication.
        /// </summary>
        /// <remarks>This means authentication must be supplied in the storage URI(s).</remarks>
        [EnumMember(Value = "keyBased")]
        KeyBased = 0,

        /// <summary>
        /// Use the AD identity configured on the hub for authentication to storage.
        /// </summary>
        [EnumMember(Value = "identityBased")]
        IdentityBased = 1,
    }
}
