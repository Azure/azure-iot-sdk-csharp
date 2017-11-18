// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Common.Service.Auth
{
    internal interface ISharedAccessSignatureCredential
    {
        bool IsExpired();

        DateTime ExpiryTime();

        void Authenticate(SharedAccessSignatureAuthorizationRule sasAuthorizationRule);

        void Authorize(string hostName);

        void Authorize(Uri targetAddress);
    }
}
