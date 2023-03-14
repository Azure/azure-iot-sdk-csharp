﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Client.Tests.Amqp
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpConnectionPoolTests
    {
        internal class AmqpConnectionPoolTest : AmqpConnectionPool
        {
            public AmqpConnectionPoolTest(Dictionary<string, AmqpConnectionHolder[]> dictionaryToUse)
            {
                _amqpSasGroupedPool = dictionaryToUse;
            }
        }

        [TestMethod]
        public void AmqpConnectionPool_Add_Remove_ConnectionHolderIsRemoved()
        {
            const string sharedAccessKeyName = "HubOwner";
            uint poolSize = 10;
            IConnectionCredentials testDevice = CreatePooledSasGroupedClientIdentity(sharedAccessKeyName);
            var injectedDictionary = new Dictionary<string, AmqpConnectionHolder[]>();
            var amqpSettings = new IotHubClientAmqpSettings
            {
                ConnectionPoolSettings = new AmqpConnectionPoolSettings
                {
                    MaxPoolSize = poolSize,
                    UsePooling = true,
                },
            };

            var pool = new AmqpConnectionPoolTest(injectedDictionary);

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
