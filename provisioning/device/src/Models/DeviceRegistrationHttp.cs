using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Device registration over HTTP.
    /// </summary>
    internal class DeviceRegistrationHttp : DeviceRegistration
    {
        /// <summary>
        /// Initializes a new instance of the DeviceRegistration class.
        /// </summary>
        public DeviceRegistrationHttp(JRaw payload = default, string registrationId = default, TpmAttestation tpm = default)
            : base(payload)
        {
            RegistrationId = registrationId;
            Tpm = tpm;
        }

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
