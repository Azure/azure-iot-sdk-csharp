// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Azure;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains device properties specified during export/import job operation.
    /// </summary>
    public sealed class ExportImportDevice
    {
        /// <summary>
        /// Create an instance of this class.
        /// </summary>
        public ExportImportDevice()
        {
        }

        /// <summary>
        /// Create an instance of this class.
        /// </summary>
        /// <param name="device">Device properties</param>
        /// <param name="importmode">Identifies the behavior when merging a device to the registry during import actions.</param>
        public ExportImportDevice(Device device, ImportMode importmode)
        {
            Argument.AssertNotNull(device, nameof(device));

            Id = device.Id;
            ETag = device.ETag;
            ImportMode = importmode;
            Status = device.Status;
            StatusReason = device.StatusReason;
            Authentication = device.Authentication;
            Capabilities = device.Capabilities;
            DeviceScope = device.Scope;
        }

        /// <summary>
        /// The unique identifier of the device.
        /// </summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }

        /// <summary>
        /// The unique identifier of the module, if applicable.
        /// </summary>
        [JsonProperty("moduleId", NullValueHandling = NullValueHandling.Ignore)]
        public string ModuleId { get; set; }

        /// <summary>
        /// A string representing an ETag for the entity as per RFC7232.
        /// </summary>
        /// <remarks>
        /// The value is only used if import mode is updateIfMatchETag, in that case the import operation is performed
        /// only if this ETag matches the value maintained by the server.
        /// </remarks>
        [JsonProperty("eTag", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))]
        public ETag ETag { get; set; }

        /// <summary>
        /// The type of registry operation and ETag preferences.
        /// </summary>
        [JsonProperty("importMode", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public ImportMode ImportMode { get; set; }

        /// <summary>
        /// The status of the device or module.
        /// </summary>
        /// <remarks>
        /// If disabled, it cannot connect to the service.
        /// </remarks>
        [JsonProperty("status", Required = Required.Always)]
        public ClientStatus Status { get; set; }

        /// <summary>
        /// The 128 character-long string that stores the reason for the device identity status.
        /// </summary>
        /// <remarks>
        /// All UTF-8 characters are allowed.
        /// </remarks>
        [JsonProperty("statusReason", NullValueHandling = NullValueHandling.Ignore)]
        public string StatusReason { get; set; }

        /// <summary>
        /// The authentication mechanism used by the module.
        /// </summary>
        /// <remarks>
        /// This parameter is optional and defaults to SAS if not provided. In that case, primary/secondary
        /// access keys are auto-generated.
        /// </remarks>
        [JsonProperty("authentication")]
        public AuthenticationMechanism Authentication { get; set; } = new();

        /// <summary>
        /// String representing a Twin ETag for the entity, as per RFC7232.
        /// </summary>
        /// <remarks>
        /// The value is only used if import mode is updateIfMatchETag, in that case the import operation is
        /// performed only if this ETag matches the value maintained by the server.
        /// </remarks>
        [JsonProperty("twinETag", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))]
        public ETag TwinETag { get; set; }

        /// <summary>
        /// The JSON document read and written by the solution back end. The tags are not visible to device apps.
        /// </summary>
        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> Tags { get; internal set; } = new Dictionary<string, object>();

        /// <summary>
        /// The desired and reported properties for the device or module.
        /// </summary>
        [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
        public PropertyContainer Properties { get; set; } = new();

        /// <summary>
        /// Status of capabilities enabled on the device or module.
        /// </summary>
        [JsonProperty("capabilities", NullValueHandling = NullValueHandling.Ignore)]
        public ClientCapabilities Capabilities { get; set; } = new();

        /// <summary>
        /// The scope of the device. For edge devices, this is auto-generated and immutable. For leaf
        /// devices, set this to create child/parent relationship.
        /// </summary>
        /// <remarks>
        /// For leaf devices, the value to set a parent edge device can be retrieved from the parent
        /// edge device's device scope property.
        /// For more information, see
        /// <see href="https://docs.microsoft.com/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2020-11#parent-and-child-relationships"/>.
        /// </remarks>
        [JsonProperty("deviceScope", NullValueHandling = NullValueHandling.Include)]
        public string DeviceScope { get; set; }
    }
}
