// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
using FluentAssertions;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class ClientAuthenticationWithTokenRefreshTests
    {
        private const string TestDeviceId = "TestDeviceID";
        private const string TestModuleId = "TestModuleID";
        private const string TestIotHubName = "contoso.azure-devices.net";
        private const string TestSharedAccessKey = "dGVzdFN0cmluZzE=";
        private const int DefaultTimeToLiveSeconds = 1 * 60 * 60;

        [TestMethod]
        public void ClientAuthenticationWithTokenRefresh_Ctor_WrongArguments_Fail()
        {
            Action act = () => _ = new TestImplementation(null);
            act.Should().Throw<ArgumentNullException>();

            act = () => _ = new TestImplementation("   ");
            act.Should().Throw<ArgumentException>();

            act = () => _ = new TestImplementation(null, TestModuleId);
            act.Should().Throw<ArgumentNullException>();

            act = () => _ = new TestImplementation("   ", TestModuleId);
            act.Should().Throw<ArgumentException>();

            act = () => _ = new TestImplementation(TestDeviceId, "   ");
            act.Should().Throw<ArgumentException>();

            act = () => _ = new TestImplementation(TestDeviceId, TestModuleId, TimeSpan.FromSeconds(-1), 10);
            act.Should().Throw<ArgumentOutOfRangeException>();

            act = () => _ = new TestImplementation(TestDeviceId, TestModuleId, TimeSpan.FromSeconds(60), -1);
            act.Should().Throw<ArgumentOutOfRangeException>();

            act = () => _ = new TestImplementation(TestDeviceId, TestModuleId, TimeSpan.FromSeconds(60), 101);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void ClientAuthenticationWithTokenRefresh_Ctor_DefaultsGetProperties_Ok()
        {
            var refresher = new TestImplementation(TestDeviceId);
            TestDeviceId.Should().Be(refresher.DeviceId);

            // Until GetTokenAsync, the token is expired.
            DateTime expectedExpiryTime = DateTime.UtcNow.AddSeconds(-DefaultTimeToLiveSeconds);
            int timeDelta = (int)(refresher.ExpiresOnUtc - expectedExpiryTime).TotalSeconds;

            Math.Abs(timeDelta).Should().BeLessThan(3, $"Expiration time delta is {timeDelta}");
            refresher.IsExpiring.Should().BeTrue();
        }

        [TestMethod]
        public async Task ClientAuthenticationWithTokenRefresh_InitializedToken_GetProperties_Ok()
        {
            var ttl = TimeSpan.FromSeconds(5);
            int buffer = 20;  // Token should refresh after 4 seconds.

            var refresher = new TestImplementation(TestDeviceId, suggestedTimeToLive: ttl, timeBufferPercentage: buffer);
            await refresher.GetTokenAsync(TestIotHubName).ConfigureAwait(false);

            DateTime currentTime = DateTime.UtcNow;
            DateTime expectedExpiryTime = currentTime.AddSeconds(ttl.TotalSeconds);
            DateTime expectedRefreshTime = expectedExpiryTime.AddSeconds(-((double)buffer / 100) * ttl.TotalSeconds);

            TestDeviceId.Should().Be(refresher.DeviceId);

            int timeDelta = (int)(refresher.ExpiresOnUtc - expectedExpiryTime).TotalSeconds;
            Math.Abs(timeDelta).Should().BeLessThan(3, $"Expiration time delta is {timeDelta}");

            timeDelta = (int)(refresher.RefreshesOnUtc - expectedRefreshTime).TotalSeconds;
            Math.Abs(timeDelta).Should().BeLessThan(3, $"Expiration time delta is {timeDelta}");

            TimeSpan delayTime = refresher.RefreshesOnUtc - DateTime.UtcNow + TimeSpan.FromMilliseconds(500);

            // Wait for the expiration time given the time buffer.
            if (delayTime.TotalSeconds > 0)
            {
                await Task.Delay(delayTime).ConfigureAwait(false);
            }

            Debug.Assert(refresher.IsExpiring, $"Current time = {DateTime.UtcNow}");
            refresher.IsExpiring.Should().BeTrue();
        }

        [TestMethod]
        public void ClientAuthenticationWithTokenRefresh_Populate_InvalidConnectionStringBuilder_Fail()
        {
            var refresher = new TestImplementation(TestDeviceId);
            IotHubConnectionCredentials iotHubConnectionCredentials = null;
            Action act = () => refresher.Populate(ref iotHubConnectionCredentials);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ClientAuthenticationWithTokenRefresh_GetTokenAsync_NewTtl_Ok()
        {
            var ttl = TimeSpan.FromSeconds(1);

            var refresher = new TestImplementation(TestDeviceId, suggestedTimeToLive: ttl, timeBufferPercentage: 90);
            await refresher.GetTokenAsync(TestIotHubName).ConfigureAwait(false);

            DateTime expectedExpiryTime = DateTime.UtcNow.AddSeconds(ttl.TotalSeconds);
            int timeDelta = (int)(refresher.ExpiresOnUtc - expectedExpiryTime).TotalSeconds;
            Math.Abs(timeDelta).Should().BeLessThan(3, $"Expiration time delta is {timeDelta}");

            // Wait for the token to expire;
            while (!refresher.IsExpiring)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }

            // Configure the test token refresher to ignore the suggested TTL.
            ttl = TimeSpan.FromSeconds(10);
            refresher.ActualTimeToLive = (int)ttl.TotalSeconds;

            await refresher.GetTokenAsync(TestIotHubName).ConfigureAwait(false);

            expectedExpiryTime = DateTime.UtcNow.AddSeconds(ttl.TotalSeconds);
            timeDelta = (int)(refresher.ExpiresOnUtc - expectedExpiryTime).TotalSeconds;
            Math.Abs(timeDelta).Should().BeLessThan(3, $"Expiration time delta is {timeDelta}");
        }

        [TestMethod]
        public async Task ClientAuthenticationWithTokenRefresh_WithDevice_Populate_DefaultParameters_Ok()
        {
            // arrange
            var refresher = new TestImplementation(TestDeviceId);
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(refresher, TestIotHubName);

            // act
            refresher.Populate(ref iotHubConnectionCredentials);

            // assert
            TestDeviceId.Should().Be(iotHubConnectionCredentials.DeviceId);
            iotHubConnectionCredentials.SharedAccessSignature.Should().BeNull();
            iotHubConnectionCredentials.SharedAccessKey.Should().BeNull();
            iotHubConnectionCredentials.SharedAccessKeyName.Should().BeNull();

            // act
            string token = await refresher.GetTokenAsync(TestIotHubName).ConfigureAwait(false);
            refresher.Populate(ref iotHubConnectionCredentials);

            // assert
            TestDeviceId.Should().Be(iotHubConnectionCredentials.DeviceId);
            token.Should().Be(iotHubConnectionCredentials.SharedAccessSignature);
            iotHubConnectionCredentials.SharedAccessKey.Should().BeNull();
            iotHubConnectionCredentials.SharedAccessKeyName.Should().BeNull();
        }

        [TestMethod]
        public async Task ClientAuthenticationWithTokenRefresh_WithModule_Populate_DefaultParameters_Ok()
        {
            // arrange
            var refresher = new TestImplementation(TestDeviceId, TestModuleId);
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(refresher, TestIotHubName);

            // act
            refresher.Populate(ref iotHubConnectionCredentials);

            // assert
            TestDeviceId.Should().Be(iotHubConnectionCredentials.DeviceId);
            TestModuleId.Should().Be(iotHubConnectionCredentials.ModuleId);
            iotHubConnectionCredentials.SharedAccessSignature.Should().BeNull();
            iotHubConnectionCredentials.SharedAccessKey.Should().BeNull();
            iotHubConnectionCredentials.SharedAccessKeyName.Should().BeNull();

            // act
            string token = await refresher.GetTokenAsync(TestIotHubName);
            refresher.Populate(ref iotHubConnectionCredentials);

            // assert
            TestDeviceId.Should().Be(iotHubConnectionCredentials.DeviceId);
            TestModuleId.Should().Be(iotHubConnectionCredentials.ModuleId);
            iotHubConnectionCredentials.SharedAccessSignature.Should().Be(token);
            iotHubConnectionCredentials.SharedAccessKey.Should().BeNull();
            iotHubConnectionCredentials.SharedAccessKeyName.Should().BeNull();
        }

        [TestMethod]
        public async Task ClientAuthenticationWithSakRefresh_WithDevice_SharedAccessKeyConnectionString_HasRefresher()
        {
            // arrange and act
            IConnectionCredentials iotHubConnectionCredentials = new IotHubConnectionCredentials(
                new ClientAuthenticationWithSharedAccessKeyRefresh(TestSharedAccessKey, TestDeviceId),
                TestIotHubName);

            // assert
            iotHubConnectionCredentials.SasTokenRefresher.Should().NotBeNull();
            iotHubConnectionCredentials.SasTokenRefresher.Should().BeOfType<ClientAuthenticationWithSharedAccessKeyRefresh>();

            // act
            var cbsAuth = new AmqpIotCbsTokenProvider(iotHubConnectionCredentials);
            string token1 = await iotHubConnectionCredentials.GetPasswordAsync().ConfigureAwait(false);
            CbsToken token2 = await cbsAuth.GetTokenAsync(new Uri("amqp://" + TestIotHubName), "testAppliesTo", null).ConfigureAwait(false);

            // assert
            iotHubConnectionCredentials.SharedAccessSignature.Should().BeNull();
            TestDeviceId.Should().Be(iotHubConnectionCredentials.DeviceId);
            token1.Should().NotBeNull();
            token2.Should().NotBeNull();
            token2.TokenValue.Should().Be(token1);
        }

        [TestMethod]
        public async Task ClientAuthenticationWithSakRefresh_WithModule_SharedAccessKeyConnectionString_HasRefresher()
        {
            // arrange and act
            IConnectionCredentials iotHubConnectionCredentials = new IotHubConnectionCredentials(
                new ClientAuthenticationWithSharedAccessKeyRefresh(
                    sharedAccessKey: TestSharedAccessKey,
                    deviceId: TestDeviceId,
                    moduleId: TestModuleId),
                TestIotHubName);

            // assert
            iotHubConnectionCredentials.SasTokenRefresher.Should().NotBeNull();
            iotHubConnectionCredentials.SasTokenRefresher.Should().BeOfType<ClientAuthenticationWithSharedAccessKeyRefresh>();

            // act
            var cbsAuth = new AmqpIotCbsTokenProvider(iotHubConnectionCredentials);
            string token1 = await iotHubConnectionCredentials.GetPasswordAsync().ConfigureAwait(false);
            CbsToken token2 = await cbsAuth.GetTokenAsync(new Uri("amqp://" + TestIotHubName), "testAppliesTo", null).ConfigureAwait(false);

            // assert
            iotHubConnectionCredentials.SharedAccessSignature.Should().BeNull();
            iotHubConnectionCredentials.DeviceId.Should().Be(TestDeviceId);
            token1.Should().NotBeNull();
            token2.Should().NotBeNull();
            token2.TokenValue.Should().Be(token1);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(TestSharedAccessKey + "123")]
        public void ClientAuthenticationWithSakRefresh_InvalidKey_Throws(string key)
        {
            Action act = () => _ = new SharedAccessSignatureBuilder
            {
                Key = key,
            };
            act.Should().Throw<FormatException>();
        }

        private static string CreateToken(int suggestedTimeToLiveSeconds)
        {
            var builder = new SharedAccessSignatureBuilder
            {
                Key = TestSharedAccessKey,
                TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLiveSeconds)
            };

            return builder.ToSignature();
        }

        private class TestImplementation : ClientAuthenticationWithTokenRefresh
        {
            public int SafeCreateNewTokenCallCount { get; private set; }

            public int ActualTimeToLive { get; set; }

            public TestImplementation(
                string deviceId,
                string moduleId = default,
                TimeSpan suggestedTimeToLive = default,
                int timeBufferPercentage = default)
                : base(deviceId, moduleId, suggestedTimeToLive, timeBufferPercentage)
            {
            }

            ///<inheritdoc/>
            protected override async Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive)
            {
                SafeCreateNewTokenCallCount++;

                await Task.Delay(10).ConfigureAwait(false);

                int ttl = (int)suggestedTimeToLive.TotalSeconds;
                if (ActualTimeToLive > 0)
                {
                    ttl = ActualTimeToLive;
                }

                return CreateToken(ttl);
            }
        }
    }
}
