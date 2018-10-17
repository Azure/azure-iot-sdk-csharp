// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Service.Auth
{
    /// <summary>
    /// Authentication interface to use for IoTHub communications.
    /// </summary>
    public interface IAuthenticationMethod
    {
        /// <summary>
        /// Populates the Provisioning Service connection string builder object
        /// </summary>
        /// <param name="provisioningConnectionStringBuilder"> The Provisioning Service String Builder object </param>
        /// <returns></returns>
        ServiceConnectionStringBuilder Populate(ServiceConnectionStringBuilder provisioningConnectionStringBuilder);
    }
}
