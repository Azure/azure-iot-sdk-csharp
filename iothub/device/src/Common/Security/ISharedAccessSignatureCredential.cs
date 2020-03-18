// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    internal interface ISharedAccessSignatureCredential
    {
        bool IsExpired();

        void Authenticate(SharedAccessSignatureAuthorizationRule sasAuthorizationRule);

        void AuthorizeHost(string hostName);

        void AuthorizeTarget(Uri targetAddress);
    }
}
