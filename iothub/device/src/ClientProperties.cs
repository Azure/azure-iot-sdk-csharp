﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A container for properties retrieved from the service.
    /// </summary>
    /// <remarks>
    /// The <see cref="ClientProperties"/> class is not meant to be constructed by customer code.
    /// It is intended to be returned fully populated from the client method <see cref="InternalClient.GetClientPropertiesAsync(System.Threading.CancellationToken)"/>.
    /// </remarks>
    public class ClientProperties
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ClientProperties"/>.
        /// This is provided for unit testing purposes only.
        /// </summary>
        /// <inheritdoc path="/remarks" cref="ClientProperties" />
        public ClientProperties()
        {
            WritablePropertyRequests = new ClientPropertyCollection();
            ReportedFromClient = new ClientPropertyCollection();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ClientProperties"/> with the specified collections.
        /// </summary>
        /// <param name="writablePropertyRequestCollection">A collection of writable property requests returned from IoT Hub.</param>
        /// <param name="clientReportedPropertyCollection">A collection of client reported properties returned from IoT Hub.</param>
        internal ClientProperties(ClientPropertyCollection writablePropertyRequestCollection, ClientPropertyCollection clientReportedPropertyCollection)
        {
            WritablePropertyRequests = writablePropertyRequestCollection;
            ReportedFromClient = clientReportedPropertyCollection;
        }

        /// <summary>
        /// The collection of writable property requests received from service.
        /// </summary>
        /// <remarks>
        /// See the <see href="https://docs.microsoft.com/azure/iot-pnp/concepts-convention#writable-properties">Writable properties</see> documentation for more information.
        /// </remarks>
        public ClientPropertyCollection WritablePropertyRequests { get; private set; }

        /// <summary>
        /// The collection of properties reported by the client.
        /// </summary>
        /// <remarks>
        /// Client reported properties can either be <see href="https://docs.microsoft.com/azure/iot-develop/concepts-convention#read-only-properties">Read-only properties</see>
        /// or they can be <see href="https://docs.microsoft.com/azure/iot-pnp/concepts-convention#writable-properties">Writable properties</see>.
        /// </remarks>
        public ClientPropertyCollection ReportedFromClient { get; private set; }
    }
}
