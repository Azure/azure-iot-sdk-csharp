// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.Devices.Generated.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Defines headers for GetDigitalTwin operation.
    /// </summary>
    public partial class DigitalTwinGetHeaders
    {
        /// <summary>
        /// Initializes a new instance of the DigitalTwinGetHeaders
        /// class.
        /// </summary>
        public DigitalTwinGetHeaders()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the DigitalTwinGetHeaders
        /// class.
        /// </summary>
        /// <param name="eTag">Weak Etag</param>
        public DigitalTwinGetHeaders(string eTag = default(string))
        {
            ETag = eTag;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets weak Etag
        /// </summary>
        [JsonProperty(PropertyName = "ETag")]
        public string ETag { get; set; }

    }
}