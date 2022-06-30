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
using Microsoft.Azure.Devices.Client.Tests.ConnectionString;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Client.Tests.Transport
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
        public async Task AmqpTransportHandlerSendEventAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new Message(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerSendEventAsyncMultipleMessagesTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new List<Message>(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerReceiveAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().ReceiveAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerCompleteAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().CompleteAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerAbandonAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().AbandonAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerRejectAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().RejectAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransport_Select_CorrectReceiverLink_ForEdgeModule()
        {
            // Test that we do not call EnableReceiveMessageAsync when we call EnableEventReceiveAsync indicating this is an Edge Module 
            Mock<MoqableAmqpTransportHandler> mockedMockAmqpTransportHandler = new Mock<MoqableAmqpTransportHandler>();

            mockedMockAmqpTransportHandler.Setup(p => p.EnableEventReceiveAsync(true, default)).CallBase();
            mockedMockAmqpTransportHandler.Setup(p => p.EnableReceiveMessageAsync(default)).Returns(Task.FromResult(0));
            
            await mockedMockAmqpTransportHandler.Object.EnableEventReceiveAsync(false, default);

            bool enableReceiveMessageAsyncWasCalled = mockedMockAmqpTransportHandler.Invocations.Where(x => x.Method.Name.Contains("EnableReceiveMessageAsync")).Any();

            enableReceiveMessageAsyncWasCalled.Should().BeFalse();
        }

        [TestMethod]
        public async Task AmqpTransport_Select_CorrectReceiverLink_ForModuleTwin()
        {
            Mock<MoqableAmqpTransportHandler> mockedMockAmqpTransportHandler = new Mock<MoqableAmqpTransportHandler>();

            mockedMockAmqpTransportHandler.Setup(p => p.EnableEventReceiveAsync(false, default)).CallBase();
            mockedMockAmqpTransportHandler.Setup(p => p.EnableReceiveMessageAsync(default)).Returns(Task.FromResult(0));

            await mockedMockAmqpTransportHandler.Object.EnableEventReceiveAsync(false, default);
            
            bool enableReceiveMessageAsyncWasCalled = mockedMockAmqpTransportHandler.Invocations.Where(x => x.Method.Name.Contains("EnableReceiveMessageAsync")).Any();

            enableReceiveMessageAsyncWasCalled.Should().BeTrue();
        }

        [TestMethod]
        [Obsolete]
        public void AmqpTransportHandler_RejectAmqpSettingsChange()
        {
            // Added [Obsolete] attribute to this method to suppress CS0618 message for ConnectionIdleTimeout
            var amqpTransportHandler1 = new AmqpTransportHandler(new PipelineContext(), new IotHubConnectionString(IotHubConnectionStringBuilder.Create(TestConnectionString)), new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 60, new AmqpConnectionPoolSettings()
            {
                Pooling = true,
                MaxPoolSize = 10,
                ConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            }));

            try
            {
                // Try to create a set AmqpTransportHandler with different connection pool settings.
                var amqpTransportHandler2 = new AmqpTransportHandler(new PipelineContext(), new IotHubConnectionString(IotHubConnectionStringBuilder.Create(TestConnectionString)), new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 60, new AmqpConnectionPoolSettings()
                {
                    Pooling = true,
                    MaxPoolSize = 7, // different pool size
                    ConnectionIdleTimeout = TimeSpan.FromMinutes(1)
                }));
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.Contains("AmqpTransportSettings cannot be modified from the initial settings."), "Did not return the correct error message");
            }
        }

        private async Task TestOperationCanceledByToken(Func<CancellationToken, Task> asyncMethod)
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

        private AmqpTransportHandler CreateFromConnectionString()
        {
            return new AmqpTransportHandler(
                new PipelineContext(),
                IotHubConnectionStringExtensions.Parse(TestConnectionString),
                new AmqpTransportSettings(TransportType.Amqp_Tcp_Only));
        }
    }
}
