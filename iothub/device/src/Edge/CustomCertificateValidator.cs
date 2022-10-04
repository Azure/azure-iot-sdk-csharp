// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    internal class CustomCertificateValidator : ICertificateValidator, IDisposable
    {
        private readonly IEnumerable<X509Certificate2> _certs;
        private readonly IotHubClientTransportSettings _transportSettings;

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (X509Certificate2 item in _certs)
            {
                item.Dispose();
            }
        }

        private CustomCertificateValidator(IList<X509Certificate2> certs, IotHubClientTransportSettings transportSettings)
        {
            Debug.Assert(certs.Any(), $"No certs were sent to {nameof(CustomCertificateValidator)}");

            _certs = certs;
            _transportSettings = transportSettings;
        }

        internal static CustomCertificateValidator Create(IList<X509Certificate2> certs, IotHubClientTransportSettings transportSettings)
        {
            var instance = new CustomCertificateValidator(certs, transportSettings);
            instance.SetupCertificateValidation();
            return instance;
        }

        Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> ICertificateValidator.GetCustomCertificateValidation()
        {
            if (Logging.IsEnabled)
                Logging.Info(this, "CustomCertificateValidator.GetCustomCertificateValidation()", nameof(ICertificateValidator.GetCustomCertificateValidation));

            return (sender, cert, chain, sslPolicyErrors) =>
                ValidateCertificate(_certs.First(), cert, chain, sslPolicyErrors);
        }

        private void SetupCertificateValidation()
        {
            if (Logging.IsEnabled)
                Logging.Info(this, "CustomCertificateValidator.SetupCertificateValidation()", nameof(SetupCertificateValidation));

            if (_transportSettings is IotHubClientAmqpSettings amqpTransportSettings)
            {
                amqpTransportSettings.RemoteCertificateValidationCallback ??=
                    (sender, certificate, chain, sslPolicyErrors) => ValidateCertificate(_certs.First(), certificate, chain, sslPolicyErrors);
            }
            else if (_transportSettings is IotHubClientMqttSettings mqttTransportSettings)
            {
                mqttTransportSettings.RemoteCertificateValidationCallback ??=
                    (sender, certificate, chain, sslPolicyErrors) => ValidateCertificate(_certs.First(), certificate, chain, sslPolicyErrors);
            }
        }

        private bool ValidateCertificate(X509Certificate2 trustedCertificate, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // Terminate on errors other than those caused by a chain failure
            SslPolicyErrors terminatingErrors = sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors;
            if (terminatingErrors != SslPolicyErrors.None)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Discovered SSL session errors: {terminatingErrors}", nameof(ValidateCertificate));
                return false;
            }

            // Allow the chain the chance to rebuild itself with the expected root
            chain.ChainPolicy.ExtraStore.Add(trustedCertificate);
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            using var cert = new X509Certificate2(certificate);
            if (!chain.Build(cert))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, "Unable to build the chain using the expected root certificate.", nameof(ValidateCertificate));
                return false;
            }

            // Pin the trusted root of the chain to the expected root certificate
            X509Certificate2 actualRoot = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
            if (trustedCertificate != actualRoot)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, "The certificate chain was not signed by the trusted root certificate.", nameof(ValidateCertificate));
                return false;
            }

            return true;
        }
    }
}
