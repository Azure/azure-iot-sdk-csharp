// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The JSON object for bulk enrollment group operations.
    /// </summary>
    /// <remarks>
    /// It is an internal class that creates a JSON for the bulk operations over the IndividualEnrollment. To use bulk operations, please use
    /// the external API <see cref="IndividualEnrollmentsClient.RunBulkOperationAsync(BulkOperationMode, IEnumerable{IndividualEnrollment}, CancellationToken)"/>.
    /// </remarks>
    internal sealed class EnrollmentGroupBulkOperation
    {
        /// <summary>
        /// Operation mode
        /// </summary>
        [JsonPropertyName("mode")]
        internal BulkOperationMode Mode { get; set; }

        /// <summary>
        /// Enrollments for bulk operation
        /// </summary>
        [JsonPropertyName("enrollmentGroups")]
        internal IEnumerable<EnrollmentGroup> Enrollments { get; set; }
    }
}
