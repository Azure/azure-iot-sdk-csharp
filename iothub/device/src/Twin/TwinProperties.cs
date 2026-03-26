// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A container for client properties retrieved from the service.
    /// </summary>
    public class TwinProperties
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <remarks>
        /// This class can be inherited from and set by unit tests for mocking purposes.
        /// </remarks>
        protected internal TwinProperties(PropertyCollection requestsFromService, PropertyCollection reportedByClient)
        {
            Desired = requestsFromService;
            Reported = reportedByClient;
        }

        /// <summary>
        /// The collection of desired property update requests received from service.
        /// </summary>
        public PropertyCollection Desired { get; }

        /// <summary>
        /// The collection of twin properties reported by the client.
        /// </summary>
        public PropertyCollection Reported { get; }

        /// <summary>
        /// Gets the Twin as a JSON string
        /// </summary>
        /// <returns>JSON string</returns>
        public string ToJson()
        {
            var properties = new Dictionary<string, object>
            {
                { "desired", Desired },
                { "reported", Reported }
            };

            var formattedTwinProperties = new Dictionary<string, Dictionary<string, object>>
            {
                { "properties", properties },
            };

            return JsonSerializer.Serialize(formattedTwinProperties, JsonSerializerSettings.Options);
        }
    }
}
