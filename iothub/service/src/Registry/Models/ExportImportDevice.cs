// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Azure;
using Azure.Core.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains device properties specified during export/import job operation.
    /// </summary>
    public sealed class ExportImportDevice
    {
        /// <summary>
        /// The desired and reported properties of the twin.
        /// </summary>
        /// <remarks>
        /// Type definition for the <see cref="Properties"/> property.
        /// The maximum depth of the object is 10.
        /// </remarks>
        public sealed class PropertyContainer
        {
            /// <summary>
            /// The collection of desired property key-value pairs.
            /// </summary>
            /// <remarks>
            /// The keys are UTF-8 encoded, case-sensitive and up-to 1KB in length. Allowed characters
            /// exclude UNICODE control characters (segments C0 and C1), '.', '$' and space. The
            /// desired porperty values are JSON objects, up-to 4KB in length.
            /// </remarks>
            [JsonProperty(PropertyName = "desired", NullValueHandling = NullValueHandling.Ignore)]
            public ClientTwinProperties DesiredProperties { get; set; }

            /// <summary>
            /// The collection of reported property key-value pairs.
            /// </summary>
            /// <remarks>
            /// The keys are UTF-8 encoded, case-sensitive and up-to 1KB in length. Allowed characters
            /// exclude UNICODE control characters (segments C0 and C1), '.', '$' and space. The
            /// reported property values are JSON objects, up-to 4KB in length.
            /// </remarks>
            [JsonProperty(PropertyName = "reported", NullValueHandling = NullValueHandling.Ignore)]
            public ClientTwinProperties ReportedProperties { get; set; }
        }

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
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

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
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id { get; set; }

        /// <summary>
        /// The unique identifier of the module, if applicable.
        /// </summary>
        [JsonProperty(PropertyName = "moduleId", NullValueHandling = NullValueHandling.Ignore)]
        public string ModuleId { get; set; }

        /// <summary>
        /// A string representing an ETag for the entity as per RFC7232.
        /// </summary>
        /// <remarks>
        ///  The value is only used if import mode is updateIfMatchETag, in that case the import operation is performed
        ///  only if this ETag matches the value maintained by the server.
        /// </remarks>
        [JsonProperty(PropertyName = "eTag", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))]
        public ETag ETag { get; set; }

        /// <summary>
        /// The type of registry operation and ETag preferences.
        /// </summary>
        [JsonProperty(PropertyName = "importMode", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public ImportMode ImportMode { get; set; }

        /// <summary>
        /// The status of the device or module.
        /// </summary>
        /// <remarks>
        /// If disabled, it cannot connect to the service.
        /// </remarks>
        [JsonProperty(PropertyName = "status", Required = Required.Always)]
        public ClientStatus Status { get; set; }

        /// <summary>
        /// The 128 character-long string that stores the reason for the device identity status.
        /// </summary>
        /// <remarks>
        /// All UTF-8 characters are allowed.
        /// </remarks>
        [JsonProperty(PropertyName = "statusReason", NullValueHandling = NullValueHandling.Ignore)]
        public string StatusReason { get; set; }

        /// <summary>
        /// The authentication mechanism used by the module.
        /// </summary>
        /// <remarks>
        /// This parameter is optional and defaults to SAS if not provided. In that case, primary/secondary
        /// access keys are auto-generated.
        /// </remarks>
        [JsonProperty(PropertyName = "authentication")]
        public AuthenticationMechanism Authentication { get; set; }

        /// <summary>
        /// String representing a Twin ETag for the entity, as per RFC7232.
        /// </summary>
        /// <remarks>
        /// The value is only used if import mode is updateIfMatchETag, in that case the import operation is
        /// performed only if this ETag matches the value maintained by the server.
        /// </remarks>
        [JsonProperty(PropertyName = "twinETag", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))]
        public ETag TwinETag { get; set; }

        /// <summary>
        /// The JSON document read and written by the solution back end. The tags are not visible to device apps.
        /// </summary>
        [JsonProperty(PropertyName = "tags", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> Tags { get; internal set; } = new Dictionary<string, object>();

        /// <summary>
        /// The desired and reported properties for the device or module.
        /// </summary>
        [JsonProperty(PropertyName = "properties", NullValueHandling = NullValueHandling.Ignore)]
        public PropertyContainer Properties { get; set; }

        /// <summary>
        /// Status of capabilities enabled on the device or module.
        /// </summary>
        [JsonProperty(PropertyName = "capabilities", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceCapabilities Capabilities { get; set; }

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
        [JsonProperty(PropertyName = "deviceScope", NullValueHandling = NullValueHandling.Include)]
        public string DeviceScope { get; set; }
    }
}
