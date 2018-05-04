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
    public class ModuleAuthenticationWithHsmTest
    {
        string signature = "signature";
        string deviceId = "device1";
        string moduleId = "module1";
        string iotHub = "iothub.test";

        [TestMethod]
        public async Task TestSafeCreateNewToken_ShouldReturnSasToken()
        {
            // Arrange
            var httpClient = new Mock<ISignatureProvider>();
            httpClient.Setup(p => p.SignAsync(this.moduleId, It.IsAny<string>())).Returns(Task.FromResult(this.signature));

            var moduleAuthenticationWithHsm = new ModuleAuthenticationWithHsm(httpClient.Object, this.deviceId, this.moduleId);

            // Act
            string sasToken = await moduleAuthenticationWithHsm.GetTokenAsync(this.iotHub);
            SharedAccessSignature token = SharedAccessSignature.Parse(iotHub, sasToken);

            string audience = WebUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "{0}/devices/{1}/modules/{2}",
                this.iotHub,
                this.deviceId,
                this.moduleId));

            // Assert
            httpClient.Verify();
            Assert.IsNotNull(sasToken);
            Assert.AreEqual(this.signature, token.Signature);
            Assert.AreEqual(WebUtility.UrlDecode(audience), token.Audience);
            Assert.AreEqual(string.Empty, token.KeyName);
        }

        [TestMethod]
        public async Task TestSafeCreateNewToken_WhenIotEdgedThrows_ShouldThrow()
        {
            var httpClient = new Mock<ISignatureProvider>();
            httpClient.Setup(p => p.SignAsync(this.moduleId, It.IsAny<string>())).Throws(new HttpHsmComunicationException(It.IsAny<string>(), It.IsAny<int>()));

            var authenticationWithHsm = new ModuleAuthenticationWithHsm(httpClient.Object, this.deviceId, this.moduleId);

            await TestAssert.ThrowsAsync<HttpHsmComunicationException>(async () => await authenticationWithHsm.GetTokenAsync(this.iotHub));
        }
    }
}
