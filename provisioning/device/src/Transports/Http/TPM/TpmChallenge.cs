// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// TPM challenge.
    /// </summary>
    internal class TpmChallenge
    {
        /// <summary>
        /// Message.
        /// </summary>
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        /// <summary>
        /// Authentication key.
        /// </summary>
        [JsonProperty(PropertyName = "authenticationKey")]
        public string AuthenticationKey { get; set; }

        /// <summary>
        /// Key name.
        /// </summary>
        [JsonProperty(PropertyName = "keyName")]
        public string KeyName { get; set; } = "registration";
    }
}
