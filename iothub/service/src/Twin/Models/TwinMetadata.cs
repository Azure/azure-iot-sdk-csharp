// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Metadata for properties in <see cref="TwinCollection"/>.
    /// </summary>
    public sealed class TwinMetadata
    {
        /// <summary>
        /// Initializes an instance of this class.
        /// </summary>
        /// <param name="lastUpdatedOn">When a property was last updated.</param>
        /// <param name="lastUpdatedVersion">The version of the property when last updated.</param>
        public TwinMetadata(DateTimeOffset lastUpdatedOn, long? lastUpdatedVersion)
        {
            LastUpdatedOn = lastUpdatedOn;
            LastUpdatedVersion = lastUpdatedVersion;
        }

        /// <summary>
        /// When a property was last updated.
        /// </summary>
        public DateTimeOffset LastUpdatedOn { get; set; }

        /// <summary>
        /// The version of the property when last updated.
        /// </summary>
        /// <remarks>
        /// This should be null for reported properties metadata and must not be null for desired properties metadata.
        /// </remarks>
        public long? LastUpdatedVersion { get; set; }
    }
}
