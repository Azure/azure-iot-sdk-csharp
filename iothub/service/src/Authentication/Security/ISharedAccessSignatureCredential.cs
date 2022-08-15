// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Common.Security
{
    /// <summary>
    /// Credential interface used for authentication and authorization.
    /// </summary>
    internal interface ISharedAccessSignatureCredential
    {
        /// <summary>
        /// Indicates if the token has expired.
        /// </summary>
        bool IsExpired();

        /// <summary>
        /// The date and time of expiration.
        /// </summary>
        DateTime ExpiryTime();
    }
}
