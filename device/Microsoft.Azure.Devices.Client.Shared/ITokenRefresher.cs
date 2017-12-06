// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to get a new token for the device or module
    /// </summary>
    interface ITokenRefresher
    {
        /// <summary>
        /// Gets a snapshot of the UTC token expiry time.
        /// </summary>
        DateTime ExpiresOn { get; }

        /// <summary>
        /// Gets a snapshot of the UTC token refresh time.
        /// </summary>
        DateTime RefreshesOn { get; }

        /// <summary>
        /// Gets a snapshot expiry state.
        /// </summary>
        bool IsExpiring { get; }

        /// <summary>
        /// Returns a snapshot of the security token associated with the device or module.
        /// </summary>
        Task<string> GetTokenAsync(string iotHub);
    }
}