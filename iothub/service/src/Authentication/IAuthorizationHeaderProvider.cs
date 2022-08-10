// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Gets the authorization header for authenticated requests, regardless of choice of authentication.
    /// </summary>
    internal interface IAuthorizationHeaderProvider
    {
        string GetAuthorizationHeader();
    }
}
