// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Client.Utilities
{
    internal interface ICertificiateStore
    {
        internal bool Find(X509Certificate2 certificate);
        internal void Add(X509Certificate2 certificate);
    }
}
