// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Device registration.
    /// </summary>
    internal class DeviceRegistration
    {
        /// <summary>
        /// Initializes a new instance of the DeviceRegistration class.
        /// </summary>
        public DeviceRegistration(JRaw payload = default, string registrationId = default, TpmAttestation tpm = default)
        {
            Payload = payload;
            RegistrationId = registrationId;
            Tpm = tpm;
        }

        /// <summary>
        /// Gets or set the custom content payload.
        /// </summary>
        [JsonProperty(PropertyName = "payload")]
        public JRaw Payload { get; set; }

        /// <summary>
        /// The device registration Id.
        /// </summary>
        [JsonProperty(PropertyName = "registrationId")]
        public string RegistrationId { get; set; }

        /// <summary>
        /// Attestation via TPM, if any.
        /// </summary>
        [JsonProperty(PropertyName = "tpm")]
        public TpmAttestation Tpm { get; set; }
    }
}
