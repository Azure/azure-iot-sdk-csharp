//
// Copyright (C) Microsoft.  All rights reserved.
//

using System;

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    /// <summary>
    /// Data class for Azure Hardware Center Metadata
    /// </summary>
    public class AzureHardwareCenterMetadata
    {
        /// <summary>
        /// Product Family defaulted to AzureStackHCI
        /// </summary>
        public string ProductFamily
        {
            get; set;
        }

        /// <summary>
        /// Serial Number of the node
        /// </summary>
        public string SerialNumber
        {
            get; set;
        }

        /// <summary>
        /// Appliance ID required for Billing and Identity <see cref="ApplianceID"/>
        /// </summary>
        public ApplianceID ApplianceID
        {
            get; set;
        }

        /// <summary>
        /// TPM EK Public Key
        /// </summary>
        public string EndorsementKeyPublic
        {
            get; set;
        }

        /// <summary>
        /// Configuration Id ProductFamily_SKU
        /// </summary>
        public string ConfigurationId
        {
            get; set;
        }

        /// <summary>
        /// Manufacturer
        /// </summary>
        public string Manufacturer
        {
            get; set;
        }

        /// <summary>
        /// Model
        /// </summary>
        public string Model
        {
            get; set;
        }

        /// <summary>
        /// Creation Time in UTC for artifacts
        /// </summary>
        public DateTime CreateTimeUTC
        {
            get; set;
        }

        /// <summary>
        /// Version of the Metadata
        /// </summary>
        public string Version
        {
            get; set;
        }

        /// <summary>
        /// Version of the Metadata
        /// </summary>
        public string Kind
        {
            get; set;
        }
    }
}
