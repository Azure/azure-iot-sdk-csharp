//
// Copyright (C) Microsoft.  All rights reserved.
//

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    /// <summary>
    /// Wrapper Class to represent Azure Hardware Center Metadata from Partner
    /// </summary>
    public class AzureHardwareCenterMetadataWrapper
    {
        /// <summary>
        /// Partner Metadata
        /// </summary>
        public AzureHardwareCenterMetadata PartnerMetadata
        {
            get; set;
        }

        /// <summary>
        /// c'tor <see cref="AzureHardwareCenterMetadataWrapper"/> class.
        /// </summary>
        public AzureHardwareCenterMetadataWrapper(AzureHardwareCenterMetadata metadata)
        {
            PartnerMetadata = metadata;
        }
    }
}
