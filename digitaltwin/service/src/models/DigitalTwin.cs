using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.IoT.DigitalTwin.Service.Models
{
    public partial class DigitalTwin
    {
        /// <summary>
        /// Initializes a new instance of the DigitalTwinInterfaces class.
        /// </summary>
        public DigitalTwin()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the DigitalTwinInterfaces class.
        /// </summary>
        /// <param name="interfaces">Interface(s) data on the digital
        /// twin.</param>
        /// <param name="version">Version of digital twin.</param>
        public DigitalTwin(IDictionary<string, InterfaceModel> interfaces = default(IDictionary<string, InterfaceModel>), long? version = default(long?))
        {
            Components = interfaces;
            Version = version;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets interface(s) data on the digital twin.
        /// </summary>
        [JsonProperty(PropertyName = "interfaces")]
        public IDictionary<string, InterfaceModel> Components { get; set; }

        /// <summary>
        /// Gets or sets version of digital twin.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public long? Version { get; set; }
    }
}
