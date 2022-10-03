// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service bulk operation with a JSON serializer.
    /// </summary>
    /// <remarks>
    /// It is an internal class that creates a JSON for the bulk operations over the IndividualEnrollment. To use bulk operations, please use
    /// the external API <see cref="ProvisioningServiceClient.RunBulkEnrollmentOperationAsync(BulkOperationMode, IEnumerable{IndividualEnrollment}, CancellationToken)"/>.
    ///
    /// The following JSON is an example of the result of this serializer.
    /// </remarks>
    internal static class BulkEnrollmentOperation
    {
        private sealed class BulkOperation
        {
            /// <summary>
            /// Operation mode
            /// </summary>
            [JsonProperty(PropertyName = "mode", Required = Required.Always)]
            public BulkOperationMode Mode { get; set; }

            /// <summary>
            /// Enrollments for bulk operation
            /// </summary>
            [JsonProperty(PropertyName = "enrollments", Required = Required.Always)]
            public IEnumerable<IndividualEnrollment> Enrollments { get; set; }
        }

        /// <summary>
        /// Serializer
        /// </summary>
        /// <remarks>
        /// Creates a string, whose content represents the mode and the collection of
        /// individualEnrollments in a JSON format.
        /// </remarks>
        /// <param name="mode">the <see cref="BulkOperationMode"/> that defines the single operation to do over the
        /// individualEnrollments.</param>
        /// <param name="individualEnrollments">the collection of <see cref="IndividualEnrollment"/> that contains the description
        /// of each individualEnrollment.</param>
        /// <returns>The string with the content of this class.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="individualEnrollments"/> is null.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="individualEnrollments"/> is an empty collection.</exception>
        public static string ToJson(BulkOperationMode mode, IEnumerable<IndividualEnrollment> individualEnrollments)
        {
            if (!(individualEnrollments ?? throw new ArgumentNullException(nameof(individualEnrollments))).Any())
            {
                throw new ArgumentException("The collection is null or empty.", nameof(individualEnrollments));
            }

            var bulkOperation = new BulkOperation
            {
                Mode = mode,
                Enrollments = individualEnrollments,
            };
            return JsonConvert.SerializeObject(bulkOperation);
        }
    }
}
