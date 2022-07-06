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
        internal WritableClientProperty()
        {
        }

        /// <summary>
        /// The value of the writable property update request.
        /// </summary>
        public object Value { get; internal set; }

        /// <summary>
        /// The version number associated with the writable property update request.
        /// </summary>
        public long Version { get; internal set; }

        internal PayloadConvention Convention { get; set; }

        /// <summary>
        /// Creates a writable property update response that contains the requested property value and version that can be reported back to the service.
        /// You will need to send both the property name and this response object when acknowledging a writable property update request.
        /// </summary>
        /// <remarks>
        /// To construct a writable property update response with custom value and version number, use
        /// <see cref="PayloadSerializer.CreateWritablePropertyAcknowledgementValue(object, int, long, string)"/> from
        /// <see cref="DeviceClient.PayloadConvention"/>.
        /// <para>
        /// See <see href="https://docs.microsoft.com/azure/iot-develop/concepts-convention#writable-properties"/> for more details.
        /// </para>
        /// </remarks>
        /// <param name="statusCode">An acknowledgment code that uses an HTTP status code.</param>
        /// <param name="description">An optional acknowledgment description.</param>
        /// <returns>A writable property update response that can be reported back to the service.</returns>
        public IWritablePropertyAcknowledgementValue AcknowledgeWith(int statusCode, string description = default)
        {
            return Convention.PayloadSerializer.CreateWritablePropertyAcknowledgementValue(Value, statusCode, Version, description);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        public bool TryGetValue<T>(out T propertyValue)
        {
            return ObjectConversionHelpers.TryCastOrConvert(Value, Convention, out propertyValue);
        }
    }
}
