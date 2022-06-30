// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Authentication interface to use for IoT hub communications.
    /// </summary>
    public interface IAuthenticationMethod
    {
        /// <summary>
        /// Populates the necessary data in the builder.
        /// </summary>
        /// <param name="iotHubConnectionStringBuilder">The builder object to populate.</param>
        /// <returns>Populated builder object.</returns>
        IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder);
    }
}
