// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    internal class InstalledCertificateValidator : ICertificateValidator
    {
        private readonly IList<X509Certificate2> _certs;

        private InstalledCertificateValidator(IList<X509Certificate2> certs)
        {
            _certs = certs;
        }

        public static InstalledCertificateValidator Create(IList<X509Certificate2> certs)
        {
            var instance = new InstalledCertificateValidator(certs);
            instance.SetupCertificateValidation();
            return instance;
        }

        public Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> GetCustomCertificateValidation()
        {
            return null;
        }

        private void SetupCertificateValidation()
        {
            Debug.WriteLine("InstalledCertificateValidator.SetupCertificateValidation()");
            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            try
            {
                foreach (X509Certificate2 cert in _certs)
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(cert);
                }
            }
            finally
            {
                store.Close();
            }
        }
    }
}