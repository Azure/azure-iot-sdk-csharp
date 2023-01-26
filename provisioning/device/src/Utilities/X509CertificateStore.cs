// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Client.Utilities
{
    internal class X509CertificateStore : ICertificiateStore, IDisposable
    {
        private readonly X509Store _store;

        public X509CertificateStore()
        {
            _store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
            _store.Open(OpenFlags.ReadWrite);
        }

        bool ICertificiateStore.Find(X509Certificate2 certificate)
        {
            X509Certificate2Collection results = _store.Certificates.Find(
                X509FindType.FindByThumbprint,
                certificate.Thumbprint,
                false);
            return results.Count > 0;
        }

        void ICertificiateStore.Add(X509Certificate2 certificate)
        {
            _store.Add(certificate);
        }

        public void Dispose()
        {
            _store?.Dispose();
        }
    }
}
