// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Registration result returned when using symmetric key attestation.
    /// </summary>
    public class SymmetricKeyRegistrationResult
    {
        /// <summary>
        /// For deserialization and unit testing.
        /// </summary>
        protected internal SymmetricKeyRegistrationResult()
        {
        }

        /// <summary>
        /// The Id of the enrollment group.
        /// </summary>
        [JsonPropertyName("enrollmentGroupId")]
        public string EnrollmentGroupId { get; internal set; }
    }
}