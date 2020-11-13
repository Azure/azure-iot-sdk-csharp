// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ---------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------

using System;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains device properties specified during export/import operation
    /// </summary>
    public sealed class ExportImportDevice
    {
        private string _eTag;
        private string _twinETag;

        /// <summary>
        /// Property container
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Public property. No behavior changes allowed.")]
        public sealed class PropertyContainer
        {
            /// <summary>
            /// Desired properties
            /// </summary>
            [JsonProperty(PropertyName = "desired", NullValueHandling = NullValueHandling.Ignore)]
            public TwinCollection DesiredProperties { get; set; }

            /// <summary>
            /// Reported properties
            /// </summary>
            [JsonProperty(PropertyName = "reported", NullValueHandling = NullValueHandling.Ignore)]
            public TwinCollection ReportedProperties { get; set; }
        }

        /// <summary>
        /// Create an ExportImportDevice <see cref="ExportImportDevice" />
        /// </summary>
        public ExportImportDevice()
        {
        }

        /// <summary>
        /// Create an ExportImportDevice <see cref="ExportImportDevice" />
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
        }

        /// <summary>
        /// Id of the device
        /// </summary>
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id { get; set; }

        /// <summary>
        /// Module Id for the object
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
        /// ImportMode of the device
        /// </summary>
        [JsonProperty(PropertyName = "importMode", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public ImportMode ImportMode { get; set; }

        /// <summary>
        /// Status of the device
        /// </summary>
        [JsonProperty(PropertyName = "status", Required = Required.Always)]
        public DeviceStatus Status { get; set; }

        /// <summary>
        /// StatusReason of the device
        /// </summary>
        [JsonProperty(PropertyName = "statusReason", NullValueHandling = NullValueHandling.Ignore)]
        public string StatusReason { get; set; }

        /// <summary>
        /// AuthenticationMechanism of the device
        /// </summary>
        [JsonProperty(PropertyName = "authentication")]
        public AuthenticationMechanism Authentication { get; set; }

        /// <summary>
        /// string representing a Twin ETag for the entity, as per RFC7232.
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
        /// Desired and Reported property bags
        /// </summary>
        [JsonProperty(PropertyName = "properties", NullValueHandling = NullValueHandling.Ignore)]
        public PropertyContainer Properties { get; set; }

        /// <summary>
        /// Status of Capabilities enabled on the device
        /// </summary>
        [JsonProperty(PropertyName = "capabilities", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceCapabilities Capabilities { get; set; }

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
