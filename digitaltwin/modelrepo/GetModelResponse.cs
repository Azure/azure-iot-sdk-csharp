using System;
using System.Net;

namespace Microsoft.Azure.DigitalTwin.Model.Service
{

    /// <summary>
    /// Defines headers for GetModel operation.
    /// </summary>
    public sealed class GetModelResponse
    {

        public string XMsRequestId { get; set; }

        public string ETag { get; set; }

        public string XMsModelId { get; set; }

        public string XMsModelPublisherId { get; set; }

        public string XMsModelPublisherName { get; set; }

        public DateTime? XMsModelCreatedon { get; set; }

        public DateTime? XMsModelLastupdated { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public string Payload { get; set; }

    }
}
