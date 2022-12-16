// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            IConnectionCredentials testDevice = AmqpConnectionPoolTests.CreatePooledSasGroupedClientIdentity(sharedAccessKeyName);
            IDictionary<string, AmqpConnectionHolder[]> injectedDictionary = new Dictionary<string, AmqpConnectionHolder[]>();
            var amqpSettings = new IotHubClientAmqpSettings
            {
                ConnectionPoolSettings = new AmqpConnectionPoolSettings
                {
                    MaxPoolSize = poolSize,
                    UsePooling = true,
                },
            };

            AmqpConnectionPoolTest pool = new AmqpConnectionPoolTest(injectedDictionary);

            AmqpUnit addedUnit = pool.CreateAmqpUnit(testDevice, null, amqpSettings, null, null, null, null);

            injectedDictionary[sharedAccessKeyName].Length.Should().Be((int)poolSize);

            pool.RemoveAmqpUnit(addedUnit);

            foreach (object item in injectedDictionary[sharedAccessKeyName])
            {
                item.Should().BeNull();
            }
        }

        private static IConnectionCredentials CreatePooledSasGroupedClientIdentity(string sharedAccessKeyName)
        {
            var clientIdentity = new Mock<IConnectionCredentials>();

            clientIdentity.Setup(m => m.SharedAccessKeyName).Returns(sharedAccessKeyName);
            clientIdentity.Setup(m => m.AuthenticationModel).Returns(AuthenticationModel.SasGrouped);
            return clientIdentity.Object;
        }
    }
}
