// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service query response with a JSON deserializer.
    /// </summary>
    /// <remarks>
    /// It is the result of any query for the provisioning service. This class will parse the result and
    ///     return it in a best format possible. For the known formats in <see cref="QueryResultType"/>, you can
    ///     just cast the items. In case of <b>unknown</b> type, the items will contain a list of <c>string</c>
    ///     and you shall parse it by your own.
    ///
    /// The provisioning service query result is composed by 2 system properties and a body. This class exposes
    ///     it with 3 getters, <see cref="Type"/>, <see cref="Items"/>, and <see cref="ContinuationToken"/>.
    ///
    /// The system properties are:
    /// <list type="bullet">
    ///     <item>
    ///     <description><b>type:</b>
    ///         Identify the type of the content in the body. You can use it to cast the objects
    ///         in the items list. See <see cref="QueryResultType"/> for the possible types and classes
    ///         to cast.</description>
    ///     </item>
    ///     <item>
    ///     <description><b>continuationToken:</b>
    ///         Contains the token the uniquely identify the next page of information. The
    ///         service will return the next page of this query when you send a new query with
    ///         this token.</description>
    ///     </item>
    /// </list>
    ///
    /// And the body is a JSON list of the specific <b>type</b>. For instance, if the system
    ///     property type is IndividualEnrollment, the body will look like:
    /// <code>
    /// [
    ///     {
    ///         "registrationId":"validRegistrationId-1",
    ///         "deviceId":"ContosoDevice-1",
    ///         "attestation":{
    ///             "type":"tpm",
    ///             "tpm":{
    ///                 "endorsementKey":"validEndorsementKey"
    ///             }
    ///         },
    ///         "iotHubHostName":"ContosoIoTHub.azure-devices.net",
    ///         "provisioningStatus":"enabled"
    ///     },
    ///     {
    ///         "registrationId":"validRegistrationId-2",
    ///         "deviceId":"ContosoDevice-2",
    ///         "attestation":{
    ///             "type":"tpm",
    ///            "tpm":{
    ///                 "endorsementKey":"validEndorsementKey"
    ///             }
    ///         },
    ///         "iotHubHostName":"ContosoIoTHub.azure-devices.net",
    ///         "provisioningStatus":"enabled"
    ///     }
    /// ]
    /// </code>
    /// </remarks>
    public class QueryResult
    {
        /// <summary>
        /// Getter for the query result Type.
        /// </summary>
        public QueryResultType Type { get; private set; }

        /// <summary>
        /// Getter for the list of query result Items.
        /// </summary>
        public IEnumerable<Object> Items { get; private set; }

        /// <summary>
        /// Getter for the query result continuationToken.
        /// </summary>
        public string ContinuationToken { get; private set; }

        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <param name="typeString">the <code>string</code> with type of the content in the body. It cannot be <code>null</code>.</param>
        /// <param name="bodyString">the <code>string</code> with the body in a JSON list format. It cannot be <code>null</code>, or empty, if the type is different than `unknown`.</param>
        /// <param name="continuationToken">the <code>string</code> with the continuation token. It can be <code>null</code>.</param>
        /// <exception cref="ArgumentException">if one of the provided parameters is invalid.</exception>
        internal QueryResult(string typeString, string bodyString, string continuationToken)
        {
            /* SRS_QUERY_RESULT_21_001: [The constructor shall throw ArgumentException if the provided type is null, empty, or not parsed to QueryResultType.] */
            /* SRS_QUERY_RESULT_21_010: [The constructor shall store the provided parameters `type` and `continuationToken`.] */
            Type = (QueryResultType)Enum.Parse(typeof(QueryResultType), typeString, true);
            if (string.IsNullOrWhiteSpace(continuationToken))
            {
                ContinuationToken = null;
            }
            else
            {
                ContinuationToken = continuationToken;
            }

            /* SRS_QUERY_RESULT_21_002: [The constructor shall throw ArgumentException if the provided body is null or empty and the type is not `unknown`.] */
            if ((Type != QueryResultType.Unknown) && string.IsNullOrWhiteSpace(bodyString))
            {
                if(bodyString == null)
                {
                    throw new ArgumentNullException(nameof(bodyString));
                }

                throw new ArgumentException("Invalid query body.", nameof(bodyString));
            }

            /* SRS_QUERY_RESULT_21_003: [The constructor shall throw JsonSerializationException if the JSON is invalid.] */
            switch (Type)
            {
                case QueryResultType.Enrollment:
                    /* SRS_QUERY_RESULT_21_004: [If the type is `enrollment`, the constructor shall parse the body as IndividualEnrollment[].] */
                    Items = JsonConvert.DeserializeObject<IEnumerable<IndividualEnrollment>>(bodyString);
                    break;
                case QueryResultType.EnrollmentGroup:
                    /* SRS_QUERY_RESULT_21_005: [If the type is `enrollmentGroup`, the constructor shall parse the body as EnrollmentGroup[].] */
                    Items = JsonConvert.DeserializeObject<IEnumerable<EnrollmentGroup>>(bodyString);
                    break;
                case QueryResultType.DeviceRegistration:
                    /* SRS_QUERY_RESULT_21_006: [If the type is `deviceRegistration`, the constructor shall parse the body as DeviceRegistrationState[].] */
                    Items = JsonConvert.DeserializeObject<IEnumerable<DeviceRegistrationState>>(bodyString);
                    break;
                default:
                    if(bodyString == null)
                    {
                        /* SRS_QUERY_RESULT_21_007: [If the type is `unknown`, and the body is null, the constructor shall set `items` as null.] */
                        Items = null;
                    }
                    else
                    {
                        try
                        {
                            /* SRS_QUERY_RESULT_21_008: [If the type is `unknown`, the constructor shall try to parse the body as JObject[].] */
                            Items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(bodyString);
                        }
                        catch (ArgumentException)
                        {
                            try
                            {
                                /* SRS_QUERY_RESULT_21_009: [If the type is `unknown`, the constructor shall try to parse the body as Object[].] */
                                Items = JsonConvert.DeserializeObject<IEnumerable<Object>>(bodyString);
                            }
                            catch (ArgumentException)
                            {
                                /* SRS_QUERY_RESULT_21_010: [If the type is `unknown`, and the constructor failed to parse the body as JObject[] and Object[], it shall return the body as a single string in the items.] */
                                Items = new string[] { bodyString };
                            }
                        }
                        catch(JsonReaderException)
                        {
                            /* SRS_QUERY_RESULT_21_010: [If the type is `unknown`, and the constructor failed to parse the body as JObject[] and Object[], it shall return the body as a single string in the items.] */
                            Items = new string[] { bodyString };
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Convert this object in a pretty print format.
        /// </summary>
        /// <returns>The <code>string</code> with the content of this class in a pretty print format.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
