// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Client.Tests.Amqp
{
    [TestClass]
    public class AmqpConnectionPoolTests
    {
        internal class AmqpConnectionPoolTest : AmqpConnectionPool
        {
            private readonly IDictionary<string, AmqpConnectionHolder[]> _dictionaryToUse;

            public AmqpConnectionPoolTest(IDictionary<string, AmqpConnectionHolder[]> dictionaryToUse)
            {
                _dictionaryToUse = dictionaryToUse;
            }

            protected override IDictionary<string, AmqpConnectionHolder[]> GetAmqpSasGroupedPoolDictionary()
            {
                return _dictionaryToUse;
            }
        }

        [TestMethod]
        public void AmqpConnectionPool_Add_Remove_ConnectionHolderIsRemoved()
        {
            string sharedAccessKeyName = "HubOwner";
            uint poolSize = 10;
            IotHubConnectionInfo testDeviceConnectionInfo = CreatePooledSasGroupedDeviceConnectionInfo(sharedAccessKeyName, poolSize);
            IDictionary<string, AmqpConnectionHolder[]> injectedDictionary = new Dictionary<string, AmqpConnectionHolder[]>();

            AmqpConnectionPoolTest pool = new AmqpConnectionPoolTest(injectedDictionary);

            AmqpUnit addedUnit = pool.CreateAmqpUnit(testDeviceConnectionInfo, null, null, null, null, null);

            injectedDictionary[sharedAccessKeyName].Count().Should().Be((int)poolSize);

            pool.RemoveAmqpUnit(addedUnit);

            foreach (object item in injectedDictionary[sharedAccessKeyName])
            {
                item.Should().BeNull();
            }
        }

        private IotHubConnectionInfo CreatePooledSasGroupedDeviceConnectionInfo(string sharedAccessKeyName, uint poolSize)
        {
            Mock<IotHubConnectionInfo> deviceConnectionInfo = new Mock<IotHubConnectionInfo>();

            deviceConnectionInfo.Setup(m => m.IsPooling()).Returns(true);
            deviceConnectionInfo.Setup(m => m.AuthenticationModel).Returns(AuthenticationModel.SasGrouped);
            deviceConnectionInfo.Setup(m => m.SharedAccessKeyName).Returns(sharedAccessKeyName);
            deviceConnectionInfo.Setup(m => m.AmqpTransportSettings).Returns(new IotHubClientAmqpSettings()
            {
                ConnectionPoolSettings = new AmqpConnectionPoolSettings
                {
                    Pooling = true,
                    MaxPoolSize = poolSize,
                }
            });

            return deviceConnectionInfo.Object;
        }
    }
}
