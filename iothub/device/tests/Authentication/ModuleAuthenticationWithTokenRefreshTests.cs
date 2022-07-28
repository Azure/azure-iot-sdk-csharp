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
    public class ModuleAuthenticationWithTokenRefreshTests
    {
        private const string TestDeviceId = "TestDeviceID";
        private const string TestModuleId = "TestModuleID";
        private const string TestIoTHubName = "contoso.azure-devices.net";
        private const int DefaultTimeToLiveSeconds = 1 * 60 * 60;
        private static string TestSharedAccessKey;

        static ModuleAuthenticationWithTokenRefreshTests()
        {
            var rnd = new Random();
            var rndBytes = new byte[32];
            rnd.NextBytes(rndBytes);

            TestSharedAccessKey = Convert.ToBase64String(rndBytes);
        }

        [TestMethod]
        public void ModuleAuthenticationWithTokenRefresh_Ctor_WrongArguments_Fail()
        {
            TestAssert.Throws<ArgumentNullException>(() => new TestImplementation(null, TestModuleId));
            TestAssert.Throws<ArgumentNullException>(() => new TestImplementation(TestDeviceId, null));
            TestAssert.Throws<ArgumentNullException>(() => new TestImplementation("   ", TestModuleId));
            TestAssert.Throws<ArgumentNullException>(() => new TestImplementation(TestDeviceId, "  "));
            TestAssert.Throws<ArgumentOutOfRangeException>(() => new TestImplementation(TestDeviceId, TestModuleId, -1, 10));
            TestAssert.Throws<ArgumentOutOfRangeException>(() => new TestImplementation(TestDeviceId, TestModuleId, 60, -1));
            TestAssert.Throws<ArgumentOutOfRangeException>(() => new TestImplementation(TestDeviceId, TestModuleId, 60, 101));
        }

        [TestMethod]
        public void ModuleAuthenticationWithTokenRefresh_Ctor_DefaultsGetProperties_Ok()
        {
            var refresher = new TestImplementation(TestDeviceId, TestModuleId);
            Assert.AreEqual(TestDeviceId, refresher.DeviceId);

            // Until GetTokenAsync, the token is expired.
            DateTime expectedExpiryTime = DateTime.UtcNow.AddSeconds(-DefaultTimeToLiveSeconds);
            int timeDelta = (int)((refresher.ExpiresOn - expectedExpiryTime).TotalSeconds);

            Assert.IsTrue(Math.Abs(timeDelta) < 3, $"Expiration time delta is {timeDelta}");
            Assert.IsTrue(refresher.IsExpiring);
        }

        [TestMethod]
        public async Task ModuleAuthenticationWithTokenRefresh_Populate_DefaultParameters_Ok()
        {
            var refresher = new TestImplementation(TestDeviceId, TestModuleId);
            var csBuilder = new IotHubConnectionStringBuilder(refresher, TestIoTHubName);

            refresher.Populate(csBuilder);

            Assert.AreEqual(TestDeviceId, csBuilder.DeviceId);
            Assert.AreEqual(TestModuleId, csBuilder.ModuleId);
            Assert.AreEqual(null, csBuilder.SharedAccessSignature);
            Assert.AreEqual(null, csBuilder.SharedAccessKey);
            Assert.AreEqual(null, csBuilder.SharedAccessKeyName);

            string token = await refresher.GetTokenAsync(TestIoTHubName);

            refresher.Populate(csBuilder);

            Assert.AreEqual(TestDeviceId, csBuilder.DeviceId);
            Assert.AreEqual(TestModuleId, csBuilder.ModuleId);
            Assert.AreEqual(token, csBuilder.SharedAccessSignature);
            Assert.AreEqual(null, csBuilder.SharedAccessKey);
            Assert.AreEqual(null, csBuilder.SharedAccessKeyName);
        }

        [TestMethod]
        public async Task ModuleAuthenticationWithSakRefresh_SharedAccessKeyConnectionString_HasRefresher()
        {
            var csBuilder = new IotHubConnectionStringBuilder(
                new ModuleAuthenticationWithRegistrySymmetricKey(TestDeviceId, TestModuleId, TestSharedAccessKey),
                TestIoTHubName);

            IotHubConnectionInfo cs = csBuilder.ToIotHubConnectionInfo();

            Assert.IsNotNull(cs.TokenRefresher);
            Assert.IsInstanceOfType(cs.TokenRefresher, typeof(ModuleAuthenticationWithSakRefresh));

            var auth = (IAuthorizationProvider)cs;
            var cbsAuth = new AmqpIotCbsTokenProvider(cs);

            string token1 = await auth.GetPasswordAsync().ConfigureAwait(false);
            CbsToken token2 = await cbsAuth.GetTokenAsync(new Uri("amqp://" + TestIoTHubName), "testAppliesTo", null).ConfigureAwait(false);

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

        private class TestImplementation : ModuleAuthenticationWithTokenRefresh
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

            public TestImplementation(string deviceId, string moduleId) : base(deviceId, moduleId)
            {
            }

            public TestImplementation(
                string deviceId,
                string moduleId,
                int suggestedTimeToLive,
                int timeBufferPercentage)
                : base(deviceId, moduleId, suggestedTimeToLive, timeBufferPercentage)
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
