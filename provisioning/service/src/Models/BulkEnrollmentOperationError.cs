// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service device registration operation error.
    /// </summary>
    /// <remarks>
    /// This error is returned as a result of the
    /// <see cref="IndividualEnrollmentsClient.RunBulkOperationAsync(BulkOperationMode, IEnumerable{IndividualEnrollment}, CancellationToken)"/>
    /// as part of the <see cref="BulkEnrollmentOperationResult"/>.
    /// </remarks>
    public class BulkEnrollmentOperationError
    {
        /// <summary>
        /// Registration Id.
        /// </summary>
        /// <remarks>
        /// A valid registration Id shall be alphanumeric, lowercase, and may contain hyphens. Max characters 128.
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the provided registrationId does not fit the registration Id requirements</exception>
        [JsonProperty("registrationId")]
        public string RegistrationId { get; private set; }

        /// <summary>
        /// Error code.
        /// </summary>
        /// <remarks>
        /// Report any error during the operation for the specific registrationId.
        /// </remarks>
        [JsonProperty("errorCode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? ErrorCode { get; private set; }

        /// <summary>
        /// Error status.
        /// </summary>
        /// <remarks>
        /// Describe any error during the operation for the specific registrationId.
        /// </remarks>
        [JsonProperty("errorStatus", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ErrorStatus { get; private set; }
    }
}
