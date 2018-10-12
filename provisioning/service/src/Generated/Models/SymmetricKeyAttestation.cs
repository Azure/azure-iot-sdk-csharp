// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.Devices.Provisioning.Service.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Attestation via SymmetricKey.
    /// </summary>
    public partial class SymmetricKeyAttestation
    {
        /// <summary>
        /// Initializes a new instance of the SymmetricKeyAttestation class.
        /// </summary>
        public SymmetricKeyAttestation()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the SymmetricKeyAttestation class.
        /// </summary>
        /// <param name="primaryKey">Primary symmetric key.</param>
        /// <param name="secondaryKey">Secondary symmetric key.</param>
        public SymmetricKeyAttestation(string primaryKey = default(string), string secondaryKey = default(string))
        {
            PrimaryKey = primaryKey;
            SecondaryKey = secondaryKey;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets primary symmetric key.
        /// </summary>
        [JsonProperty(PropertyName = "primaryKey")]
        public string PrimaryKey { get; set; }

        /// <summary>
        /// Gets or sets secondary symmetric key.
        /// </summary>
        [JsonProperty(PropertyName = "secondaryKey")]
        public string SecondaryKey { get; set; }

    }
}
