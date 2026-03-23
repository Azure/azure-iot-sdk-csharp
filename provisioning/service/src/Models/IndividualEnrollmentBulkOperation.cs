// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The JSON object for bulk individual enrollment operations.
    /// </summary>
    public sealed class IndividualEnrollmentBulkOperation
    {
        /// <summary>
        /// Operation mode
        /// </summary>
        [JsonPropertyName("mode")]
        public BulkOperationMode Mode { get; set; }

        /// <summary>
        /// Enrollments for bulk operation
        /// </summary>
        [JsonPropertyName("enrollments")]
        public IList<IndividualEnrollment> Enrollments { get; set; }
    }
}
