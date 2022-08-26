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
        private const string TestIotHubName = "contoso.azure-devices.net";
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
            TestAssert.Throws<ArgumentOutOfRangeException>(() => new TestImplementation(TestDeviceId, TestModuleId, TimeSpan.FromSeconds(-1), 10));
            TestAssert.Throws<ArgumentOutOfRangeException>(() => new TestImplementation(TestDeviceId, TestModuleId, TimeSpan.FromSeconds(60), -1));
            TestAssert.Throws<ArgumentOutOfRangeException>(() => new TestImplementation(TestDeviceId, TestModuleId, TimeSpan.FromSeconds(60), 101));
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
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(refresher, TestIotHubName);

            refresher.Populate(iotHubConnectionCredentials);

            Assert.AreEqual(TestDeviceId, iotHubConnectionCredentials.DeviceId);
            Assert.AreEqual(TestModuleId, iotHubConnectionCredentials.ModuleId);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessSignature);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessKey);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessKeyName);

            string token = await refresher.GetTokenAsync(TestIotHubName);

            refresher.Populate(iotHubConnectionCredentials);

            Assert.AreEqual(TestDeviceId, iotHubConnectionCredentials.DeviceId);
            Assert.AreEqual(TestModuleId, iotHubConnectionCredentials.ModuleId);
            Assert.AreEqual(token, iotHubConnectionCredentials.SharedAccessSignature);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessKey);
            Assert.AreEqual(null, iotHubConnectionCredentials.SharedAccessKeyName);
        }

        [TestMethod]
        public async Task ModuleAuthenticationWithSakRefresh_SharedAccessKeyConnectionString_HasRefresher()
        {
            IConnectionCredentials iotHubConnectionCredentials = new IotHubConnectionCredentials(
                new ModuleAuthenticationWithRegistrySymmetricKey(TestDeviceId, TestModuleId, TestSharedAccessKey),
                TestIotHubName);

            Assert.IsNotNull(iotHubConnectionCredentials.SasTokenRefresher);
            Assert.IsInstanceOfType(iotHubConnectionCredentials.SasTokenRefresher, typeof(ModuleAuthenticationWithSakRefresh));

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

        private class TestImplementation : ModuleAuthenticationWithTokenRefresh
        {
            private int _callCount = 0;

            public int SafeCreateNewTokenCallCount => _callCount;

            public int ActualTimeToLive { get; set; }

            public TestImplementation(string deviceId, string moduleId) : base(deviceId, moduleId)
            {
            }

            public TestImplementation(
                string deviceId,
                string moduleId,
                TimeSpan suggestedTimeToLive,
                int timeBufferPercentage)
                : base(deviceId, moduleId, suggestedTimeToLive, timeBufferPercentage)
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
