// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The response of an <see cref="InternalClient.UpdateClientPropertiesAsync(ClientPropertyCollection, System.Threading.CancellationToken)"/> operation.
    /// </summary>
    public class ClientPropertiesUpdateResponse
    {
        /// <summary>
        /// The request Id that is associated with the <see cref="InternalClient.UpdateClientPropertiesAsync(ClientPropertyCollection, System.Threading.CancellationToken)"/> operation.
        /// </summary>
        /// <remarks>
        /// This request Id is relevant only for operations over MQTT, and can be used for tracking the operation on service side logs.
        /// Note that you would need to contact the support team to track operations on the service side.
        /// </remarks>
        public string RequestId { get; internal set; }

        /// <summary>
        /// The updated version after the property patch has been applied.
        /// </summary>
        public long Version { get; internal set; }
    }
}
