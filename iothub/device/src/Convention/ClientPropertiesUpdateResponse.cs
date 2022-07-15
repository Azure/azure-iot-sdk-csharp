// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The response of a <see cref="DeviceClient.UpdateClientPropertiesAsync(ClientPropertyCollection, System.Threading.CancellationToken)"/> 
    /// (or corresponding method on the <see cref="ModuleClient"/>) operation.
    /// </summary>
    public class ClientPropertiesUpdateResponse
    {
        // TODO: Unit-testable and mockable

        internal ClientPropertiesUpdateResponse()
        {
        }

        /// <summary>
        /// The request Id that is associated with the <see cref="DeviceClient.UpdateClientPropertiesAsync(ClientPropertyCollection, System.Threading.CancellationToken)"/> 
        /// (or corresponding method on the <see cref="ModuleClient"/>) operation.
        /// </summary>
        /// <remarks>
        /// This request Id is relevant only for operations over MQTT, and can be used for tracking the operation on service side logs.
        /// Note that you would need to contact the support team to track operations on the service side.
        /// </remarks>
        public string RequestId { get; internal set; }

        /// <summary>
        /// The updated version after the property patch has been applied.
        /// </summary>
        /// <remarks>
        /// For clients communicating with IoT hub via IoT Edge, since the patch isn't applied immediately an updated version number is not returned.
        /// You can call <see cref="ModuleClient.GetClientPropertiesAsync(System.Threading.CancellationToken)"/>
        /// and verify <see cref="ClientPropertyCollection.Version"/> from <see cref="ClientProperties.ReportedByClient"/> to check
        /// if your patch is successfully applied.
        /// </remarks>
        public long Version { get; internal set; }
    }
}
