﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Metadata for properties in a <see cref="TwinCollection"/>.
    /// </summary>
    public sealed class Metadata
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="lastUpdated">When the property was last updated.</param>
        /// <param name="lastUpdatedVersion">The version of the property when updated.</param>
        public Metadata(DateTime lastUpdated, long? lastUpdatedVersion)
        {
            LastUpdated = lastUpdated;
            LastUpdatedVersion = lastUpdatedVersion;
        }

        /// <summary>
        /// When a property was last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <remarks>
        /// This should be null for Reported properties metadata and must not be null for Desired properties metadata.
        /// </remarks>
        public long? LastUpdatedVersion { get; set; }
    }
}