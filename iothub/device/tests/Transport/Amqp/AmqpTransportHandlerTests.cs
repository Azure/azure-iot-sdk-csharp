// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Client.Test.Transport
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpTransportHandlerTests
    {
        public const string TestConnectionString = "HostName=Do.Not.Exist;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;SharedAccessKey=dGVzdFN0cmluZzE=";

        [TestMethod]
        public async Task AmqpTransportHandlerOpenAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().OpenAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerSendTelemetryAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendTelemetryAsync(new TelemetryMessage(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerSendTelemetryAsyncMultipleMessagesTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendTelemetryAsync(new List<TelemetryMessage>(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransport_Select_CorrectReceiverLink_ForModuleTwin()
        {
            var mockedMockAmqpTransportHandler = new Mock<MockableAmqpTransportHandler>();

            mockedMockAmqpTransportHandler.Setup(p => p.EnableReceiveMessageAsync(default)).CallBase();
            mockedMockAmqpTransportHandler.Setup(p => p.EnableReceiveMessageAsync(default)).Returns(Task.FromResult(0));

            await mockedMockAmqpTransportHandler.Object.EnableReceiveMessageAsync(default);

            bool enableReceiveMessageAsyncWasCalled = mockedMockAmqpTransportHandler.Invocations.Where(x => x.Method.Name.Contains("EnableReceiveMessageAsync")).Any();

            enableReceiveMessageAsyncWasCalled.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpTransportHandler_RejectAmqpSettingsChange()
        {
            var transportSettings1 = new IotHubClientAmqpSettings
            {
                PrefetchCount = 60,
                ConnectionPoolSettings = new AmqpConnectionPoolSettings
                {
                    UsePooling = true,
                    MaxPoolSize = 10,
                },
            };
            var pipelineContext1 = new PipelineContext
            {
                IotHubConnectionCredentials = new IotHubConnectionCredentials(TestConnectionString),
                IotHubClientTransportSettings = transportSettings1,
            };

            using var amqpTransportHandler1 = new AmqpTransportHandler(
                pipelineContext1,
                transportSettings1);

            try
            {
                var transportSettings2 = new IotHubClientAmqpSettings
                {
                    PrefetchCount = 60,
                    ConnectionPoolSettings = new AmqpConnectionPoolSettings
                    {
                        UsePooling = true,
                        MaxPoolSize = 7, // different pool size
                    },
                };
                var pipelineContext2 = new PipelineContext
                {
                    IotHubConnectionCredentials = new IotHubConnectionCredentials(TestConnectionString),
                    IotHubClientTransportSettings = transportSettings2,
                };

                using var amqpTransportHandler2 = new AmqpTransportHandler(
                    pipelineContext2,
                    transportSettings2);
            }
            catch (ArgumentException ex)
            {
                ex.Message.Should().Contain("AmqpTransportSettings cannot be modified from the initial settings.");
            }
        }

        private static async Task TestOperationCanceledByToken(Func<CancellationToken, Task> asyncMethod)
        {
            using var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            try
            {
                await asyncMethod(tokenSource.Token).ConfigureAwait(false);
                Assert.Fail("Fail to skip execution of this operation using cancellation token.");
            }
            catch (OperationCanceledException) { }
        }

        private static AmqpTransportHandler CreateFromConnectionString()
        {
            var pipelineContext = new PipelineContext
            {
                IotHubConnectionCredentials = new IotHubConnectionCredentials(TestConnectionString),
                IotHubClientTransportSettings = new IotHubClientAmqpSettings(),
            };

            return new AmqpTransportHandler(
                pipelineContext,
                new IotHubClientAmqpSettings());
        }
    }
}
