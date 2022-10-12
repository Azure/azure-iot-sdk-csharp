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
            // arrange
            var httpClient = new Mock<ISignatureProvider>();
            httpClient.Setup(p => p.SignAsync(this.moduleId, this.generationId, It.IsAny<string>())).Returns(Task.FromResult(this.signature));

            var moduleAuthenticationWithHsm = new EdgeModuleAuthenticationWithHsm(httpClient.Object, this.deviceId, this.moduleId, this.generationId);

            // act
            string sasToken = await moduleAuthenticationWithHsm.GetTokenAsync(this.iotHub);
            SharedAccessSignature token = SharedAccessSignatureParser.Parse(sasToken);

            string audience = string.Format(CultureInfo.InvariantCulture, "{0}/devices/{1}/modules/{2}",
                this.iotHub,
                WebUtility.UrlEncode(this.deviceId),
                WebUtility.UrlEncode(this.moduleId));

            // assert
            httpClient.Verify();
            Assert.IsNotNull(sasToken);
            Assert.AreEqual(this.signature, token.Signature);
            Assert.AreEqual(audience, token.Audience);
            Assert.AreEqual(string.Empty, token.KeyName);
        }

        [TestMethod]
        public async Task TestSafeCreateNewToken_ShouldReturnSasToken_DeviceIdWithChars()
        {
            // arrange
            string deviceId = "n@m.et#st";
            string moduleId = "$edgeAgent";
            var httpClient = new Mock<ISignatureProvider>();
            httpClient.Setup(p => p.SignAsync(moduleId, this.generationId, It.IsAny<string>())).Returns(Task.FromResult(this.signature));

            var moduleAuthenticationWithHsm = new EdgeModuleAuthenticationWithHsm(httpClient.Object, deviceId, moduleId, this.generationId);

            // act
            string sasToken = await moduleAuthenticationWithHsm.GetTokenAsync(this.iotHub);
            SharedAccessSignature token = SharedAccessSignatureParser.Parse(sasToken);

            string audience = string.Format(CultureInfo.InvariantCulture, "{0}/devices/{1}/modules/{2}",
                this.iotHub,
                WebUtility.UrlEncode(deviceId),
                WebUtility.UrlEncode(moduleId));

            // assert
            httpClient.Verify();
            Assert.IsNotNull(sasToken);
            Assert.AreEqual(this.signature, token.Signature);
            Assert.AreEqual(audience, token.Audience);
            Assert.AreEqual(string.Empty, token.KeyName);
        }

        [TestMethod]
        public async Task TestSafeCreateNewToken_WhenIotEdgedThrows_ShouldThrow()
        {
            var httpClient = new Mock<ISignatureProvider>();
            httpClient.Setup(p => p.SignAsync(this.moduleId, this.generationId, It.IsAny<string>())).Throws(new HttpHsmComunicationException(It.IsAny<string>(), It.IsAny<int>()));

            var authenticationWithHsm = new EdgeModuleAuthenticationWithHsm(httpClient.Object, this.deviceId, this.moduleId, this.generationId);

            await TestAssert.ThrowsAsync<HttpHsmComunicationException>(async () => await authenticationWithHsm.GetTokenAsync(this.iotHub));
        }
    }
}
