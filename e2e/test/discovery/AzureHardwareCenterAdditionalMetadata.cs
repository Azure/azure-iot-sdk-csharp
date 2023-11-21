//
// Copyright (C) Microsoft.  All rights reserved.
//

using System.Collections.Generic;

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    /// <summary>
    /// Data class for Azure Hardware Center Additional Metadata
    /// </summary>
    public class AzureHardwareCenterAdditionalMetadata
    {
        /// <summary>
        /// Version
        /// </summary>
        public string Version
        {
            get; set;
        }

        /// <summary>
        /// Kind of Additional Metadata
        /// </summary>
        public string Kind
        {
            get; set;
        }

        /// <summary>
        /// Additional properties <see cref="AdditionalProperties"/>
        /// </summary>
        public AdditionalProperties Properties
        {
            get; set;
        }
    }

    /// <summary>
    /// Data class for Additonal Metadata properies like HardwareProfile
    /// </summary>
    public class AdditionalProperties
    {
        /// <summary>
        /// Hardware Profile which contains a list of HardwareComponents
        /// </summary>
        public List<string> HardwareMetadataProfile
        {
            get; set;
        }

        /// <summary>
        /// c'tor <see cref="AdditionalProperties"/> class.
        /// </summary>
        public AdditionalProperties(List<string> hardwareComponents)
        {
            HardwareMetadataProfile = hardwareComponents;
        }
    }
}
