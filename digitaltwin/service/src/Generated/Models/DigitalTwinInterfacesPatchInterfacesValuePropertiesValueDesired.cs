// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Azure.IoT.DigitalTwin.Service.Generated.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    internal partial class DigitalTwinInterfacesPatchInterfacesValuePropertiesValueDesired
    {
        /// <summary>
        /// Initializes a new instance of the
        /// DigitalTwinInterfacesPatchInterfacesValuePropertiesValueDesired
        /// class.
        /// </summary>
        public DigitalTwinInterfacesPatchInterfacesValuePropertiesValueDesired()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the
        /// DigitalTwinInterfacesPatchInterfacesValuePropertiesValueDesired
        /// class.
        /// </summary>
        /// <param name="value">The desired value of the interface property to
        /// set in a digitalTwin.</param>
        public DigitalTwinInterfacesPatchInterfacesValuePropertiesValueDesired(object value = default(object))
        {
            Value = value;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets the desired value of the interface property to set in
        /// a digitalTwin.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public object Value { get; set; }

    }
}