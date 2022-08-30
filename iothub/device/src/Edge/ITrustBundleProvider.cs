// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    internal interface ITrustBundleProvider
    {
        Task<IList<X509Certificate2>> GetTrustBundleAsync(Uri providerUri, string defaultApiVersion);
    }
}
