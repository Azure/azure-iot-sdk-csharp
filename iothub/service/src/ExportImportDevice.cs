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
    ///  contains device properties specified during export/import operation
    /// </summary>
    public sealed class ExportImportDevice
    {
        private string _eTag;
        private string _twinETag;

        /// <summary>
        /// making default ctor public
        /// </summary>
        public ExportImportDevice()
        {
        }

        /// <summary>
        /// ctor which takes a Device object along with import mode
        /// </summary>
        /// <param name="device"></param>
        /// <param name="importmode"></param>
        public ExportImportDevice(Device device, ImportMode importmode)
        {
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
        /// ETag of the device
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

        [JsonProperty(PropertyName = "twinETag", NullValueHandling = NullValueHandling.Ignore)]
        public string TwinETag
        {
            get => _twinETag;
            set => _twinETag = SanitizeETag(value);
        }

        [JsonProperty(PropertyName = "tags", NullValueHandling = NullValueHandling.Ignore)]
        public TwinCollection Tags { get; set; }

        [JsonProperty(PropertyName = "properties", NullValueHandling = NullValueHandling.Ignore)]
        public PropertyContainer Properties { get; set; }

        [JsonProperty(PropertyName = "capabilities", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceCapabilities Capabilities { get; set; }

        public sealed class PropertyContainer
        {
            [JsonProperty(PropertyName = "desired", NullValueHandling = NullValueHandling.Ignore)]
            public TwinCollection DesiredProperties { get; set; }

            [JsonProperty(PropertyName = "reported", NullValueHandling = NullValueHandling.Ignore)]
            public TwinCollection ReportedProperties { get; set; }
        }

        private string SanitizeETag(string eTag)
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
                // in case it is empty or contains whitespace
                return eTag;
            }
        }
    }
}
