// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ---------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains device properties specified during export/import job operation.
    /// </summary>
    public sealed class ExportImportDevice
    {
        private string _eTag;
        private string _twinETag;

        /// <summary>
        /// Type definition for the <see cref="Properties"/> property.
        /// </summary>
        public sealed class PropertyContainer
        {
            /// <summary>
            /// Desired properties are requested updates by a service client.
            /// </summary>
            [JsonProperty(PropertyName = "desired", NullValueHandling = NullValueHandling.Ignore)]
            public TwinCollection DesiredProperties { get; set; }

            /// <summary>
            /// Reported properties are the latest value reported by the device.
            /// </summary>
            [JsonProperty(PropertyName = "reported", NullValueHandling = NullValueHandling.Ignore)]
            public TwinCollection ReportedProperties { get; set; }
        }

        /// <summary>
        /// Create an ExportImportDevice.
        /// </summary>
        public ExportImportDevice()
        {
        }

        /// <summary>
        /// Create an ExportImportDevice.
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
            _eTag = SanitizeETag(device.ETag);
            ImportMode = importmode;
            Status = device.Status;
            StatusReason = device.StatusReason;
            Authentication = device.Authentication;
            Capabilities = device.Capabilities;
            DeviceScope = device.Scope;
            ParentScopes = device.ParentScopes;
        }

        /// <summary>
        /// Id of the device.
        /// </summary>
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id { get; set; }

        /// <summary>
        /// Module Id for the object.
        /// </summary>
        [JsonProperty(PropertyName = "moduleId", NullValueHandling = NullValueHandling.Ignore)]
        public string ModuleId { get; set; }

        /// <summary>
        /// A string representing an ETag for the entity as per RFC7232.
        /// </summary>
        [JsonProperty(PropertyName = "eTag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag
        {
            get => _eTag;
            set => _eTag = SanitizeETag(value);
        }

        /// <summary>
        /// Import mode of the device.
        /// </summary>
        [JsonProperty(PropertyName = "importMode", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public ImportMode ImportMode { get; set; }

        /// <summary>
        /// Status of the device.
        /// </summary>
        [JsonProperty(PropertyName = "status", Required = Required.Always)]
        public DeviceStatus Status { get; set; }

        /// <summary>
        /// Status reason of the device.
        /// </summary>
        [JsonProperty(PropertyName = "statusReason", NullValueHandling = NullValueHandling.Ignore)]
        public string StatusReason { get; set; }

        /// <summary>
        /// Authentication mechanism of the device.
        /// </summary>
        [JsonProperty(PropertyName = "authentication")]
        public AuthenticationMechanism Authentication { get; set; }

        /// <summary>
        /// String representing a Twin ETag for the entity, as per RFC7232.
        /// </summary>
        [JsonProperty(PropertyName = "twinETag", NullValueHandling = NullValueHandling.Ignore)]
        public string TwinETag
        {
            get => _twinETag;
            set => _twinETag = SanitizeETag(value);
        }

        /// <summary>
        /// Tags representing a collection of properties.
        /// </summary>
        [JsonProperty(PropertyName = "tags", NullValueHandling = NullValueHandling.Ignore)]
        public TwinCollection Tags { get; set; }

        /// <summary>
        /// Desired and reported property bags
        /// </summary>
        [JsonProperty(PropertyName = "properties", NullValueHandling = NullValueHandling.Ignore)]
        public PropertyContainer Properties { get; set; }

        /// <summary>
        /// Status of capabilities enabled on the device
        /// </summary>
        [JsonProperty(PropertyName = "capabilities", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceCapabilities Capabilities { get; set; }

        /// <summary>
        /// The scope of the device. For edge devices, this is auto-generated and immutable. For leaf devices, set this to create child/parent
        /// relationship.
        /// </summary>
        /// <remarks>
        /// For leaf devices, the value to set a parent edge device can be retrieved from the parent edge device's device scope property.
        ///
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2020-11#parent-and-child-relationships"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "deviceScope", NullValueHandling = NullValueHandling.Include)]
        public string DeviceScope { get; set; }

        /// <summary>
        /// The scopes of the upper level edge devices if applicable.
        /// </summary>
        /// <remarks>
        /// For edge devices, the value to set a parent edge device can be retrieved from the parent edge device's <see cref="DeviceScope"/> property.
        ///
        /// For leaf devices, this could be set to the same value as <see cref="DeviceScope"/> or left for the service to copy over.
        ///
        /// For now, this list can only have 1 element in the collection.
        ///
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2020-11#parent-and-child-relationships"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "parentScopes", NullValueHandling = NullValueHandling.Ignore)]
        public IList<string> ParentScopes { get; internal set; } = new List<string>();

        private static string SanitizeETag(string eTag)
        {
            if (!string.IsNullOrWhiteSpace(eTag))
            {
                string localETag = eTag;

                if (eTag.StartsWith("\"", StringComparison.OrdinalIgnoreCase))
                {
                    // remove only the first char
                    localETag = eTag.Substring(1);
                }

                if (localETag.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
                {
                    // remove only the last char
                    localETag = localETag.Remove(localETag.Length - 1);
                }

                return localETag;
            }
            else
            {
                // In case it is empty or contains whitespace
                return eTag;
            }
        }
    }
}
