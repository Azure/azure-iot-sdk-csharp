// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Registration result returned when using symmetric key attestation.
    /// </summary>
    public class SymmetricKeyRegistrationResult
    {
        /// <summary>
        /// For deserialization.
        /// </summary>
        protected internal SymmetricKeyRegistrationResult()
        {
        }

        /// <summary>
        /// The Id of the enrollment group.
        /// </summary>
        [JsonProperty("enrollmentGroupId")]
        public string EnrollmentGroupId { get; protected internal set; }
    }
}