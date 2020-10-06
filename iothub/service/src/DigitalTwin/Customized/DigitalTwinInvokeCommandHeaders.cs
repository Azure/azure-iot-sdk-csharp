// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Defines headers for InvokeCommandAsync and InvokeComponentCommandAsync operation.
    /// </summary>
    public class DigitalTwinInvokeCommandHeaders
    {
        /// <summary>
        /// Initializes a new instance of the
        /// DigitalTwinInvokeCommandHeaders class.
        /// </summary>
        /// <param name="requestId">Server Generated Request Id (GUID), to uniquely identify this request in the service</param>
        public DigitalTwinInvokeCommandHeaders(string requestId = default)
        {
            RequestId = requestId;
        }

        /// <summary>
        /// Gets or sets server Generated Request Id (GUID), to uniquely
        /// identify this request in the service.
        /// </summary>
        public string RequestId { get; internal set; }
    }
}
