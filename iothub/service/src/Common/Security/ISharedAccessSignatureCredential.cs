// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Common.Data;

namespace Microsoft.Azure.Devices.Common.Security
{
    /// <summary>
    /// Credential interface used for authentication and authorization.
    /// </summary>
    public interface ISharedAccessSignatureCredential
    {
        /// <summary>
        /// Indicates if the token has expired.
        /// </summary>
        bool IsExpired();

        /// <summary>
        /// The date and time of expiration.
        /// </summary>
        DateTime ExpiryTime();

        /// <summary>
        /// Authenticate against the IoT Hub using an authorization rule.
        /// </summary>
        /// <param name="sasAuthorizationRule">The properties that describe the keys to access the IotHub artifacts.</param>
        void Authenticate(SharedAccessSignatureAuthorizationRule sasAuthorizationRule);

        /// <summary>
        /// Authorize access to the IoT Hub.
        /// </summary>
        /// <param name="hostName">IoT Hub host to authorize against.</param>
        void Authorize(string hostName);

        /// <summary>
        /// Authorize access to the provided target address.
        /// </summary>
        /// <param name="targetAddress">Target address to authorize against.</param>
        void Authorize(Uri targetAddress);
    }
}
