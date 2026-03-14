// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningDeviceClientTests
    {
        private const string GlobalDeviceEndpoint = "global.azure-devices-provisioning.net";
        private const string IdScope = "fakeidscope";
        private const string FakeRegistrationId = "registrationId";
        private const string FakePrimaryKey = "dGVzdFN0cmluZbB=";
        private const string FakeSecondaryKey = "wGVzdFN9CmluZaA=";

#pragma warning disable SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2 s_cert = new();
#pragma warning restore SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2Collection s_certs = new();

        [TestMethod]
        public void ProvisioningDeviceClient_AuthProviderSymmetricKey_Works()
        {
            // arrange
            var authProvider = new AuthenticationProviderSymmetricKey(FakeRegistrationId, FakePrimaryKey, FakeSecondaryKey);

            // act
            Func<ProvisioningDeviceClient> act = () => _ = new ProvisioningDeviceClient(GlobalDeviceEndpoint, IdScope, authProvider);

            // assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ProvisioningDeviceClient_AuthProviderX509_Works()
        {
            // arrange
            var authProvider = new AuthenticationProviderX509(s_cert, s_certs);

            // act
            Func<ProvisioningDeviceClient> act = () => _ = new ProvisioningDeviceClient(GlobalDeviceEndpoint, IdScope, authProvider);

            // assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ProvisioningDeviceClient_MqttWs_Works()
        {
            // arrange

            var mockAuthProvider = new Mock<AuthenticationProvider>();
            var options = new ProvisioningClientOptions(new ProvisioningClientMqttSettings(ProvisioningClientTransportProtocol.WebSocket));

            // act
            var provisioningDeviceClient = new ProvisioningDeviceClient(GlobalDeviceEndpoint, IdScope, mockAuthProvider.Object, options);

            // assert
            provisioningDeviceClient.Should().NotBeNull();
        }

        [TestMethod]
        public void ProvisioningDeviceClient_AmqpTcp_Works()
        {
            // arrange

            var mockAuthProvider = new Mock<AuthenticationProvider>();
            var options = new ProvisioningClientOptions(new ProvisioningClientAmqpSettings());

            // act
            var provisioningDeviceClient = new ProvisioningDeviceClient(GlobalDeviceEndpoint, IdScope, mockAuthProvider.Object, options);

            // assert
            provisioningDeviceClient.Should().NotBeNull();
        }

        [TestMethod]
        public void ProvisioningDeviceClient_AmqpWs_Works()
        {
            // arrange

            var mockAuthProvider = new Mock<AuthenticationProvider>();
            var options = new ProvisioningClientOptions(new ProvisioningClientAmqpSettings(ProvisioningClientTransportProtocol.WebSocket));

            // act
            var provisioningDeviceClient = new ProvisioningDeviceClient(GlobalDeviceEndpoint, IdScope, mockAuthProvider.Object, options);

            // assert
            provisioningDeviceClient.Should().NotBeNull();
        }

        [TestMethod]
        public void ProvisioningDeviceClient_RetryPolicy_Null()
        {
            // arrange

            var options = new ProvisioningClientOptions
            {
                RetryPolicy = null,
            };
            var mockAuthProvider = new Mock<AuthenticationProvider>();

            // act
            var provisioningDeviceClient = new ProvisioningDeviceClient(GlobalDeviceEndpoint, IdScope, mockAuthProvider.Object, options);

            // assert

            provisioningDeviceClient.Should().NotBeNull();
            provisioningDeviceClient.RetryPolicy.Should().BeOfType<ProvisioningClientNoRetry>();
        }

        [TestMethod]
        public void ProvisioningDeviceClient_RetryPolicy_Default()
        {
            // arrange
            var mockAuthProvider = new Mock<AuthenticationProvider>();

            // act
            var provisioningDeviceClient = new ProvisioningDeviceClient(GlobalDeviceEndpoint, IdScope, mockAuthProvider.Object);

            // assert

            provisioningDeviceClient.Should().NotBeNull();
            provisioningDeviceClient.RetryPolicy.Should().BeOfType<ProvisioningClientExponentialBackoffRetryPolicy>();
        }

        [TestMethod]
        public void ProvisioningDeviceClient_RetryPolicy_FixedDelayRetryPolicy()
        {
            // arrange

            var options = new ProvisioningClientOptions
            {
                RetryPolicy = new ProvisioningClientFixedDelayRetryPolicy(0, TimeSpan.FromSeconds(10), false),
            };
            var mockAuthProvider = new Mock<AuthenticationProvider>();

            // act
            var provisioningDeviceClient = new ProvisioningDeviceClient(GlobalDeviceEndpoint, IdScope, mockAuthProvider.Object, options);

            // assert

            provisioningDeviceClient.Should().NotBeNull();
            provisioningDeviceClient.RetryPolicy.Should().BeOfType<ProvisioningClientFixedDelayRetryPolicy>();
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_RegisterAsync()
        {
            // arrange

            var customPayload = new CustomType { CustomInt = 4, CustomString = "bar" };
            var mockAuthProvider = new Mock<AuthenticationProvider>();
            var mockRegistrationResult = new DeviceRegistrationResult
            {
                Status = ProvisioningRegistrationStatus.Assigned,
                Payload = new JRaw(JsonConvert.SerializeObject(customPayload)),
            };

            var mockTransportHandler = new Mock<ProvisioningTransportHandler>();
            mockTransportHandler
                .Setup(t => t.RegisterAsync(It.IsAny<ProvisioningTransportRegisterRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockRegistrationResult);

            var provisioningDeviceClient = new ProvisioningDeviceClient(
                GlobalDeviceEndpoint,
                IdScope,
                mockAuthProvider.Object,
                mockTransportHandler.Object);

            // act
            Func<Task> act = async () => _ = await provisioningDeviceClient.RegisterAsync().ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_RegisterAsync_WithRegistrationPayload()
        {
            // arrange

            var customPayload = new CustomType { CustomInt = 4, CustomString = "bar" };
            var mockAuthProvider = new Mock<AuthenticationProvider>();
            var mockRegistrationResult = new DeviceRegistrationResult
            {
                Status = ProvisioningRegistrationStatus.Assigned,
                Payload = new JRaw(JsonConvert.SerializeObject(customPayload)),
            };

            var mockTransportHandler = new Mock<ProvisioningTransportHandler>();
            mockTransportHandler
                .Setup(t => t.RegisterAsync(It.IsAny<ProvisioningTransportRegisterRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockRegistrationResult);

            var provisioningDeviceClient = new ProvisioningDeviceClient(
                GlobalDeviceEndpoint,
                IdScope,
                mockAuthProvider.Object,
                mockTransportHandler.Object);

            var requestPayload = new RegistrationRequestPayload
            {
                Payload = customPayload,
            };

            // act
            Func<Task> act = async () => _ = await provisioningDeviceClient.RegisterAsync(requestPayload).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        private class CustomType
        {
            [JsonProperty("customInt")]
            public int CustomInt { get; set; }

            [JsonProperty("customString")]
            public string CustomString { get; set; }
        }
    }
}
