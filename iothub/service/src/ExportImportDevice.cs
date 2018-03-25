// ---------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------

namespace Microsoft.Azure.Devices
{
    using System;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;

    /// <summary>
    ///  contains device properties specified during export/import operation
    /// </summary>
    public sealed class ExportImportDevice
    {
        string eTag;
        string twinETag;

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
            this.Id = device.Id;
            this.eTag = this.SanitizeETag(device.ETag);
            this.ImportMode = importmode;
            this.Status = device.Status;
            this.StatusReason = device.StatusReason;
            this.Authentication = device.Authentication;
        }

        /// <summary>
        /// Id of the device
        /// </summary>
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id { get; set; }

#if ENABLE_MODULES_SDK
        /// <summary>
        /// Module Id for the object
        /// </summary>
        [JsonProperty(PropertyName = "moduleId", NullValueHandling = NullValueHandling.Ignore)]
        public string ModuleId { get; set; }
#endif
        /// <summary>
        /// ETag of the device
        /// </summary>
        [JsonProperty(PropertyName = "eTag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag {
            get
            {
                return this.eTag;
            }
            set
            {
                this.eTag = this.SanitizeETag(value);
            }
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
            get { return this.twinETag; }
            set { this.twinETag = this.SanitizeETag(value); }
        }

        [JsonProperty(PropertyName = "tags", NullValueHandling = NullValueHandling.Ignore)]
        public TwinCollection Tags { get; set; }

        [JsonProperty(PropertyName = "properties", NullValueHandling = NullValueHandling.Ignore)]
        public PropertyContainer Properties { get; set; }

        public sealed class PropertyContainer
        {
            [JsonProperty(PropertyName = "desired", NullValueHandling = NullValueHandling.Ignore)]
            public TwinCollection DesiredProperties { get; set; }

            [JsonProperty(PropertyName = "reported", NullValueHandling = NullValueHandling.Ignore)]
            public TwinCollection ReportedProperties { get; set; }
        }

        string SanitizeETag(string eTag)
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