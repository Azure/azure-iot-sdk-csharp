// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Used to specify the authentication type used by a device.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AuthenticationType
    {
        /// <summary>
        /// Shared Access Key
        /// </summary>
        [EnumMember(Value = "sas")] Sas = 0,

        /// <summary>
        /// Self-signed certificate
        /// </summary>
        [EnumMember(Value = "selfSigned")] SelfSigned = 1,

        /// <summary>
        /// Certificate Authority
        /// </summary>
        [EnumMember(Value = "certificateAuthority")] CertificateAuthority = 2,

        /// <summary>
        /// No Authentication Token at this scope
        /// </summary>
        [EnumMember(Value = "none")] None = 3
    }
}
