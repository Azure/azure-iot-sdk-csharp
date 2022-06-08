// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// <see cref="Metadata"/> for properties in <see cref="TwinCollection"/>
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1724:TypeNamesShouldNotMatchNamespaces",
        Justification = "Public API cannot change name.")]
    public sealed class Metadata
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Metadata"/>
        /// </summary>
        /// <param name="lastUpdated"></param>
        /// <param name="lastUpdatedVersion"></param>
        public Metadata(DateTime lastUpdated, long? lastUpdatedVersion)
        {
            LastUpdated = lastUpdated;
            LastUpdatedVersion = lastUpdatedVersion;
        }

        /// <summary>
        /// Time when a property was last updated
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <remarks>
        /// This SHOULD be null for Reported properties metadata and MUST not be null for Desired properties metadata.
        /// </remarks>
        public long? LastUpdatedVersion { get; set; }
    }
}