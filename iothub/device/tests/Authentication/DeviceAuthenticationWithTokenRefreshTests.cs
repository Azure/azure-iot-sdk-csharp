// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class DeviceAuthenticationWithTokenRefreshTests
    {
        private const string TestDeviceId = "TestDeviceID";
        private const string TestIotHubName = "contoso.azure-devices.net";
        private const int DefaultTimeToLiveSeconds = 1 * 60 * 60;
        private static string TestSharedAccessKey;

        static DeviceAuthenticationWithTokenRefreshTests()
        {
            var rnd = new Random();
            var rndBytes = new byte[32];
            rnd.NextBytes(rndBytes);

            TestSharedAccessKey = Convert.ToBase64String(rndBytes);
        }

        [TestMethod]
        public void DeviceAuthenticationWithTokenRefresh_Ctor_WrongArguments_Fail()
        {
            TestAssert.Throws<ArgumentNullException>(() => new TestImplementation(null));
            TestAssert.Throws<ArgumentException>(() => new TestImplementation("   "));
            TestAssert.Throws<ArgumentOutOfRangeException>(() => new TestImplementation(TestDeviceId, TimeSpan.FromSeconds(-1), 10));
            TestAssert.Throws<ArgumentOutOfRangeException>(() => new TestImplementation(TestDeviceId, TimeSpan.FromSeconds(60), -1));
            TestAssert.Throws<ArgumentOutOfRangeException>(() => new TestImplementation(TestDeviceId, TimeSpan.FromSeconds(60), 101));
        }

        [TestMethod]
        public void DeviceAuthenticationWithTokenRefresh_Ctor_DefaultsGetProperties_Ok()
        {
            var refresher = new TestImplementation(TestDeviceId);
            Assert.AreEqual(TestDeviceId, refresher.DeviceId);

            // Until GetTokenAsync, the token is expired.
            DateTime expectedExpiryTime = DateTime.UtcNow.AddSeconds(-DefaultTimeToLiveSeconds);
            int timeDelta = (int)((refresher.ExpiresOn - expectedExpiryTime).TotalSeconds);

            Assert.IsTrue(Math.Abs(timeDelta) < 3, $"Expiration time delta is {timeDelta}");
            Assert.IsTrue(refresher.IsExpiring);
        }

        [TestMethod]
        public async Task DeviceAuthenticationWithTokenRefresh_InitializedToken_GetProperties_Ok()
        {
            TimeSpan ttl = TimeSpan.FromSeconds(5);
            int buffer = 20;  // Token should refresh after 4 seconds.

            var refresher = new TestImplementation(TestDeviceId, ttl, buffer);
            await refresher.GetTokenAsync(TestIotHubName).ConfigureAwait(false);

            DateTime currentTime = DateTime.UtcNow;
            DateTime expectedExpiryTime = currentTime.AddSeconds(ttl.TotalSeconds);
            DateTime expectedRefreshTime = expectedExpiryTime.AddSeconds(-((double)buffer / 100) * ttl.TotalSeconds);

            Assert.AreEqual(TestDeviceId, refresher.DeviceId);

            int timeDelta = (int)((refresher.ExpiresOn - expectedExpiryTime).TotalSeconds);
            Assert.IsTrue(Math.Abs(timeDelta) < 3, $"ExpiresOn time delta is {timeDelta}");

            timeDelta = (int)((refresher.RefreshesOn - expectedRefreshTime).TotalSeconds);
            Assert.IsTrue(Math.Abs(timeDelta) < 3, $"RefreshesOn time delta is {timeDelta}");

            TimeSpan delayTime = refresher.RefreshesOn - DateTime.UtcNow + TimeSpan.FromMilliseconds(500);

            // Wait for the expiration time given the time buffer.
            if (delayTime.TotalSeconds > 0)
            {
                await Task.Delay(delayTime).ConfigureAwait(false);
            }

            Debug.Assert(refresher.IsExpiring, $"Current time = {DateTime.UtcNow}");
            Assert.AreEqual(true, refresher.IsExpiring);
        }

        [TestMethod]
        public async Task DeviceAuthenticationWithTokenRefresh_Populate_DefaultParameters_Ok()
        {
            var refresher = new TestImplementation(TestDeviceId);
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(refresher, TestIotHubName);

            refresher.Populate(iotHubConnectionCredentials);

            Assert.AreEqual(TestDeviceId, iotHubConnectionCredentials.DeviceId);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessSignature);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessKey);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessKeyName);

            string token = await refresher.GetTokenAsync(TestIotHubName).ConfigureAwait(false);

            refresher.Populate(iotHubConnectionCredentials);

            Assert.AreEqual(TestDeviceId, iotHubConnectionCredentials.DeviceId);
            Assert.AreEqual(token, iotHubConnectionCredentials.SharedAccessSignature);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessKey);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessKeyName);
        }

        [TestMethod]
        public void DeviceAuthenticationWithTokenRefresh_Populate_InvalidConnectionStringBuilder_Fail()
        {
            var refresher = new TestImplementation(TestDeviceId);
            TestAssert.Throws<ArgumentNullException>(() => refresher.Populate(null));
        }

        [TestMethod]
        public async Task DeviceAuthenticationWithTokenRefresh_GetTokenAsync_NewTtl_Ok()
        {
            TimeSpan ttl = TimeSpan.FromSeconds(1);

            var refresher = new TestImplementation(TestDeviceId, ttl, 90);
            await refresher.GetTokenAsync(TestIotHubName).ConfigureAwait(false);

            DateTime expectedExpiryTime = DateTime.UtcNow.AddSeconds(ttl.TotalSeconds);
            int timeDelta = (int)((refresher.ExpiresOn - expectedExpiryTime).TotalSeconds);
            Assert.IsTrue(Math.Abs(timeDelta) < 3, $"Expiration time delta is {timeDelta}");

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
            timeDelta = (int)((refresher.ExpiresOn - expectedExpiryTime).TotalSeconds);
            Assert.IsTrue(Math.Abs(timeDelta) < 3, $"Expiration time delta is {timeDelta}");
        }

        [TestMethod]
        public async Task DeviceAuthenticationWithSakRefresh_SharedAccessKeyConnectionString_HasRefresher()
        {
            IConnectionCredentials iotHubConnectionCredentials = new IotHubConnectionCredentials(
                new ClientAuthenticationWithRegistrySymmetricKey(TestSharedAccessKey, TestDeviceId),
                TestIotHubName);

            Assert.IsNotNull(iotHubConnectionCredentials.SasTokenRefresher);
            Assert.IsInstanceOfType(iotHubConnectionCredentials.SasTokenRefresher, typeof(ClientAuthenticationWithSakRefresh));

            var cbsAuth = new AmqpIotCbsTokenProvider(iotHubConnectionCredentials);

            string token1 = await iotHubConnectionCredentials.GetPasswordAsync().ConfigureAwait(false);
            CbsToken token2 = await cbsAuth.GetTokenAsync(new Uri("amqp://" + TestIotHubName), "testAppliesTo", null).ConfigureAwait(false);

            Assert.IsNull(iotHubConnectionCredentials.SharedAccessSignature);
            Assert.AreEqual(TestDeviceId, iotHubConnectionCredentials.DeviceId);

            Assert.IsNotNull(token1);
            Assert.IsNotNull(token2);
            Assert.AreEqual(token1, token2.TokenValue);
        }

        private static string CreateToken(int suggestedTimeToLiveSeconds)
        {
            var builder = new SharedAccessSignatureBuilder()
            {
                Key = TestSharedAccessKey,
                TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLiveSeconds)
            };

            return builder.ToSignature();
        }

        private class TestImplementation : ClientAuthenticationWithTokenRefresh
        {
            private int _callCount = 0;

            public int SafeCreateNewTokenCallCount => _callCount;

            public int ActualTimeToLive { get; set; }

            public TestImplementation(string deviceId) : base(deviceId)
            {
            }

            public TestImplementation(
                string deviceId,
                TimeSpan suggestedTimeToLive,
                int timeBufferPercentage)
                : base(
                      deviceId: deviceId,
                      suggestedTimeToLive: suggestedTimeToLive,
                      timeBufferPercentage: timeBufferPercentage)
            {
            }

            ///<inheritdoc/>
            protected override async Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive)
            {
                _callCount++;

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
