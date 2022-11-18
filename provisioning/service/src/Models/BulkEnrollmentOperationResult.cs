// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service bulk operation result with a JSON deserializer.
    /// </summary>
    /// <remarks>
    /// This error is returned as a result of the
    ///     <see cref="IndividualEnrollmentsClient.RunBulkOperationAsync(BulkOperationMode, IEnumerable{IndividualEnrollment}, CancellationToken)"/>.
    ///
    /// The provisioning service provides general bulk result in the isSuccessful, and a individual error result
    ///     for each enrollment in the bulk.
    /// </remarks>
    /// <example>
    ///  The following JSON is an example of the result from a bulk operation.
    /// <code language="json">
    /// {
    ///     "isSuccessful": true,
    ///     "errors": [
    ///         {
    ///             "registrationId": "validRegistrationId1",
    ///             "errorCode": 200,
    ///             "errorStatus": "Succeeded"
    ///         },
    ///         {
    ///             "registrationId": "validRegistrationId2",
    ///             "errorCode": 200,
    ///             "errorStatus": "Succeeded"
    ///         }
    ///     ]
    /// }
    /// </code>
    /// </example>
    public class BulkEnrollmentOperationResult
    {
        /// <summary>
        /// If false, not all operations in the bulk enrollment succeeded.
        /// </summary>
        [JsonPropertyName("isSuccessful", Required = Required.Always)]
        public bool IsSuccessful { get; protected internal set; }

        /// <summary>
        /// Registration errors.
        /// </summary>
        /// <remarks>
        /// Detail each enrollment failed in the bulk operation, and report the fail reason.
        /// </remarks>
        [JsonPropertyName("errors")]
        public IEnumerable<BulkEnrollmentOperationError> Errors { get; protected internal set; }

        /// <summary>
        /// Convert this object in a pretty print format.
        /// </summary>
        /// <returns>The string with the content of this class in a pretty print format.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(
                this,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                });
        }
    }
}
