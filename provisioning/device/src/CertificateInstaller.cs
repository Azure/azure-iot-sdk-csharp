// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal static class CertificateInstaller
    {
        private static HashSet<string> _installedCertificates = new HashSet<string>();
        private static object _lock = new object();

        static CertificateInstaller()
        {
            try
            {
                using (var store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    foreach (X509Certificate2 certificate in store.Certificates)
                    {
                        _installedCertificates.Add(certificate.Thumbprint);
                    }
                }
            }
            catch (Exception ex)
            {
                if(Logging.IsEnabled) Logging.Error(
                    null, 
                    $"{nameof(CertificateInstaller)} failed to read store: {ex}.");
            }
        }

        public static void EnsureChainIsInstalled(X509Certificate2Collection certificates)
        {
            if (certificates == null) return;

            lock (_lock)
            {
                using (var store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadWrite);

                    foreach (X509Certificate2 certificate in certificates)
                    {
                        if (!_installedCertificates.Contains(certificate.Thumbprint))
                        {
                            if (Logging.IsEnabled)
                                Logging.Info(null, $"{nameof(CertificateInstaller)} adding {certificate.Thumbprint}");

                            store.Add(certificate);
                            _installedCertificates.Add(certificate.Thumbprint);
                        }
                    }
                }
            }
        }
    }
}
