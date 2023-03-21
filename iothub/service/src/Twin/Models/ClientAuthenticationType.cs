// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Used to specify the authentication type used by a device.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ClientAuthenticationType
    {
        /// <summary>
        /// No authentication token at this scope.
        /// </summary>
        [EnumMember(Value = "none")]
        None,

        /// <summary>
        /// Shared access key.
        /// </summary>
        [EnumMember(Value = "sas")]
        Sas,

        /// <summary>
        /// Self-signed certificate.
        /// </summary>
        [EnumMember(Value = "selfSigned")]
        SelfSigned,

        /// <summary>
        /// Certificate authority.
        /// </summary>
        [EnumMember(Value = "certificateAuthority")]
        CertificateAuthority,
    }
}
