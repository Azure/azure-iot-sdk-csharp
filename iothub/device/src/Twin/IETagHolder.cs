// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// ETag Holder Interface
    /// </summary>
    public interface IETagHolder
    {
        /// <summary>
        /// ETag value
        /// </summary>
        string ETag { get; set; }
    }
}
