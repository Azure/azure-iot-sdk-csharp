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
        /// Gets or sets server Generated Request Id (GUID), to uniquely
        /// identify this request in the service.
        /// </summary>
        public string RequestId { get; set; }
    }
}
