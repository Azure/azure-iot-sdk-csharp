// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.HsmAuthentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Client.Test.HsmAuthentication
{
    [TestClass]
    [TestCategory("Unit")]
    public class ModuleAuthenticationWithHsmTest
    {
        string signature = "signature";
        string deviceId = "device1";
        string moduleId = "module1";
        string generationId = "1";
        string iotHub = "iothub.test";

        [TestMethod]
        public async Task TestSafeCreateNewToken_ShouldReturnSasToken()
        {
            // Arrange
            var httpClient = new Mock<ISignatureProvider>();
            httpClient.Setup(p => p.SignAsync(moduleId, generationId, It.IsAny<string>())).Returns(Task.FromResult(signature));

            var moduleAuthenticationWithHsm = new ModuleAuthenticationWithHsm(httpClient.Object, deviceId, moduleId, generationId);

            // Act
            string audience = string.Format(CultureInfo.InvariantCulture, "{0}/devices/{1}/modules/{2}",
                iotHub,
                WebUtility.UrlEncode(deviceId),
                WebUtility.UrlEncode(moduleId));

            string sasToken = await moduleAuthenticationWithHsm.GetTokenAsync(audience);
            SharedAccessSignature token = SharedAccessSignature.Parse(iotHub, sasToken);

            // Assert
            httpClient.Verify();
            Assert.IsNotNull(sasToken);
            Assert.AreEqual(signature, token.Signature);
            Assert.AreEqual(audience, token.Audience);
            Assert.AreEqual(string.Empty, token.KeyName);
        }

        [TestMethod]
        public async Task TestSafeCreateNewToken_ShouldReturnSasToken_DeviceIdWithChars()
        {
            // Arrange
            string deviceId = "n@m.et#st";
            string moduleId = "$edgeAgent";
            var httpClient = new Mock<ISignatureProvider>();
            httpClient.Setup(p => p.SignAsync(moduleId, generationId, It.IsAny<string>())).Returns(Task.FromResult(signature));

            var moduleAuthenticationWithHsm = new ModuleAuthenticationWithHsm(httpClient.Object, deviceId, moduleId, generationId);

            // Act
            string audience = string.Format(CultureInfo.InvariantCulture, "{0}/devices/{1}/modules/{2}",
                iotHub,
                WebUtility.UrlEncode(deviceId),
                WebUtility.UrlEncode(moduleId));

            string sasToken = await moduleAuthenticationWithHsm.GetTokenAsync(audience);
            SharedAccessSignature token = SharedAccessSignature.Parse(iotHub, sasToken);

            // Assert
            httpClient.Verify();
            Assert.IsNotNull(sasToken);
            Assert.AreEqual(signature, token.Signature);
            Assert.AreEqual(audience, token.Audience);
            Assert.AreEqual(string.Empty, token.KeyName);
        }

        [TestMethod]
        public async Task TestSafeCreateNewToken_WhenIotEdgedThrows_ShouldThrow()
        {
            var httpClient = new Mock<ISignatureProvider>();
            httpClient.Setup(p => p.SignAsync(moduleId, generationId, It.IsAny<string>())).Throws(new HttpHsmComunicationException(It.IsAny<string>(), It.IsAny<int>()));

            var authenticationWithHsm = new ModuleAuthenticationWithHsm(httpClient.Object, deviceId, moduleId, generationId);

            await TestAssert.ThrowsAsync<HttpHsmComunicationException>(async () => await authenticationWithHsm.GetTokenAsync(iotHub));
        }
    }
}
