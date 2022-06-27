// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service bulk operation result with a JSON deserializer.
    /// </summary>
    /// <remarks>
    /// This error is returned as a result of the 
    ///     <see cref="ProvisioningServiceClient.RunBulkEnrollmentOperationAsync(BulkOperationMode, IEnumerable{IndividualEnrollment})"/>.
    ///
    /// The provisioning service provides general bulk result in the isSuccessful, and a individual error result
    ///     for each enrollment in the bulk.
    /// </remarks>
    /// <example>
    ///  The following JSON is an example of the result from a bulk operation.
    /// <c>
    /// {
    ///     "isSuccessful":true,
    ///     "errors": [
    ///         {
    ///             "registrationId":"validRegistrationId1",
    ///             "errorCode":200,
    ///             "errorStatus":"Succeeded"
    ///         },
    ///         {
    ///             "registrationId":"validRegistrationId2",
    ///             "errorCode":200,
    ///             "errorStatus":"Succeeded"
    ///         }
    ///     ]
    /// }
    /// </c>
    /// </example>
    public class BulkEnrollmentOperationResult
    {
        /// <summary>
        /// If false, not all operations in the bulk enrollment succeeded.
        /// </summary>
        [JsonProperty(PropertyName = "isSuccessful", Required = Required.Always)]
        public bool IsSuccessful { get; internal set; }

        /// <summary>
        /// Registration errors.
        /// </summary>
        /// <remarks>
        /// Detail each enrollment failed in the bulk operation, and report the fail reason.
        /// </remarks>
        [JsonProperty(PropertyName = "errors", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<BulkEnrollmentOperationError> Errors { get; internal set; }

        /// <summary>
        /// Convert this object in a pretty print format.
        /// </summary>
        /// <returns>The <c>string</c> with the content of this class in a pretty print format.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
