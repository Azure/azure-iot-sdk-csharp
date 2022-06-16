// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Transport;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;

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
            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.OpenAsync(CancellationToken.None).ReturnsForAnyArgs(TaskHelpers.CompletedTask);
            contextMock.TransportSettingsArray = new[] { amqpTransportSettings, mqttTransportSettings };
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);
            sut.ContinuationFactory = (ctx, inner) => innerHandler;
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);

            await innerHandler.Received(1).OpenAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TransportRouting_TryOpenFailedWithUnsupportedException_FailOnFirstTry()
        {
            var contextMock = Substitute.For<PipelineContext>();
            var amqpTransportSettings = Substitute.For<ITransportSettings>();
            var mqttTransportSettings = Substitute.For<ITransportSettings>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            int openCallCounter = 0;
            innerHandler.OpenAsync(CancellationToken.None).ReturnsForAnyArgs(async ci =>
            {
                openCallCounter++;
                await Task.Yield();
                throw new InvalidOperationException();
            });
            contextMock.TransportSettingsArray = new[] { amqpTransportSettings, mqttTransportSettings };
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);
            sut.ContinuationFactory = (ctx, inner) => innerHandler;
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

            var innerHandlers = new List<IDelegatingHandler>(2);
            IDelegatingHandler GetInnerHandler()
            {
                var innerHandler = Substitute.For<IDelegatingHandler>();
                innerHandler.IsUsable.Returns(true);
                innerHandler.OpenAsync(Arg.Any<CancellationToken>()).Returns(async ci =>
                {
                    openCallCounter++;
                    await Task.Yield();
                    innerHandler.IsUsable.Returns(false);
                    throw new TimeoutException();
                });
                innerHandlers.Add(innerHandler);
                return innerHandler;
            }

            contextMock.TransportSettingsArray = new[] { amqpTransportSettings, mqttTransportSettings };
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);
            sut.ContinuationFactory = (ctx, inner) => GetInnerHandler();
            var cancellationToken = new CancellationToken();

            await sut.OpenAsync(cancellationToken).ExpectedAsync<TimeoutException>().ConfigureAwait(false);
            Assert.AreEqual(1, innerHandlers.Count);
            innerHandlers[0].Received(0).Dispose();

            await sut.OpenAsync(cancellationToken).ExpectedAsync<TimeoutException>().ConfigureAwait(false);
            Assert.AreEqual(2, innerHandlers.Count);
            innerHandlers[0].Received(1).Dispose();
            innerHandlers[1].Received(0).Dispose();

            Assert.AreEqual(2, openCallCounter);
        }

        [TestMethod]
        public async Task TransportRouting_TryOpenWhenInnerHandlerNotUsable_Success()
        {
            var contextMock = Substitute.For<PipelineContext>();
            var amqpTransportSettings = Substitute.For<ITransportSettings>();
            var mqttTransportSettings = Substitute.For<ITransportSettings>();
            int openCallCounter = 0;

            var innerHandlers = new List<IDelegatingHandler>(2);
            IDelegatingHandler GetInnerHandler()
            {
                var innerHandler = Substitute.For<IDelegatingHandler>();
                innerHandler.IsUsable.Returns(true);
                innerHandler.OpenAsync(Arg.Any<CancellationToken>()).Returns(async ci =>
                {
                    openCallCounter++;
                    await Task.Yield();
                    innerHandler.IsUsable.Returns(false);
                });
                innerHandlers.Add(innerHandler);
                return innerHandler;
            }

            contextMock.TransportSettingsArray = new[] { amqpTransportSettings, mqttTransportSettings };
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);
            sut.ContinuationFactory = (ctx, inner) => GetInnerHandler();
            var cancellationToken = new CancellationToken();

            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);
            Assert.AreEqual(1, innerHandlers.Count);
            innerHandlers[0].Received(0).Dispose();

            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);
            Assert.AreEqual(2, innerHandlers.Count);
            innerHandlers[0].Received(1).Dispose();
            innerHandlers[1].Received(0).Dispose();

            Assert.AreEqual(2, openCallCounter);
        }

        [TestMethod]
        public async Task TransportRouting_CancellationTokenCanceled_Open()
        {
            var transportSettings = Substitute.For<ITransportSettings>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.OpenAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);
            var contextMock = Substitute.For<PipelineContext>();
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);

            var userDefinedTimeoutCancellationTokenSource = new CancellationTokenSource();
            userDefinedTimeoutCancellationTokenSource.Cancel();

            await sut.OpenAsync(userDefinedTimeoutCancellationTokenSource.Token).ExpectedAsync<OperationCanceledException>().ConfigureAwait(false);
        }
    }
}
