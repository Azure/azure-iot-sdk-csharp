// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A container for client properties retrieved from the service.
    /// </summary>
    public class ClientTwin
    {
        internal ClientTwin(DesiredPropertyCollection requestsFromService, ReportedPropertyCollection reportedByClient)
        {
            RequestsFromService = requestsFromService;
            ReportedByClient = reportedByClient;
        }

        /// <summary>
        /// The collection of desired property update requests received from service.
        /// </summary>
        public DesiredPropertyCollection RequestsFromService { get; }

        /// <summary>
        /// The collection of twin properties reported by the client.
        /// </summary>
        public ReportedPropertyCollection ReportedByClient { get; }
    }
}
