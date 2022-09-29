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
    ///     just cast the items. In case of unknown type, the items will contain a list of string
    ///     and you shall parse it by your own.
    ///
    /// The provisioning service query result is composed by 2 system properties and a body. This class exposes
    ///     it with 3 getters, <see cref="Type"/>, <see cref="Items"/>, and <see cref="ContinuationToken"/>.
    ///
    /// The system properties are:
    /// <list type="bullet">
    ///     <item>
    ///     <description>type:
    ///         Identify the type of the content in the body. You can use it to cast the objects
    ///         in the items list. See <see cref="QueryResultType"/> for the possible types and classes
    ///         to cast.</description>
    ///     </item>
    ///     <item>
    ///     <description>continuationToken:
    ///         Contains the token the uniquely identify the next page of information. The
    ///         service will return the next page of this query when you send a new query with
    ///         this token.</description>
    ///     </item>
    /// </list>
    ///
    /// And the body is a JSON list of the specific type. For instance, if the system
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
        public IEnumerable<object> Items { get; private set; }

        /// <summary>
        /// Getter for the query result continuationToken.
        /// </summary>
        public string ContinuationToken { get; private set; }

        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <param name="typeString">The string with type of the content in the body.
        /// It cannot be null.</param>
        /// <param name="bodyString">The string with the body in a JSON list format.
        /// It cannot be null, or empty, if the type is different than `unknown`.</param>
        /// <param name="continuationToken">The string with the continuation token.
        /// It can be null.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="bodyString"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="bodyString"/> is empty or white space.</exception>
        ///
        internal QueryResult(string typeString, string bodyString, string continuationToken)
        {
            Type = (QueryResultType)Enum.Parse(typeof(QueryResultType), typeString, true);
            ContinuationToken = string.IsNullOrWhiteSpace(continuationToken)
                ? null
                : continuationToken;

            if (Type != QueryResultType.Unknown && string.IsNullOrWhiteSpace(bodyString))
            {
                if (bodyString == null)
                {
                    throw new ArgumentNullException(nameof(bodyString));
                }

                throw new ArgumentException("Invalid query body.", nameof(bodyString));
            }

            switch (Type)
            {
                case QueryResultType.Enrollment:
                    Items = JsonConvert.DeserializeObject<IEnumerable<IndividualEnrollment>>(bodyString);
                    break;

                case QueryResultType.EnrollmentGroup:
                    Items = JsonConvert.DeserializeObject<IEnumerable<EnrollmentGroup>>(bodyString);
                    break;

                case QueryResultType.DeviceRegistration:
                    Items = JsonConvert.DeserializeObject<IEnumerable<DeviceRegistrationState>>(bodyString);
                    break;

                default:
                    if (bodyString == null)
                    {
                        Items = null;
                    }
                    else
                    {
                        try
                        {
                            Items = JsonConvert.DeserializeObject<IEnumerable<JObject>>(bodyString);
                        }
                        catch (ArgumentException)
                        {
                            try
                            {
                                Items = JsonConvert.DeserializeObject<IEnumerable<object>>(bodyString);
                            }
                            catch (ArgumentException)
                            {
                                Items = new string[] { bodyString };
                            }
                        }
                        catch (JsonSerializationException)
                        {
                            Items = JsonConvert.DeserializeObject<IEnumerable<object>>(bodyString);
                        }
                        catch (JsonReaderException)
                        {
                            Items = new string[] { bodyString };
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Convert this object in a pretty print format.
        /// </summary>
        /// <returns>The string with the content of this class in a pretty print format.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
