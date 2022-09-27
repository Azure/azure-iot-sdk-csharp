// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Represents the device command invocation results.
    /// </summary>
    public class InvokeDigitalTwinCommandResponse
    {
        /// <summary>
        /// This constructor is for deserialization and unit test mocking purposes.
        /// </summary>
        /// <remarks>
        /// To unit test methods that use this type as a response, inherit from this class and give it a constructor
        /// that can set the properties you want.
        /// </remarks>
        protected internal InvokeDigitalTwinCommandResponse()
        { }

        /// <summary>
        /// Command invocation result status, as supplied by the device.
        /// </summary>
        public int? Status { get; protected internal set; }

        /// <summary>
        /// Command invocation result payload, as supplied by the device.
        /// </summary>
        public string Payload { get; protected internal set; }

        /// <summary>
        /// Server generated request Id to uniquely identify this request in the service.
        /// </summary>
        public string RequestId { get; protected internal set; }
    }
}
