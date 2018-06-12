// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
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
    /// <code>
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
    /// </code>
    /// </example>
    /// <seealso cref="!:https://docs.microsoft.com/en-us/rest/api/iot-dps/deviceenrollment">Device Enrollment</seealso>
    public class BulkEnrollmentOperationResult
    {
        /* SRS_BULK_ENROLLMENT_OPERATION_RESULT_21_001: [The BulkEnrollmentOperationResult shall throws JsonSerializationException if the 
                                            provided registrationId is null, empty, or invalid.] */
        /* SRS_BULK_ENROLLMENT_OPERATION_RESULT_21_002: [The BulkEnrollmentOperationResult shall store the provided information.] */

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
        /// <returns>The <code>string</code> with the content of this class in a pretty print format.</returns>
        public override string ToString()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
