// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Edge
{
    internal interface ITrustBundleProvider
    {
        Task SetupTrustBundle(Uri providerUri, string apiVersion, ITransportSettings[] transportSettings);

        void SetupTrustBundle(string filePath, ITransportSettings[] transportSettings);
    }
}
