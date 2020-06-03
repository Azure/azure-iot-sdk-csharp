// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Options that allow configuration of the device client instance during initialization.
    /// </summary>
    public class ClientOptions
    {
        /// <summary>
        /// The digital twins model Id associated with the device client instance.
        /// </summary>
        public string ModelId { get; set; }
    }
}
