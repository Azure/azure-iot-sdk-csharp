// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Edge;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.Edge
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
            var transportSettings = new IotHubClientMqttSettings();
            var certs = TrustBundleProvider.ParseCertificates(certificatesString);
            using var customCertificateValidator = CustomCertificateValidator.Create(certs, transportSettings);

            transportSettings.RemoteCertificateValidationCallback.Should().NotBeNull();
        }

        [TestMethod]
        public void TestSetupCertificateValidation_Amqp_ShouldSucceed()
        {
            var transportSettings = new IotHubClientAmqpSettings();
            var certs = TrustBundleProvider.ParseCertificates(certificatesString);
            using var customCertificateValidator = CustomCertificateValidator.Create(certs, transportSettings);

            transportSettings.RemoteCertificateValidationCallback.Should().NotBeNull();
        }

        [TestMethod]
        public void TestSetupCertificateValidation_Mqtt_CallbackAlreadySet_ShouldSucceed()
        {
            var certs = TrustBundleProvider.ParseCertificates(certificatesString);

            RemoteCertificateValidationCallback callback = (sender, certificate, chain, sslPolicyErrors) => true;

            var setting = new IotHubClientMqttSettings
            {
                RemoteCertificateValidationCallback = callback,
            };
            using var customCertificateValidator = CustomCertificateValidator.Create(certs, setting);

            setting.RemoteCertificateValidationCallback.Should().Be(callback);
        }

        [TestMethod]
        public void TestSetupCertificateValidation_Amqp_CallbackAlreadySet_ShouldSucceed()
        {
            var certs = TrustBundleProvider.ParseCertificates(certificatesString);

            RemoteCertificateValidationCallback callback = (sender, certificate, chain, sslPolicyErrors) => true;
            var setting = new IotHubClientAmqpSettings
            {
                RemoteCertificateValidationCallback = callback,
            };
            using var customCertificateValidator = CustomCertificateValidator.Create(certs, setting);

            setting.RemoteCertificateValidationCallback.Should().Be(callback);
        }
    }
}
