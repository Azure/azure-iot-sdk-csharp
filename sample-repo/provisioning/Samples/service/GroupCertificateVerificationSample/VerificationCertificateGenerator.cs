// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class VerificationCertificateGenerator
    {
        public static X509Certificate2 GenerateSignedCertificate(X509Certificate2 signerCertificate, string commonName)
        {
            string subjectName = string.Concat("CN=", commonName);
            var name = new X509Name(subjectName);
            X509Name signerName = DotNetUtilities.FromX509Certificate(signerCertificate).SubjectDN;

            Pkcs12Store store = CreateTemporaryStore(signerCertificate);
            ECPrivateKeyParameters privateKey = GetPrivateKey(store);
            X509Certificate2 certificate = GenerateAndSignCertificate(name, signerName, privateKey);
            return certificate;
        }

        private static Pkcs12Store CreateTemporaryStore(X509Certificate2 signerCertificate)
        {
            Pkcs12Store store = new Pkcs12StoreBuilder().Build();

            var random = new SecureRandom();
            string sessionPassword = Convert.ToBase64String(SecureRandom.GetNextBytes(random, 32));

            using (var stream = new MemoryStream(signerCertificate.Export(X509ContentType.Pfx, sessionPassword)))
            {
                store.Load(stream, sessionPassword.ToCharArray());
            }

            return store;
        }

        private static ECPrivateKeyParameters GetPrivateKey(Pkcs12Store store)
        {
            ECPrivateKeyParameters privateParameter = null;

            foreach (string alias in store.Aliases)
            {
                var key = store.GetKey(alias);

                if (key != null && key.Key.IsPrivate)
                {
                    privateParameter = key.Key as ECPrivateKeyParameters;
                }
            }

            return privateParameter;
        }

        private static X509Certificate2 GenerateAndSignCertificate(
            X509Name subjectName,
            X509Name issuerName,
            AsymmetricKeyParameter privateSigningKey)
        {
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);
            var certGenerator = new X509V3CertificateGenerator();

            var keyGenerationParameters = new KeyGenerationParameters(random, 256);
            var keyPairGenerator = new ECKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            AsymmetricCipherKeyPair certKeyPair = keyPairGenerator.GenerateKeyPair();

            certGenerator.SetPublicKey(certKeyPair.Public);

            var serialNumber = new BigInteger(64, random);
            certGenerator.SetSerialNumber(serialNumber);

            certGenerator.SetSubjectDN(subjectName);
            certGenerator.SetIssuerDN(issuerName);

            DateTime notBefore = DateTime.UtcNow - TimeSpan.FromDays(3);
            DateTime notAfter = DateTime.UtcNow + TimeSpan.FromDays(3);
            certGenerator.SetNotBefore(notBefore);
            certGenerator.SetNotAfter(notAfter);

            certGenerator.AddExtension(
                X509Extensions.ExtendedKeyUsage, 
                true, 
                ExtendedKeyUsage.GetInstance(new DerSequence(KeyPurposeID.IdKPClientAuth)));

            ISignatureFactory signatureFactory = 
                new Asn1SignatureFactory("SHA256WITHECDSA", privateSigningKey, random);

            var certificate = certGenerator.Generate(signatureFactory);
            return new X509Certificate2(DotNetUtilities.ToX509Certificate(certificate));
        }
    }
}
