// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary>
    /// Device registration.
    /// </summary>
    internal partial class DeviceRegistration
    {
        /// <summary>
        /// Initializes a new instance of the DeviceRegistration class.
        /// </summary>
        public DeviceRegistration(string registrationId = default(string), TpmAttestation tpm = default(TpmAttestation))
        {
            RegistrationId = registrationId;
            Tpm = tpm;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "registrationId")]
        public string RegistrationId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "tpm")]
        public TpmAttestation Tpm { get; set; }
    }
}
