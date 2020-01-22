using System.Net;

namespace Microsoft.Azure.DigitalTwin.Model.Service
{
    /// <summary>
    /// Defines headers for CreateOrUpdateModel operation.
    /// </summary>
    public class CreateOrUpdateModelResponse
    {
        public string XMsRequestId { get; set; }

        public string ETag { get; set; }

        public HttpStatusCode StatusCode { get; set; }
    }
}
