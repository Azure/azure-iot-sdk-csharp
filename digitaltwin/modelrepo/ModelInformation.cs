using System;

namespace Microsoft.Azure.DigitalTwin.Model.Service
{
    /// <summary>
    /// Object containing Model information
    /// </summary>
    public class ModelInformation
    {
        public string Comment { get; set; }
        public string Description { get; set; }
        public string DisplayName { get; set; }
        public string UrnId { get; set; }
        public string ModelName { get; set; }
        public int? Version { get; set; }
        public string Type { get; set; }
        public string Etag { get; set; }
        public string PublisherId { get; set; }
        public string PublisherName { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
