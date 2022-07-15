// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The writable property update request received from service.
    /// </summary>
    /// <remarks>
    /// A writable property update request should be acknowledged by the device or module by sending a reported property.
    /// This type contains a convenience method <see cref="AcknowledgeWith(int, string)"/> to format the reported property as per IoT Plug and Play convention.
    /// For more details see <see href="https://docs.microsoft.com/azure/iot-develop/concepts-convention#writable-properties"/>.
    /// </remarks>
    public class WritableClientProperty
    {
        // TODO: Unit-testable and mockable

        internal WritableClientProperty(long version, PayloadConvention payloadConvention)
        {
            Version = version;
            Convention = payloadConvention;
        }

        /// <summary>
        /// The name of the component for which an update request is received.
        /// This is <c>null</c> for an update request for a root-level writable property.
        /// </summary>
        public string ComponentName { get; internal set; }

        /// <summary>
        /// The name of the property for which an update request is received.
        /// </summary>
        public string PropertyName { get; internal set; }

        /// <summary>
        /// The value of the property for which an update request is received.
        /// </summary>
        public object Value { get; internal set; }

        private long Version { get; set; }

        private PayloadConvention Convention { get; set; }

        /// <summary>
        /// Creates a writable property update acknowledgement that contains the requested property name, property value, component name (if applicable) and version.
        /// </summary>
        /// <remarks>
        /// Use the <see cref="ClientPropertyCollection.AddWritableClientPropertyAcknowledgement(WritableClientPropertyAcknowledgement)"/>
        /// to add this payload to your client properties to be reported back to the service using 
        /// <see cref="DeviceClient.UpdateClientPropertiesAsync(ClientPropertyCollection, System.Threading.CancellationToken)"/>
        /// (or corresponding method on the <see cref="ModuleClient"/>).
        /// <para>
        /// To construct a writable property update payload with custom value and version number, use
        /// <see cref="PayloadSerializer.CreateWritablePropertyAcknowledgementPayload(object, int, long, string)"/> from
        /// <see cref="DeviceClient.PayloadConvention"/> to create a <see cref="WritableClientPropertyAcknowledgement"/>.
        /// </para>
        /// <para>
        /// See <see href="https://docs.microsoft.com/azure/iot-develop/concepts-convention#writable-properties"/> for more details.
        /// </para>
        /// </remarks>
        /// <param name="statusCode">An acknowledgment code that uses an HTTP status code.</param>
        /// <param name="description">An optional acknowledgment description.</param>
        /// <returns>A writable property update acknowledegement to be reported back to the service.</returns>
        public WritableClientPropertyAcknowledgement AcknowledgeWith(int statusCode, string description = default)
        {
            return new WritableClientPropertyAcknowledgement
            {
                ComponentName = ComponentName,
                PropertyName = PropertyName,
                Payload = Convention.PayloadSerializer.CreateWritablePropertyAcknowledgementPayload(Value, statusCode, Version, description),
            };
        }

        /// <summary>
        /// The value of the property for which an update request is received, deserialized to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to cast the <paramref name="propertyValue"/> to.</typeparam>
        /// <param name="propertyValue">When this method returns true, this contains the value of the writable property update request.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if a writable property update request of type <c>T</c> was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue<T>(out T propertyValue)
        {
            return ObjectConversionHelpers.TryCastOrConvert(Value, Convention, out propertyValue);
        }
    }
}
