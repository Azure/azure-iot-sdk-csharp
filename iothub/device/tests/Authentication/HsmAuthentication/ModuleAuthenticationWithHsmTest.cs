// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.HsmAuthentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Client.Test.HsmAuthentication
{
    [TestClass]
    [TestCategory("Unit")]
    public class ModuleAuthenticationWithHsmTest
    {
        private const string Signature = "signature";
        private const string DeviceId = "device1";
        private const string ModuleId = "module1";
        private const string GenerationId = "1";
        private const string IotHub = "iothub.test";

        [TestMethod]
        public async Task TestSafeCreateNewToken_ShouldReturnSasToken()
        {
            // arrange
            var httpClient = new Mock<ISignatureProvider>();
            httpClient.Setup(p => p.SignAsync(ModuleId, GenerationId, It.IsAny<string>())).Returns(Task.FromResult(Signature));

            var moduleAuthenticationWithHsm = new EdgeModuleAuthenticationWithHsm(httpClient.Object, DeviceId, ModuleId, GenerationId);

            // act
            string sasToken = await moduleAuthenticationWithHsm.GetTokenAsync(IotHub);
            SharedAccessSignature token = SharedAccessSignatureParser.Parse(sasToken);

            string audience = string.Format(CultureInfo.InvariantCulture, "{0}/devices/{1}/modules/{2}",
                IotHub,
                WebUtility.UrlEncode(DeviceId),
                WebUtility.UrlEncode(ModuleId));

            // assert
            httpClient.Verify();
            Assert.IsNotNull(sasToken);
            Assert.AreEqual(Signature, token.Signature);
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
            httpClient.Setup(p => p.SignAsync(moduleId, GenerationId, It.IsAny<string>())).Returns(Task.FromResult(Signature));

            var moduleAuthenticationWithHsm = new EdgeModuleAuthenticationWithHsm(httpClient.Object, deviceId, moduleId, GenerationId);

            // act
            string sasToken = await moduleAuthenticationWithHsm.GetTokenAsync(IotHub);
            SharedAccessSignature token = SharedAccessSignatureParser.Parse(sasToken);

            string audience = string.Format(CultureInfo.InvariantCulture, "{0}/devices/{1}/modules/{2}",
                IotHub,
                WebUtility.UrlEncode(deviceId),
                WebUtility.UrlEncode(moduleId));

            // assert
            httpClient.Verify();
            Assert.IsNotNull(sasToken);
            Assert.AreEqual(Signature, token.Signature);
            Assert.AreEqual(audience, token.Audience);
            Assert.AreEqual(string.Empty, token.KeyName);
        }

        [TestMethod]
        public async Task TestSafeCreateNewToken_WhenIotEdgedThrows_ShouldThrow()
        {
            var httpClient = new Mock<ISignatureProvider>();
            httpClient.Setup(p => p.SignAsync(ModuleId, GenerationId, It.IsAny<string>())).Throws(new HttpHsmComunicationException(It.IsAny<string>(), It.IsAny<int>()));

            var authenticationWithHsm = new EdgeModuleAuthenticationWithHsm(httpClient.Object, DeviceId, ModuleId, GenerationId);

            Func<Task> act = async () => await authenticationWithHsm.GetTokenAsync(IotHub);
            await act.Should().ThrowAsync<HttpHsmComunicationException>();
        }
    }
}
