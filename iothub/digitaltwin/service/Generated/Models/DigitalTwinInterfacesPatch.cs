// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Azure.IoT.DigitalTwin.Service.Models
{
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public partial class DigitalTwinInterfacesPatch
    {
        /// <summary>
        /// Initializes a new instance of the DigitalTwinInterfacesPatch class.
        /// </summary>
        public DigitalTwinInterfacesPatch()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the DigitalTwinInterfacesPatch class.
        /// </summary>
        /// <param name="interfaces">Interface(s) data to patch in the digital
        /// twin.</param>
        public DigitalTwinInterfacesPatch(IDictionary<string, DigitalTwinInterfacesPatchInterfacesValue> interfaces = default(IDictionary<string, DigitalTwinInterfacesPatchInterfacesValue>))
        {
            Interfaces = interfaces;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets interface(s) data to patch in the digital twin.
        /// </summary>
        [JsonProperty(PropertyName = "interfaces")]
        public IDictionary<string, DigitalTwinInterfacesPatchInterfacesValue> Interfaces { get; set; }

    }
}
