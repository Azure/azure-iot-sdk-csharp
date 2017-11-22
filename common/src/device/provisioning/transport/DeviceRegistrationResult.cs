// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
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
        /// <summary>
        /// Initializes a new instance of the DeviceRegistrationResult class.
        /// </summary>
        public DeviceRegistrationResult()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the DeviceRegistrationResult class.
        /// </summary>
        /// <param name="status">Possible values include: 'unassigned',
        /// 'assigning', 'assigned', 'failed', 'disabled'</param>
        public DeviceRegistrationResult(
            TpmRegistrationResult tpm = default(TpmRegistrationResult), 
            X509RegistrationResult x509 = default(X509RegistrationResult), 
            string registrationId = default(string), 
            DateTime? createdDateTimeUtc = default(DateTime?), 
            string assignedHub = default(string), 
            string deviceId = default(string), 
            string status = default(string), 
            string generationId = default(string), 
            DateTime? lastUpdatedDateTimeUtc = default(DateTime?), 
            int? errorCode = default(int?), 
            string errorMessage = default(string), 
            string etag = default(string))
        {
            Tpm = tpm;
            X509 = x509;
            RegistrationId = registrationId;
            CreatedDateTimeUtc = createdDateTimeUtc;
            AssignedHub = assignedHub;
            DeviceId = deviceId;
            Status = status;
            GenerationId = generationId;
            LastUpdatedDateTimeUtc = lastUpdatedDateTimeUtc;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Etag = etag;
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
        /// Gets or sets possible values include: 'unassigned', 'assigning',
        /// 'assigned', 'failed', 'disabled'
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

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

    }
}
