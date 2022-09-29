// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents the interface for a Provisioning Transport Handler.
    /// </summary>
    internal abstract class ProvisioningTransportHandler
    {
        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        internal abstract Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterRequest message,
            CancellationToken cancellationToken);
    }
}
