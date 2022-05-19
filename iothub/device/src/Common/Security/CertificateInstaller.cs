// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Helper to install certificates to the cert store.
    /// </summary>
    internal static class CertificateInstaller
    {
        private static readonly object s_certOperationsLock = new object();

        /// <summary>
        /// Ensures the specified certs (presumably in a chain) are installed in the cert store.
        /// </summary>
        /// <remarks>
        /// Because Intermediate Authorities may have been issued by the uploaded CA, the application must present the full chain of
        /// certificates from the one used during authentication to the one uploaded to the service.
        /// for more information.
        /// </remarks>
        /// <param name="certificates">The certificate chain to ensure is installed.</param>
        internal static void EnsureChainIsInstalled(X509Certificate2Collection certificates)
        {
            // Certificate install on Windows is a multi-step process, and in the case that someone might have more than one
            // Device client we'll want to ensure these actions (get certs, install certs) are atomic.
            lock (s_certOperationsLock)
            {
                var store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);

                store.Open(OpenFlags.ReadWrite);

                try
                {
                    if (certificates != null)
                    {
                        foreach (X509Certificate2 certificate in certificates)
                        {
                            X509Certificate2Collection results = store.Certificates.Find(
                                X509FindType.FindByThumbprint,
                                certificate.Thumbprint,
                                false);
                            if (results.Count == 0)
                            {
                                if (Logging.IsEnabled) Logging.Info(null, $"{nameof(CertificateInstaller)} adding cert with thumbprint {certificate.Thumbprint} to X509 store.");

                                store.Add(certificate);
                            }
                        }
                    }
                }
                finally
                {
                    store?.Dispose();
                }
            }
        }
    }
}
