// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class AuthenticationWithTokenRefreshTests
    {
        private const string TestIotHubName = "contoso.azure-devices.net";
        private const int DefaultTimeToLiveSeconds = 1 * 60 * 60;
        private static string TestSharedAccessKey;

        static AuthenticationWithTokenRefreshTests()
        {
            var rnd = new Random();
            var rndBytes = new byte[32];
            rnd.NextBytes(rndBytes);

            TestSharedAccessKey = Convert.ToBase64String(rndBytes);
        }

        [TestMethod]
        public async Task AuthenticationWithTokenRefresh_InitializedToken_GetProperties_Ok()
        {
            TimeSpan ttl = TimeSpan.FromSeconds(5);
            int buffer = 20;  // Token should refresh after 4 seconds.

            var refresher = new TestImplementation(ttl, buffer);
            await refresher.GetTokenAsync(TestIotHubName);

            DateTime currentTime = DateTime.UtcNow;
            DateTime expectedExpiryTime = currentTime.AddSeconds(ttl.TotalSeconds);
            DateTime expectedRefreshTime = expectedExpiryTime.AddSeconds(-((double)buffer / 100) * ttl.TotalSeconds);

            int timeDelta = (int)((refresher.ExpiresOn - expectedExpiryTime).TotalSeconds);
            Assert.IsTrue(Math.Abs(timeDelta) < 3, $"ExpiresOn time delta is {timeDelta}");

            timeDelta = (int)((refresher.RefreshesOn - expectedRefreshTime).TotalSeconds);
            Assert.IsTrue(Math.Abs(timeDelta) < 3, $"RefreshesOn time delta is {timeDelta}");

            TimeSpan delayTime = refresher.RefreshesOn - DateTime.UtcNow + TimeSpan.FromMilliseconds(500);

            // Wait for the expiration time given the time buffer.
            if (delayTime.TotalSeconds > 0)
            {
                await Task.Delay(delayTime);
            }

            Debug.Assert(refresher.IsExpiring, $"Current time = {DateTime.UtcNow}");
            Assert.AreEqual(true, refresher.IsExpiring);
        }

        [TestMethod]
        public async Task AuthenticationWithTokenRefresh_NonExpiredToken_GetTokenCached_Ok()
        {
            var refresher = new TestImplementation();
            string expectedToken = CreateToken(DefaultTimeToLiveSeconds);

            string token1 = await refresher.GetTokenAsync(TestIotHubName);
            string token2 = await refresher.GetTokenAsync(TestIotHubName);

            Assert.AreEqual(1, refresher.SafeCreateNewTokenCallCount); // Cached.
            Assert.AreEqual(expectedToken, token1);
            Assert.AreEqual(token1, token2);
        }

        [TestMethod]
        public async Task AuthenticationWithTokenRefresh_Populate_DefaultParameters_Ok()
        {
            var refresher = new TestImplementation();
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(
                new ModuleAuthenticationWithRegistrySymmetricKey("deviceId", "moduleid", TestSharedAccessKey),
                TestIotHubName);

            refresher.Populate(iotHubConnectionCredentials);

            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessSignature);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessKey);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessKeyName);

            string token = await refresher.GetTokenAsync(TestIotHubName);

            refresher.Populate(iotHubConnectionCredentials);

            Assert.AreEqual(token, iotHubConnectionCredentials.SharedAccessSignature);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessKey);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessKeyName);
        }

        [TestMethod]
        public void AuthenticationWithTokenRefresh_Populate_InvalidConnectionStringBuilder_Fail()
        {
            var refresher = new TestImplementation();
            TestAssert.Throws<ArgumentNullException>(() => refresher.Populate(null));
        }

        [TestMethod]
        public async Task AuthenticationWithTokenRefresh_GetTokenAsync_NewTtl_Ok()
        {
            TimeSpan ttl = TimeSpan.FromSeconds(1);

            var refresher = new TestImplementation(ttl, 90);
            await refresher.GetTokenAsync(TestIotHubName);

            DateTime expectedExpiryTime = DateTime.UtcNow.AddSeconds(ttl.TotalSeconds);
            int timeDelta = (int)((refresher.ExpiresOn - expectedExpiryTime).TotalSeconds);
            Assert.IsTrue(Math.Abs(timeDelta) < 3, $"Expiration time delta is {timeDelta}");

            // Wait for the token to expire;
            while (!refresher.IsExpiring)
            {
                await Task.Delay(100);
            }

            // Configure the test token refresher to ignore the suggested TTL.
            ttl = TimeSpan.FromSeconds(10);
            refresher.ActualTimeToLive = (int)ttl.TotalSeconds;

            await refresher.GetTokenAsync(TestIotHubName);

            expectedExpiryTime = DateTime.UtcNow.AddSeconds(ttl.TotalSeconds);
            timeDelta = (int)((refresher.ExpiresOn - expectedExpiryTime).TotalSeconds);
            Assert.IsTrue(Math.Abs(timeDelta) < 3, $"Expiration time delta is {timeDelta}");
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

        private class TestImplementation : AuthenticationWithTokenRefresh
        {
            private const int DefaultBufferPercentage = 15;
            private static readonly TimeSpan DefaultTimeToLiveSeconds = TimeSpan.FromHours(1);

            private int _callCount = 0;

            public int SafeCreateNewTokenCallCount
            {
                get
                {
                    return _callCount;
                }
            }

            public int ActualTimeToLive { get; set; } = 0;

            public TestImplementation() : base(DefaultTimeToLiveSeconds, DefaultBufferPercentage)
            {
            }

            public TestImplementation(TimeSpan suggestedTimeToLive, int timeBufferPercentage)
                : base(suggestedTimeToLive, timeBufferPercentage)
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
