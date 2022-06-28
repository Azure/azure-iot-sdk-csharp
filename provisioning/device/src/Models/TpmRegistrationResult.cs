// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// TPM registration result.
    /// </summary>
    internal class TpmRegistrationResult
    {
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public TpmRegistrationResult(string authenticationKey = default)
        {
            AuthenticationKey = authenticationKey;
        }

        /// <summary>
        /// The TPM authentication key.
        /// </summary>
        [JsonProperty(PropertyName = "authenticationKey")]
        public string AuthenticationKey { get; }
    }
}
