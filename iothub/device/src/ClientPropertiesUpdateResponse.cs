// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The response of an <see cref="DeviceClient.UpdateClientPropertiesAsync(ClientPropertyCollection, System.Threading.CancellationToken)"/> operation.
    /// </summary>
    public class ClientPropertiesUpdateResponse
    {
        /// <summary>
        /// The request Id that is appended to the <see cref="DeviceClient.UpdateClientPropertiesAsync(ClientPropertyCollection, System.Threading.CancellationToken)"/> operation.
        /// </summary>
        /// <remarks>
        /// This request Id is relevant only for operations over MQTT, and can be used for debugging the operation from the service side.
        /// </remarks>
        public string RequestId { get; internal set; }

        /// <summary>
        /// The updated version after the property patch has been aplied.
        /// </summary>
        public long Version { get; internal set; }
    }
}
