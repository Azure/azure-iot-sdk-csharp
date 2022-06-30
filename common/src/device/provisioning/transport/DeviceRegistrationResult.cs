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
        public DeviceRegistrationResult()
        {
            CustomInit();
        }

        internal DeviceRegistrationResult(
            TpmRegistrationResult tpm = default,
            X509RegistrationResult x509 = default,
            SymmetricKeyRegistrationResult symmetricKey = default,
            string registrationId = default,
            DateTime? createdDateTimeUtc = default,
            string assignedHub = default,
            string deviceId = default,
            string status = default,
            string substatus = default,
            string generationId = default,
            DateTime? lastUpdatedDateTimeUtc = default,
            int? errorCode = default,
            string errorMessage = default,
            string etag = default,
            JRaw payload = default)
        {
            Tpm = tpm;
            X509 = x509;
            SymmetricKey = symmetricKey;
            RegistrationId = registrationId;
            CreatedDateTimeUtc = createdDateTimeUtc;
            AssignedHub = assignedHub;
            DeviceId = deviceId;
            Status = status;
            Substatus = substatus;
            GenerationId = generationId;
            LastUpdatedDateTimeUtc = lastUpdatedDateTimeUtc;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Etag = etag;
            Payload = payload;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "tpm")]
        public TpmRegistrationResult Tpm { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "x509")]
        public X509RegistrationResult X509 { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "symmetricKey")]
        public SymmetricKeyRegistrationResult SymmetricKey { get; set; }


        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "registrationId")]
        public string RegistrationId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "createdDateTimeUtc")]
        public DateTime? CreatedDateTimeUtc { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "assignedHub")]
        public string AssignedHub { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets possible values include:
        /// 'unassigned', 'assigning', 'assigned', 'failed', 'disabled'
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Possible values include: 'initialAssignment', 'deviceDataMigrated', 'deviceDataReset'
        /// </summary>
        [JsonProperty(PropertyName = "substatus")]
        public string Substatus { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "generationId")]
        public string GenerationId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdatedDateTimeUtc")]
        public DateTime? LastUpdatedDateTimeUtc { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "errorCode")]
        public int? ErrorCode { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "errorMessage")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "etag")]
        public string Etag { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "payload")]
        public JRaw Payload { get; set; }
    }
}
