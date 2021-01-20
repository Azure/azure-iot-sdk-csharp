// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Used to provide storage parameters when making an export or import request.
    /// </summary>
    public sealed class ExportImportRequest
    {
        /// <summary>
        /// Storage account connection string.
        /// </summary>
        public string StorageConnectionString { get; set; }

        /// <summary>
        /// Container name in storage account to use for the export or import jobs.
        /// </summary>
        public string ContainerName { get; set; }
    }
}
