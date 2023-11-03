//
// Copyright (C) Microsoft.  All rights reserved.
//

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    /// <summary>
    /// Data class for Azure Hardare Center Artifacts
    /// </summary>
    public class AzureHardwareCenterArtifacts
    {
        /// <summary>
        /// Metadata from partner <see cref="Metadata"/>
        /// </summary>
        public AzureHardwareCenterMetadataWrapper Metadata
        {
            get; set;
        }

        /// <summary>
        /// Additional Metadata from partner
        /// </summary>
        public AzureHardwareCenterAdditionalMetadata PartnerAdditionalMetadata
        {
            get; set;
        }

    }
}
