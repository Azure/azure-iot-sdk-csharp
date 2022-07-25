// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;

namespace Microsoft.Azure.Devices.Client.Edge
{
    internal class CustomCertificateValidator : ICertificateValidator
    {
        private readonly IEnumerable<X509Certificate2> _certs;
        private readonly ITransportSettings _transportSettings;

        private CustomCertificateValidator(IList<X509Certificate2> certs, ITransportSettings transportSettings)
        {
            _certs = certs;
            _transportSettings = transportSettings;
        }

        public static CustomCertificateValidator Create(IList<X509Certificate2> certs, ITransportSettings transportSettings)
        {
            var instance = new CustomCertificateValidator(certs, transportSettings);
            instance.SetupCertificateValidation();
            return instance;
        }

        public Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> GetCustomCertificateValidation()
        {
            Debug.WriteLine("CustomCertificateValidator.GetCustomCertificateValidation()");

            return (sender, cert, chain, sslPolicyErrors) =>
                ValidateCertificate(_certs.First(), cert, chain, sslPolicyErrors);
        }

        private void SetupCertificateValidation()
        {
            Debug.WriteLine("CustomCertificateValidator.SetupCertificateValidation()");

            if (_transportSettings is AmqpTransportSettings amqpTransportSettings)
            {
                if (amqpTransportSettings.RemoteCertificateValidationCallback == null)
                {
                    amqpTransportSettings.RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => ValidateCertificate(_certs.First(), certificate, chain, sslPolicyErrors);
                }
            }
            else if (_transportSettings is MqttTransportSettings mqttTransportSettings)
            {
                if (mqttTransportSettings.RemoteCertificateValidationCallback == null)
                {
                    mqttTransportSettings.RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => ValidateCertificate(_certs.First(), certificate, chain, sslPolicyErrors);
                }
            }

            // TODO: Tim/Abhipsa, what does this mean?

            // InvokeMethodAsync is over HTTP even when transportSettings set a different protocol
            // So set the callback in HttpClientHandler for InvokeMethodAsync
        }

        private static bool ValidateCertificate(X509Certificate2 trustedCertificate, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // Terminate on errors other than those caused by a chain failure
            SslPolicyErrors terminatingErrors = sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors;
            if (terminatingErrors != SslPolicyErrors.None)
            {
                Debug.WriteLine("Discovered SSL session errors: {0}", terminatingErrors);
                return false;
            }

            // Allow the chain the chance to rebuild itself with the expected root
            chain.ChainPolicy.ExtraStore.Add(trustedCertificate);
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            using var cert = new X509Certificate2(certificate);
            if (!chain.Build(cert))
            {
                Debug.WriteLine("Unable to build the chain using the expected root certificate.");
                return false;
            }

            // Pin the trusted root of the chain to the expected root certificate
            X509Certificate2 actualRoot = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
            if (!trustedCertificate.Equals(actualRoot))
            {
                Debug.WriteLine("The certificate chain was not signed by the trusted root certificate.");
                return false;
            }

            return true;
        }
    }
}
