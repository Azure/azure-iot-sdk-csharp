// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Options that allow configuration of the token credential during initialization.
    /// </summary>
    public class TokenCredentialOptions
    {
        /// <summary>
        /// The cloud in which IoT hub is hosted.
        /// </summary>
        public CloudConfiguration cloudConfiguraion { get; set; } = CloudConfiguration.Others;
    }
}
