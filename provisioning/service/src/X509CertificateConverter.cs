// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class X509CertificateConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            X509Certificate2 certificate = value as X509Certificate2;
            if (certificate == null)
            {
                writer.WriteNull();
                return;
            }
            ValidateCertificate(certificate);

            var result = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
            serializer.Serialize(writer, result);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string value = reader.Value as string;
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            var certificate = new X509Certificate2(Convert.FromBase64String(value));
            ValidateCertificate(certificate);

            return certificate;
        }

        public override bool CanConvert(Type objectType)
        {
            bool result = objectType == typeof(X509Certificate2);
            return result;
        }

        static void ValidateCertificate(X509Certificate2 certificate)
        {
            if (certificate.HasPrivateKey)
            {
                throw new InvalidOperationException("Certificate should not contain a private key.");
            }
        }
    }
}
