﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

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
        /// Creates a writable property update response that can be reported back to the service.
        /// </summary>
        /// <remarks>
        /// This writable property update response will contain the property value and version supplied in the writable property update request.
        /// If you would like to construct your own writable property update response with custom value and version number, you can
        /// create an instance of <see cref="PayloadSerializer.CreateWritablePropertyResponse(object, int, long, string)"/>.
        /// See <see href="https://docs.microsoft.com/azure/iot-develop/concepts-convention#writable-properties"/> for more details.
        /// </remarks>
        /// <param name="statusCode">An acknowledgment code that uses an HTTP status code.</param>
        /// <param name="description">An optional acknowledgment description.</param>
        /// <returns>A writable property update response that can be reported back to the service.</returns>
        public IWritablePropertyResponse AcknowledgeWith(int statusCode, string description = default)
        {
            return Convention.PayloadSerializer.CreateWritablePropertyResponse(Value, statusCode, Version, description);
        }
    }
}
