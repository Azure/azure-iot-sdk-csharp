// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests.Amqp
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpCbsSessionHandlerTests
    {
        private const string HostName = "contoso.azure-devices.net";

        [TestMethod]
        public void AmqpCbsSessionHandler_OpenAsync_Ok()
        {
            // arrange
            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);
            EventHandler ConnectionLossHandler = (object sender, EventArgs e) => { };

            var mockAmqpConnection = new Mock<AmqpConnection>();
            using var cbsSessionHandler = new AmqpCbsSessionHandler(tokenCredentialProperties, ConnectionLossHandler);
            var ct = new CancellationToken();

            // act
            Func<Task> act = async () => await cbsSessionHandler.OpenAsync(mockAmqpConnection.Object, ct).ConfigureAwait(false);

        }
    }
}
