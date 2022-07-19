// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// TPM registration result.
    /// </summary>
    public class TpmRegistrationResult
    {
        /// <summary>
        /// For deserialization.
        /// </summary>
        internal TpmRegistrationResult()
        {
        }

        /// <summary>
        /// The encrypted authentication key.
        /// </summary>
        [JsonProperty(PropertyName = "authenticationKey")]
        public string AuthenticationKey { get; internal set; }
    }
}
