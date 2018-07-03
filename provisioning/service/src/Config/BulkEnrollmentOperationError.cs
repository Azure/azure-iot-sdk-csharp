// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service device registration operation error.
    /// </summary>
    /// <remarks>
    /// This error is returned as a result of the 
    ///     <see cref="ProvisioningServiceClient.RunBulkEnrollmentOperationAsync(BulkOperationMode, System.Collections.Generic.IEnumerable{IndividualEnrollment})"/>
    ///     as part of the <see cref="BulkEnrollmentOperationResult"/>.
    /// </remarks>
    /// <example>
    /// The following JSON is an example of a single error operation from a Bulk operation
    /// <code>
    /// {
    ///      "registrationId":"validRegistrationId1",
    ///      "errorCode":200,
    ///      "errorStatus":"Succeeded"
    /// }
    /// </code>
    /// </example>
    public class BulkEnrollmentOperationError
    {
        /* SRS_BULK_ENROLLMENT_OPERATION_ERRO_21_002: [The BulkEnrollmentOperationError shall store the provided information.] */

        /// <summary>
        /// Registration ID.
        /// </summary>
        /// <remarks>
        /// A valid registration Id shall be alphanumeric, lowercase, and may contain hyphens. Max characters 128.
        /// </remarks>
        /// <exception cref="ArgumentException">if the provided string does not fit the registration Id requirements</exception>
        [JsonProperty(PropertyName = "registrationId", Required = Required.Always)]
        public string RegistrationId
        {
            get
            {
                return _registrationId;
            }

            private set
            {
                /* SRS_BULK_ENROLLMENT_OPERATION_ERRO_21_001: [The BulkEnrollmentOperationError shall throws JsonSerializationException if the 
                                                    provided registrationId is null, empty, or invalid.] */
                ParserUtils.EnsureRegistrationId(value);
                _registrationId = value;
            }
        }
        private string _registrationId;

        /// <summary>
        /// Error code.
        /// </summary>
        /// <remarks>
        /// Report any error during the operation for the specific registrationId.
        /// </remarks>
        [JsonProperty(PropertyName = "errorCode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? ErrorCode { get; private set; }

        /// <summary>
        /// Error status.
        /// </summary>
        /// <remarks>
        /// Describe any error during the operation for the specific registrationId.
        /// </remarks>
        [JsonProperty(PropertyName = "errorStatus", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ErrorStatus { get; private set; }
    }
}
