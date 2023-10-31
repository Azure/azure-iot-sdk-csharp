// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

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
        protected internal TwinProperties(DesiredProperties requestsFromService, ReportedProperties reportedByClient)
        {
            Desired = requestsFromService;
            Reported = reportedByClient;
        }

        /// <summary>
        /// The collection of desired property update requests received from service.
        /// </summary>
        [JsonProperty("desired")]
        public DesiredProperties Desired { get; }

        /// <summary>
        /// The collection of twin properties reported by the client.
        /// </summary>
        [JsonProperty("reported")]
        public ReportedProperties Reported { get; }

        /// <summary>
        /// Gets the Twin as a JSON string
        /// </summary>
        /// <param name="formatting">Optional. Formatting for the output JSON string.</param>
        /// <returns>JSON string</returns>
        public string ToJson(Formatting formatting = Formatting.None)
        {
            var twin = new Dictionary<string, string>
            {
                { "desired", Desired.GetSerializedString() },
                { "reported", Reported.GetSerializedString() }
            };

            return JsonConvert.SerializeObject(twin);
        }
    }
}
