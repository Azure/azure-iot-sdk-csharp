// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary>
    /// Device registration result.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Used by the JSon parser.")]
    internal partial class DeviceRegistrationResult
    {
        [JsonProperty(PropertyName = "tpm")]
        public TpmRegistrationResult Tpm { get; set; }

        [JsonProperty(PropertyName = "x509")]
        public X509RegistrationResult X509 { get; set; }

        [JsonProperty(PropertyName = "symmetricKey")]
        public SymmetricKeyRegistrationResult SymmetricKey { get; set; }

        [JsonProperty(PropertyName = "registrationId")]
        public string RegistrationId { get; set; }

        [JsonProperty(PropertyName = "createdDateTimeUtc")]
        public DateTime? CreatedDateTimeUtc { get; set; }

        [JsonProperty(PropertyName = "assignedHub")]
        public string AssignedHub { get; set; }

        [JsonProperty(PropertyName = "deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// The status of the operation.
        /// Possible values include: 'unassigned', 'assigning', 'assigned', 'failed', 'disabled'
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// The substatus of the operation.
        /// Possible values include: 'initialAssignment', 'deviceDataMigrated', 'deviceDataReset'
        /// </summary>
        [JsonProperty(PropertyName = "substatus")]
        public string Substatus { get; set; }

        [JsonProperty(PropertyName = "generationId")]
        public string GenerationId { get; set; }

        [JsonProperty(PropertyName = "lastUpdatedDateTimeUtc")]
        public DateTime? LastUpdatedDateTimeUtc { get; set; }

        [JsonProperty(PropertyName = "errorCode")]
        public int? ErrorCode { get; set; }

        [JsonProperty(PropertyName = "errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty(PropertyName = "etag")]
        public string Etag { get; set; }

        [JsonProperty(PropertyName = "payload")]
        public JRaw Payload { get; set; }

        /// <summary>
        /// The client certificate that was signed by the certificate authority.
        /// This client certificate was used by the device provisioning service to register the enrollment with IoT hub.
        /// The IoT device can then use this returned client certificate along with the private key information to authenticate with IoT hub.
        /// </summary>
        [JsonProperty(PropertyName = "issuedClientCertificate")]
        public string IssuedClientCertificate { get; set; }

        /// <summary>
        /// The trust bundle result returned after a successful device registation.
        /// This trust bundle is a collection of trusted root or intermediate certificates that have been uploaded to the provisioning service.
        /// </summary>
        [JsonProperty(PropertyName = "trustBundle")]
        public TrustBundle TrustBundle { get; set; }
    }
}
