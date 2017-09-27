// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.ComponentModel;

    /// <summary>
    /// Used to specify the attestation mechanism used by a device during enrollment.
    /// </summary>
    public sealed class AttestationMechanism
    {
        private TpmAttestation tpm;
        private X509Attestation x509;

        /// <summary>
        /// Creates a new instance of <see cref="AttestationMechanism"/>
        /// </summary>
        public AttestationMechanism()
        {
        }

        /// <summary>
        ///  Attestation Type.
        /// </summary>
        [DefaultValue(AttestationMechanismType.None)]
        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AttestationMechanismType Type { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TpmAttestation"/> used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "tpm")]
        public TpmAttestation Tpm
        {
            get { return this.tpm; }
            set
            {
                this.tpm = value;
                if (value != null)
                {
                    this.Type = AttestationMechanismType.Tpm;
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="X509Attestation"/> used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "x509")]
        public X509Attestation X509
        {
            get { return this.x509; }
            set
            {
                this.x509 = value;
                if (value != null)
                {
                    this.Type = AttestationMechanismType.X509;
                }
            }
        }
    }
}
