// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service bulk operation result with a JSON deserializer.
    /// </summary>
    /// <remarks>
    /// This error is returned as a result of the
    /// <see cref="EnrollmentGroupsClient.RunBulkOperationAsync(BulkOperationMode, IEnumerable{EnrollmentGroup}, CancellationToken)"/>.
    ///
    /// The provisioning service provides general bulk result in the isSuccessful, and a individual error result
    /// for each enrollment in the bulk.
    /// </remarks>
    public class BulkEnrollmentOperationResult
    {
        /// <summary>
        /// If false, not all operations in the bulk enrollment succeeded.
        /// </summary>
        [JsonProperty("isSuccessful", Required = Required.Always)]
        public bool IsSuccessful { get; protected internal set; }

        /// <summary>
        /// Registration errors.
        /// </summary>
        /// <remarks>
        /// Detail each enrollment failed in the bulk operation, and report the fail reason.
        /// </remarks>
        [JsonProperty("errors", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IList<BulkEnrollmentOperationError> Errors { get; protected internal set; } = new List<BulkEnrollmentOperationError>();
    }
}
