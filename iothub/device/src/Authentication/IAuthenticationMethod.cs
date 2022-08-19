// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication interface to use for device communications.
    /// </summary>
    public interface IAuthenticationMethod
    {
        /// <summary>
        /// Populates an <see cref="IotHubConnectionCredentials"/> instance based on the properties of the current instance.
        /// </summary>
        /// <param name="iotHubConnectionStringBuilder">Instance to populate.</param>
        /// <returns>The populated <see cref="IotHubConnectionCredentials"/> instance.</returns>
        IotHubConnectionCredentials Populate(IotHubConnectionCredentials iotHubConnectionStringBuilder);
    }
}
