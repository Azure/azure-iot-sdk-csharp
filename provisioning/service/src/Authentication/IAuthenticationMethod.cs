﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Service.Auth
{
    /// <summary>
    /// Authentication interface to use for IoTHub communications.
    /// </summary>
    internal interface IAuthenticationMethod
    {
        ServiceConnectionStringBuilder Populate(ServiceConnectionStringBuilder provisioningConnectionStringBuilder);
    }
}
