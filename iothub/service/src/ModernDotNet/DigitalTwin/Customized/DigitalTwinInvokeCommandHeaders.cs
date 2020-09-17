// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Defines headers for InvokeCommand operation.
    /// </summary>
    public class DigitalTwinInvokeCommandHeaders
    {
        /// <summary>
        /// Initializes a new instance of the
        /// DigitalTwinInvokeRootLevelCommandHeaders class.
        /// </summary>
        public DigitalTwinInvokeCommandHeaders()
        {
        }

        /// <summary>
        /// Initializes a new instance of the
        /// DigitalTwinInvokeRootLevelCommandHeaders class.
        /// </summary>
        /// <param name="xMsCommandStatuscode">Device Generated Status Code for
        /// this Operation</param>
        /// <param name="xMsRequestId">Server Generated Request Id (GUID), to
        /// uniquely identify this request in the service</param>
        public DigitalTwinInvokeCommandHeaders(int? xMsCommandStatuscode = default, string xMsRequestId = default)
        {
            XMsCommandStatuscode = xMsCommandStatuscode;
            XMsRequestId = xMsRequestId;
        }

        /// <summary>
        /// Gets or sets device Generated Status Code for this Operation
        /// </summary>
        [JsonProperty(PropertyName = "x-ms-command-statuscode")]
        public int? XMsCommandStatuscode { get; set; }

        /// <summary>
        /// Gets or sets server Generated Request Id (GUID), to uniquely
        /// identify this request in the service
        /// </summary>
        [JsonProperty(PropertyName = "x-ms-request-id")]
        public string XMsRequestId { get; set; }
    }
}
