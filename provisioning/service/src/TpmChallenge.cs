// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// TPM challenge.
    /// </summary>
    public class TpmChallenge
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
