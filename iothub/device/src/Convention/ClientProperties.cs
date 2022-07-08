// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A container for properties retrieved from the service.
    /// </summary>
    public class ClientProperties
    {
        // TODO: Unit-testable and mockable

        /// <summary>
        /// Initializes a new instance of <see cref="ClientProperties"/> with the specified collections.
        /// </summary>
        /// <param name="writablePropertyRequestCollection">A collection of writable property requests returned from IoT Hub.</param>
        /// <param name="clientReportedPropertyCollection">A collection of client reported properties returned from IoT Hub.</param>
        internal ClientProperties(WritableClientPropertyCollection writablePropertyRequestCollection, ClientPropertyCollection clientReportedPropertyCollection)
        {
            WritablePropertyRequests = writablePropertyRequestCollection;
            ReportedByClient = clientReportedPropertyCollection;
        }

        /// <summary>
        /// The collection of writable property requests received from service.
        /// </summary>
        /// <remarks>
        /// See the <see href="https://docs.microsoft.com/azure/iot-pnp/concepts-convention#writable-properties">Writable properties</see> documentation for more information.
        /// </remarks>
        public WritableClientPropertyCollection WritablePropertyRequests { get; }

        /// <summary>
        /// The collection of properties reported by the client.
        /// </summary>
        /// <remarks>
        /// Client reported properties can either be <see href="https://docs.microsoft.com/azure/iot-develop/concepts-convention#read-only-properties">Read-only properties</see>
        /// or they can be <see href="https://docs.microsoft.com/azure/iot-pnp/concepts-convention#writable-properties">Writable property acknowledgements</see>.
        /// </remarks>
        public ClientPropertyCollection ReportedByClient { get; }
    }
}
