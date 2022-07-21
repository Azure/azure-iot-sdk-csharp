// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProtocolRoutingDelegatingHandlerTests
    {
        [TestMethod]
        public async Task TransportRouting_FirstTrySucceed_Open()
        {
            var contextMock = Substitute.For<PipelineContext>();
            var amqpTransportSettings = Substitute.For<ITransportSettings>();
            var mqttTransportSettings = Substitute.For<ITransportSettings>();
            var nextHandler = Substitute.For<IDelegatingHandler>();
            nextHandler.OpenAsync(CancellationToken.None).ReturnsForAnyArgs(TaskHelpers.CompletedTask);
            contextMock.TransportSettingsArray = new[] { amqpTransportSettings, mqttTransportSettings };
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);
            sut.ContinuationFactory = (ctx, next) => nextHandler;
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);

            await nextHandler.Received(1).OpenAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TransportRouting_TryOpenFailedWithUnsupportedException_FailOnFirstTry()
        {
            var contextMock = Substitute.For<PipelineContext>();
            var amqpTransportSettings = Substitute.For<ITransportSettings>();
            var mqttTransportSettings = Substitute.For<ITransportSettings>();
            var nextHandler = Substitute.For<IDelegatingHandler>();
            int openCallCounter = 0;
            nextHandler.OpenAsync(CancellationToken.None).ReturnsForAnyArgs(async ci =>
            {
                openCallCounter++;
                await Task.Yield();
                throw new InvalidOperationException();
            });
            contextMock.TransportSettingsArray = new[] { amqpTransportSettings, mqttTransportSettings };
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);
            sut.ContinuationFactory = (ctx, next) => nextHandler;
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(cancellationToken).ExpectedAsync<InvalidOperationException>().ConfigureAwait(false);

            Assert.AreEqual(1, openCallCounter);
        }

        [TestMethod]
        public async Task TransportRouting_TryOpenFailedWithSupportedExceptionTwoTimes_Fail()
        {
            var contextMock = Substitute.For<PipelineContext>();
            var amqpTransportSettings = Substitute.For<ITransportSettings>();
            var mqttTransportSettings = Substitute.For<ITransportSettings>();
            int openCallCounter = 0;

            var nextHandlers = new List<IDelegatingHandler>(2);
            IDelegatingHandler GetNextHandler()
            {
                var nextHandler = Substitute.For<IDelegatingHandler>();
                nextHandler.IsUsable.Returns(true);
                nextHandler.OpenAsync(Arg.Any<CancellationToken>()).Returns(async ci =>
                {
                    openCallCounter++;
                    await Task.Yield();
                    nextHandler.IsUsable.Returns(false);
                    throw new TimeoutException();
                });
                nextHandlers.Add(nextHandler);
                return nextHandler;
            }

            contextMock.TransportSettingsArray = new[] { amqpTransportSettings, mqttTransportSettings };
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);
            sut.ContinuationFactory = (ctx, next) => GetNextHandler();
            var cancellationToken = new CancellationToken();

            await sut.OpenAsync(cancellationToken).ExpectedAsync<TimeoutException>().ConfigureAwait(false);
            Assert.AreEqual(1, nextHandlers.Count);
            nextHandlers[0].Received(0).Dispose();

            await sut.OpenAsync(cancellationToken).ExpectedAsync<TimeoutException>().ConfigureAwait(false);
            Assert.AreEqual(2, nextHandlers.Count);
            nextHandlers[0].Received(1).Dispose();
            nextHandlers[1].Received(0).Dispose();

            Assert.AreEqual(2, openCallCounter);
        }

        [TestMethod]
        public async Task TransportRouting_TryOpenWhenNextHandlerNotUsable_Success()
        {
            var contextMock = Substitute.For<PipelineContext>();
            var amqpTransportSettings = Substitute.For<ITransportSettings>();
            var mqttTransportSettings = Substitute.For<ITransportSettings>();
            int openCallCounter = 0;

            var nextHandlers = new List<IDelegatingHandler>(2);
            IDelegatingHandler GetNextHandler()
            {
                var nextHandler = Substitute.For<IDelegatingHandler>();
                nextHandler.IsUsable.Returns(true);
                nextHandler.OpenAsync(Arg.Any<CancellationToken>()).Returns(async ci =>
                {
                    openCallCounter++;
                    await Task.Yield();
                    nextHandler.IsUsable.Returns(false);
                });
                nextHandlers.Add(nextHandler);
                return nextHandler;
            }

            contextMock.TransportSettingsArray = new[] { amqpTransportSettings, mqttTransportSettings };
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);
            sut.ContinuationFactory = (ctx, next) => GetNextHandler();
            var cancellationToken = new CancellationToken();

            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);
            Assert.AreEqual(1, nextHandlers.Count);
            nextHandlers[0].Received(0).Dispose();

            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);
            Assert.AreEqual(2, nextHandlers.Count);
            nextHandlers[0].Received(1).Dispose();
            nextHandlers[1].Received(0).Dispose();

            Assert.AreEqual(2, openCallCounter);
        }

        [TestMethod]
        public async Task TransportRouting_CancellationTokenCanceled_Open()
        {
            var transportSettings = Substitute.For<ITransportSettings>();
            var nextHandler = Substitute.For<IDelegatingHandler>();
            nextHandler.OpenAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);
            var contextMock = Substitute.For<PipelineContext>();
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);

            var userDefinedTimeoutCancellationTokenSource = new CancellationTokenSource();
            userDefinedTimeoutCancellationTokenSource.Cancel();

            await sut.OpenAsync(userDefinedTimeoutCancellationTokenSource.Token).ExpectedAsync<OperationCanceledException>().ConfigureAwait(false);
        }
    }
}
