// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A writable property update acknowledgement that contains the requested property name, property value, component name (if applicable) and version.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="ClientPropertyCollection.AddWritableClientPropertyAcknowledgement(WritableClientPropertyAcknowledgement)"/>
    /// to add this payload to your client properties to be reported back to the service using 
    /// <see cref="DeviceClient.UpdateClientPropertiesAsync(ClientPropertyCollection, System.Threading.CancellationToken)"/>
    /// (or corresponding method on the <see cref="ModuleClient"/>).
    /// </remarks>
    public class WritableClientPropertyAcknowledgement
    {
        // TODO: Unit-testable and mockable

        /// <summary>
        /// The name of the component for which an update request is received.
        /// This is <c>null</c> for an update request for a root-level writable property.
        /// </summary>
        public string ComponentName { get; set; }

        /// <summary>
        /// The name of the property for which an update request is received.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// The acknowledgement payload.
        /// </summary>
        public IWritablePropertyAcknowledgementPayload Payload { get; set; }
    }
}
