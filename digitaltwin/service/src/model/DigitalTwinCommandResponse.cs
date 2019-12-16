using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.DigitalTwin.Service.Models
{
    public class DigitalTwinCommandResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinCommandResponse"/> class.
        /// </summary>
        /// <param name="requestId">The request identifier.</param>
        /// <param name="status">The status.</param>
        /// <param name="payload">The payload.</param>
        public DigitalTwinCommandResponse(string requestId, int? status, string payload)
        {
            this.Payload = payload;
            this.RequestId = requestId;
            this.Status = status;
        }

        /// <summary>
        /// Gets the serialized payload associated with this update.
        /// </summary>
        public string Payload { get; }

        /// <summary>
        /// Gets the command request id associated with this update.
        /// </summary>
        public string RequestId { get; }

        /// <summary>
        /// Gets the status associated with this update.
        /// </summary>
        public int? Status { get; }
    }
}
