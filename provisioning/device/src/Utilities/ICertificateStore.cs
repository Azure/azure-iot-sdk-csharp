// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Added for unit testing of certificate install operations.
    /// </summary>
    internal interface ICertificateStore
    {
        internal bool Contains(X509Certificate2 certificate);
        internal void Add(X509Certificate2 certificate);
    }
}
