using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Substatus for 'Assigned' devices.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProvisioningServiceClientSubstatus
    {
        /// <summary>
        /// Device has been assigned to an IoT hub for the first time.
        /// </summary>
        [EnumMember(Value = "initialAssignment")]
        InitialAssignment = 1,

        /// <summary>
        /// Device has been assigned to a different IoT hub and its device data was migrated from the previously assigned
        /// IoT hub. Device data was removed from the previously assigned IoT hub.
        /// </summary>
        [EnumMember(Value = "deviceDataMigrated")]
        DeviceDataMigrated = 2,

        /// <summary>
        /// Device has been assigned to a different IoT hub and its device data was populated from the initial state stored
        /// in the enrollment. Device data was removed from the previously assigned IoT hub.
        /// </summary>
        [EnumMember(Value = "deviceDataReset")]
        DeviceDataReset = 3,

        /// <summary>
        /// Device has been re-provisioned to a previously assigned IoT hub.
        /// </summary>
        [EnumMember(Value = "reprovisionedToInitialAssignment")]
        ReprovisionedToInitialAssignment = 4
    }
}
