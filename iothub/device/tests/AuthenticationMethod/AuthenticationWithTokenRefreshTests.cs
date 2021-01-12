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
        private const string TestIoTHubName = "contoso.azure-devices.net";
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
            int ttl = 5;
            int buffer = 20;  // Token should refresh after 4 seconds.

            var refresher = new TestImplementation(ttl, buffer);
            await refresher.GetTokenAsync(TestIoTHubName);

            DateTime currentTime = DateTime.UtcNow;
            DateTime expectedExpiryTime = currentTime.AddSeconds(ttl);
            DateTime expectedRefreshTime = expectedExpiryTime.AddSeconds(-((double)buffer / 100) * ttl);

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

            string token1 = await refresher.GetTokenAsync(TestIoTHubName);
            string token2 = await refresher.GetTokenAsync(TestIoTHubName);

            Assert.AreEqual(1, refresher.SafeCreateNewTokenCallCount); // Cached.
            Assert.AreEqual(expectedToken, token1);
            Assert.AreEqual(token1, token2);
        }

        [TestMethod]
        public async Task AuthenticationWithTokenRefresh_Populate_DefaultParameters_Ok()
        {
            var refresher = new TestImplementation();
            var csBuilder = IotHubConnectionStringBuilder.Create(
                TestIoTHubName,
                new ModuleAuthenticationWithRegistrySymmetricKey("deviceId", "moduleid", TestSharedAccessKey));

            refresher.Populate(csBuilder);

            Assert.AreEqual(null, csBuilder.SharedAccessSignature);
            Assert.AreEqual(null, csBuilder.SharedAccessKey);
            Assert.AreEqual(null, csBuilder.SharedAccessKeyName);

            string token = await refresher.GetTokenAsync(TestIoTHubName);

            refresher.Populate(csBuilder);

            Assert.AreEqual(token, csBuilder.SharedAccessSignature);
            Assert.AreEqual(null, csBuilder.SharedAccessKey);
            Assert.AreEqual(null, csBuilder.SharedAccessKeyName);
        }

        [TestMethod]
        public void AuthenticationWithTokenRefresh_Populate_InvalidConnectionStringBuilder_Fail()
        {
            var refresher = new TestImplementation();
            TestAssert.Throws<ArgumentNullException>(() => refresher.Populate(null));
        }

        [TestMethod]
        public async Task AuthenticationWithTokenRefresh_GetTokenAsync_ConcurrentUpdate_Ok()
        {
            var refresher = new TestImplementation();

            var tasks = new Task[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = refresher.GetTokenAsync(TestIoTHubName);
            }

            await Task.WhenAll(tasks);

            Assert.AreEqual(1, refresher.SafeCreateNewTokenCallCount);
        }

        [TestMethod]
        public async Task AuthenticationWithTokenRefresh_GetTokenAsync_NewTtl_Ok()
        {
            int ttl = 1;

            var refresher = new TestImplementation(ttl, 90);
            await refresher.GetTokenAsync(TestIoTHubName);

            DateTime expectedExpiryTime = DateTime.UtcNow.AddSeconds(ttl);
            int timeDelta = (int)((refresher.ExpiresOn - expectedExpiryTime).TotalSeconds);
            Assert.IsTrue(Math.Abs(timeDelta) < 3, $"Expiration time delta is {timeDelta}");

            // Wait for the token to expire;
            while (!refresher.IsExpiring)
            {
                await Task.Delay(100);
            }

            // Configure the test token refresher to ignore the suggested TTL.
            ttl = 10;
            refresher.ActualTimeToLive = ttl;

            await refresher.GetTokenAsync(TestIoTHubName);

            expectedExpiryTime = DateTime.UtcNow.AddSeconds(ttl);
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
            private const int DefaultTimeToLiveSeconds = 1 * 60 * 60;
            private const int DefaultBufferPercentage = 15;

            private int _callCount = 0;

            public int SafeCreateNewTokenCallCount
            {
                get
                {
                    return _callCount;
                }
            }

            public int ActualTimeToLive { get; set; } = 0;

            public TestImplementation() : this(DefaultTimeToLiveSeconds, DefaultBufferPercentage)
            {
            }

            public TestImplementation(int suggestedTimeToLiveSeconds, int timeBufferPercentage)
                : base(suggestedTimeToLiveSeconds, timeBufferPercentage)
            {
            }

            ///<inheritdoc/>
            protected override async Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive)
            {
                _callCount++;

                await Task.Delay(10).ConfigureAwait(false);

                int ttl = suggestedTimeToLive;
                if (ActualTimeToLive > 0)
                {
                    ttl = ActualTimeToLive;
                }

                return CreateToken(ttl);
            }
        }
    }
}
