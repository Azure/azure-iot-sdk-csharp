// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Authentication interface to use for IoT hub communications.
    /// </summary>
    internal interface IAuthenticationMethod
    {
        ServiceConnectionStringBuilder Populate(ServiceConnectionStringBuilder provisioningConnectionStringBuilder);
    }
}
