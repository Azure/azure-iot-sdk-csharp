// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using Microsoft.Azure.Devices.Client.Edge;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests.Edge
{
    [TestClass]
    [TestCategory("Unit")]
    public class CustomCertificateValidatorTest
    {
        private const string certificatesString =
            "-----BEGIN CERTIFICATE-----\n" +
            "MIIBiDCCAS2gAwIBAgIFWks8LR4wCgYIKoZIzj0EAwIwNjEUMBIGA1UEAwwLcmlv\n" +
            "dGNvcmVuZXcxETAPBgNVBAoMCE1TUl9URVNUMQswCQYDVQQGEwJVUzAgFw0xNzAx\n" +
            "MDEwMDAwMDBaGA8zNzAxMDEzMTIzNTk1OVowNjEUMBIGA1UEAwwLcmlvdGNvcmVu\n" +
            "ZXcxETAPBgNVBAoMCE1TUl9URVNUMQswCQYDVQQGEwJVUzBZMBMGByqGSM49AgEG\n" +
            "CCqGSM49AwEHA0IABLVS6bK+QMm+HZ0247Nm+JmnERuickBXTj6rydcP3WzVQNBN\n" +
            "pvcQ/4YVrPp60oiYRxZbsPyBtHt2UCAC00vEXy+jJjAkMA4GA1UdDwEB/wQEAwIH\n" +
            "gDASBgNVHRMBAf8ECDAGAQH/AgECMAoGCCqGSM49BAMCA0kAMEYCIQDEjs2PoZEi\n" +
            "/yAQNj2Vji9RthQ33HG/QdL12b1ABU5UXgIhAPJujG/c/S+7vcREWI7bQcCb31JI\n" +
            "BDhWZbt4eyCvXZtZ\n" +
            "-----END CERTIFICATE-----\n";

        [TestMethod]
        public void TestSetupCertificateValidation_Mqtt_ShouldSucceed()
        {
            ITransportSettings[] transportSettings = { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) };
            var certs = TrustBundleProvider.ParseCertificates(certificatesString);
            var customCertificateValidator = CustomCertificateValidator.Create(certs, transportSettings);

            Assert.IsNotNull(((MqttTransportSettings)transportSettings[0]).RemoteCertificateValidationCallback);
        }

        [TestMethod]
        public void TestSetupCertificateValidation_Amqp_ShouldSucceed()
        {
            ITransportSettings[] transportSettings = new ITransportSettings[] { new AmqpTransportSettings(TransportType.Amqp_Tcp_Only) };
            var certs = TrustBundleProvider.ParseCertificates(certificatesString);
            var customCertificateValidator = CustomCertificateValidator.Create(certs, transportSettings);

            Assert.IsNotNull(((AmqpTransportSettings)transportSettings[0]).RemoteCertificateValidationCallback);
        }

        [TestMethod]
        public void TestSetupCertificateValidation_Mqtt_CallbackAlreadySet_ShouldSucceed()
        {
            var certs = TrustBundleProvider.ParseCertificates(certificatesString);

            var setting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            RemoteCertificateValidationCallback callback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    return true;
                };
            setting.RemoteCertificateValidationCallback = callback;
            ITransportSettings[] transportSettings = new ITransportSettings[] { setting };
            var customCertificateValidator = CustomCertificateValidator.Create(certs, transportSettings);

            Assert.AreEqual(setting.RemoteCertificateValidationCallback, callback);
        }

        [TestMethod]
        public void TestSetupCertificateValidation_Amqp_CallbackAlreadySet_ShouldSucceed()
        {
            var certs = TrustBundleProvider.ParseCertificates(certificatesString);

            var setting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            RemoteCertificateValidationCallback callback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                return true;
            };
            setting.RemoteCertificateValidationCallback = callback;
            ITransportSettings[] transportSettings = new ITransportSettings[] { setting };
            var customCertificateValidator = CustomCertificateValidator.Create(certs, transportSettings);

            Assert.IsNotNull(((AmqpTransportSettings)transportSettings[0]).RemoteCertificateValidationCallback);
        }
    }
}
