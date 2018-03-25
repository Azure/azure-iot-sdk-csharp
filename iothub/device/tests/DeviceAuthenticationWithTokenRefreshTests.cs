// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    public class DeviceAuthenticationWithTokenRefreshTests
    {
        private const string TestDeviceId = "TestDeviceID";
        private const string TestIoTHubName = "contoso.azure-devices.net";
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
            TestAssert.Throws<ArgumentNullException>(() => new TestImplementation("   "));
            TestAssert.Throws<ArgumentOutOfRangeException>(() => new TestImplementation(TestDeviceId, -1, 10));
            TestAssert.Throws<ArgumentOutOfRangeException>(() => new TestImplementation(TestDeviceId, 60, -1));
            TestAssert.Throws<ArgumentOutOfRangeException>(() => new TestImplementation(TestDeviceId, 60, 101));
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
            int ttl = 5;
            int buffer = 20;  // Token should refresh after 4 seconds.

            var refresher = new TestImplementation(TestDeviceId, ttl, buffer);
            await refresher.GetTokenAsync(TestIoTHubName);

            DateTime currentTime = DateTime.UtcNow;
            DateTime expectedExpiryTime = currentTime.AddSeconds(ttl);
            DateTime expectedRefreshTime = expectedExpiryTime.AddSeconds(-((double)buffer / 100) * ttl);

            Assert.AreEqual(TestDeviceId, refresher.DeviceId);

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
        public async Task DeviceAuthenticationWithTokenRefresh_NonExpiredToken_GetTokenCached_Ok()
        {
            var refresher = new TestImplementation(TestDeviceId);

            string token1 = await refresher.GetTokenAsync(TestIoTHubName);
            string token2 = await refresher.GetTokenAsync(TestIoTHubName);

            Assert.AreEqual(1, refresher.SafeCreateNewTokenCallCount); // Cached.
            Assert.AreEqual(token1, token2);
        }

        [TestMethod]
        public async Task DeviceAuthenticationWithTokenRefresh_Populate_DefaultParameters_Ok()
        {
            var refresher = new TestImplementation(TestDeviceId);
            var csBuilder = IotHubConnectionStringBuilder.Create(TestIoTHubName, refresher);

            refresher.Populate(csBuilder);

            Assert.AreEqual(TestDeviceId, csBuilder.DeviceId);
            Assert.AreEqual(null, csBuilder.SharedAccessSignature);
            Assert.AreEqual(null, csBuilder.SharedAccessKey);
            Assert.AreEqual(null, csBuilder.SharedAccessKeyName);

            string token = await refresher.GetTokenAsync(TestIoTHubName);

            refresher.Populate(csBuilder);

            Assert.AreEqual(TestDeviceId, csBuilder.DeviceId);
            Assert.AreEqual(token, csBuilder.SharedAccessSignature);
            Assert.AreEqual(null, csBuilder.SharedAccessKey);
            Assert.AreEqual(null, csBuilder.SharedAccessKeyName);
        }
        
        [TestMethod]
        public async Task DeviceAuthenticationWithSakRefresh_SharedAccessKeyConnectionString_HasRefresher()
        {
            var csBuilder = IotHubConnectionStringBuilder.Create(
                TestIoTHubName,
                new DeviceAuthenticationWithRegistrySymmetricKey(TestDeviceId, TestSharedAccessKey));

            IotHubConnectionString cs = csBuilder.ToIotHubConnectionString();

            Assert.IsNotNull(cs.TokenRefresher);
            Assert.IsInstanceOfType(cs.TokenRefresher, typeof(DeviceAuthenticationWithSakRefresh));

            var auth = (IAuthorizationProvider)cs;
            var cbsAuth = (ICbsTokenProvider)cs;

            string token1 = await auth.GetPasswordAsync();
            CbsToken token2 = await cbsAuth.GetTokenAsync(new Uri("amqp://" + TestIoTHubName), "testAppliesTo", null);

            Assert.IsNull(cs.SharedAccessSignature);
            Assert.AreEqual(TestDeviceId, cs.DeviceId);

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

        private class TestImplementation : DeviceAuthenticationWithTokenRefresh
        {
            private int _callCount = 0;

            public int SafeCreateNewTokenCallCount
            {
                get
                {
                    return _callCount;
                }
            }

            public int ActualTimeToLive { get; set; } = 0;

            public TestImplementation(string deviceId) : base(deviceId)
            {
            }

            public TestImplementation(
                string deviceId,
                int suggestedTimeToLive,
                int timeBufferPercentage)
                : base(deviceId, suggestedTimeToLive, timeBufferPercentage)
            {
            }

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