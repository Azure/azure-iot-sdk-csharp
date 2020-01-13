using System.Net;

namespace Microsoft.Azure.DigitalTwin.Model.Service
{
    /// <summary>
    /// Defines headers for DeleteModel operation.
    /// </summary>
    public class DeleteModelResponse
    {
        public string RequestId { get; set; }

        public HttpStatusCode StatusCode { get; set; }

    }
}
