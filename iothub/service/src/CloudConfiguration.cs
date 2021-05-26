// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The cloud in which IoT hub is hosted.
    /// </summary>
    public enum CloudConfiguration
    {
        /// <summary>
        /// US Government cloud
        /// </summary>
        FairFax,

        /// <summary>
        /// All other clouds including public cloud
        /// </summary>
        Others = 0
    }
}
