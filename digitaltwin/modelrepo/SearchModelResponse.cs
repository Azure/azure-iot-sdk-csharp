using System.Collections.Generic;
using System.Net;

namespace Microsoft.Azure.DigitalTwin.Model.Service
{
    public class SearchModelResponse
    {
        public string ContinuationToken { get; set; }

        public IReadOnlyList<ModelInformation> Results { get; set; }

        public HttpStatusCode StatusCode { get; set; }

    }
}
