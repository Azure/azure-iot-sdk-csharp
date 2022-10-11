﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication interface to use for device communications.
    /// </summary>
    public interface IAuthenticationMethod
    {
        /// <summary>
        /// Populates an <c>IotHubConnectionCredentials</c> instance based on the properties of the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        void Populate(ref IotHubConnectionCredentials iotHubConnectionCredentials);
    }
}
