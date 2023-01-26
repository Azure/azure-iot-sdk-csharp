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
        private static readonly string s_globalDeviceEndpoint = "global.azure-devices-provisioning.net";
        private static readonly string s_idScope = "fakeidscope";

        private static readonly string s_fakeRegistrationId = "registrationId";
        private static readonly string s_fakePrimaryKey = "dGVzdFN0cmluZbB=";
        private static readonly string s_fakeSecondaryKey = "wGVzdFN9CmluZaA=";

#pragma warning disable SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2 s_cert = new();
#pragma warning restore SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2Collection s_certs = new();

        [TestMethod]
        public void ProvisioningDeviceClient_AuthProviderSymmetricKey_Works()
        {
            // arrange
            var authProvider = new AuthenticationProviderSymmetricKey(s_fakeRegistrationId, s_fakePrimaryKey, s_fakeSecondaryKey);

            // act
            var provisioningDeviceClient = new ProvisioningDeviceClient(s_globalDeviceEndpoint, s_idScope, authProvider);

            // assert
            provisioningDeviceClient.Should().NotBeNull();
        }

        [TestMethod]
        public void ProvisioningDeviceClient_AuthProviderX509_Works()
        {
            // arrange
            var authProvider = new AuthenticationProviderX509(s_cert, s_certs);

            // act
            var provisioningDeviceClient = new ProvisioningDeviceClient(s_globalDeviceEndpoint, s_idScope, authProvider);

            // assert
            provisioningDeviceClient.Should().NotBeNull();
        }

        [TestMethod]
        public void ProvisioningDeviceClient_MqttWs_Works()
        {
            // arrange

            var mockAuthProvider = new Mock<AuthenticationProvider>();
            var options = new ProvisioningClientOptions(new ProvisioningClientMqttSettings(ProvisioningClientTransportProtocol.WebSocket));

            // act
            var provisioningDeviceClient = new ProvisioningDeviceClient(s_globalDeviceEndpoint, s_idScope, mockAuthProvider.Object, options);

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
            var provisioningDeviceClient = new ProvisioningDeviceClient(s_globalDeviceEndpoint, s_idScope, mockAuthProvider.Object, options);

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
            var provisioningDeviceClient = new ProvisioningDeviceClient(s_globalDeviceEndpoint, s_idScope, mockAuthProvider.Object, options);

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
            var provisioningDeviceClient = new ProvisioningDeviceClient(s_globalDeviceEndpoint, s_idScope, mockAuthProvider.Object, options);

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
            var provisioningDeviceClient = new ProvisioningDeviceClient(s_globalDeviceEndpoint, s_idScope, mockAuthProvider.Object);

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
            var provisioningDeviceClient = new ProvisioningDeviceClient(s_globalDeviceEndpoint, s_idScope, mockAuthProvider.Object, options);

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
                s_globalDeviceEndpoint,
                s_idScope,
                mockAuthProvider.Object,
                mockTransportHandler.Object);

            // act
            Func<Task> act = async () => await provisioningDeviceClient.RegisterAsync().ConfigureAwait(false);

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
                s_globalDeviceEndpoint,
                s_idScope,
                mockAuthProvider.Object,
                mockTransportHandler.Object);

            var requestPayload = new RegistrationRequestPayload
            {
                Payload = customPayload,
            };

            // act
            Func<Task> act = async () => await provisioningDeviceClient.RegisterAsync(requestPayload).ConfigureAwait(false);

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
