// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The writable property update request received from service.
    /// </summary>
    /// <remarks>
    /// A writable property update request should be acknowledged by the device or module by sending a reported property.
    /// This type contains a convenience method to format the reported property as per IoT Plug and Play convention.
    /// For more details see <see href="https://docs.microsoft.com/azure/iot-develop/concepts-convention#writable-properties"/>.
    /// </remarks>
    public class WritableClientProperty
    {
        // TODO: Unit-testable and mockable

        internal WritableClientProperty()
        {
        }

        /// <summary>
        /// The value of the writable property update request.
        /// </summary>
        public object Value { get; internal set; }

        internal long Version { get; set; }

        internal PayloadConvention Convention { get; set; }

        /// <summary>
        /// Creates a writable property update payload that contains the requested property value and version to be reported back to the service
        /// using <see cref="DeviceClient.UpdateClientPropertiesAsync(ClientPropertyCollection, System.Threading.CancellationToken)"/>
        /// or <see cref="ModuleClient.UpdateClientPropertiesAsync(ClientPropertyCollection, System.Threading.CancellationToken)"/>.
        /// Send the component name (if applicable), property name and this payload when acknowledging a writable property update request.
        /// </summary>
        /// <remarks>
        /// To construct a writable property update payload with custom value and version number, use
        /// <see cref="PayloadSerializer.CreateWritablePropertyAcknowledgementValue(object, int, long, string)"/> from
        /// <see cref="DeviceClient.PayloadConvention"/>.
        /// <para>
        /// See <see href="https://docs.microsoft.com/azure/iot-develop/concepts-convention#writable-properties"/> for more details.
        /// </para>
        /// </remarks>
        /// <param name="statusCode">An acknowledgment code that uses an HTTP status code.</param>
        /// <param name="description">An optional acknowledgment description.</param>
        /// <returns>A writable property update payload to be reported back to the service.</returns>
        public IWritablePropertyAcknowledgementValue AcknowledgeWith(int statusCode, string description = default)
        {
            return Convention.PayloadSerializer.CreateWritablePropertyAcknowledgementValue(Value, statusCode, Version, description);
        }

        /// <summary>
        /// The value of the writable property update request, deserialized to the specified type.
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
