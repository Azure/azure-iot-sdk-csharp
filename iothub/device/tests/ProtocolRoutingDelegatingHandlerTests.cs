// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Common;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.Transport;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using NSubstitute.ExceptionExtensions;

    [TestClass]
    [TestCategory("Unit")]
    public class ProtocolRoutingDelegatingHandlerTests
    {
        [TestMethod]
        public async Task TransportRouting_FirstTrySucceed_Open()
        {
            var contextMock = Substitute.For<IPipelineContext>();
            var amqpTransportSettings = Substitute.For<ITransportSettings>();
            var mqttTransportSettings = Substitute.For<ITransportSettings>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.OpenAsync(CancellationToken.None).ReturnsForAnyArgs(Task.CompletedTask);
            contextMock.Get<ITransportSettings[]>().Returns(new[] { amqpTransportSettings, mqttTransportSettings });
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);
            sut.ContinuationFactory = (ctx, inner) => innerHandler;
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);
            
            await innerHandler.Received(1).OpenAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TransportRouting_TryOpenFailedWithUnsupportedException_FailOnFirstTry()
        {
            var contextMock = Substitute.For<IPipelineContext>();
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
            contextMock.Get<ITransportSettings[]>().Returns(new[] { amqpTransportSettings, mqttTransportSettings });
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);
            sut.ContinuationFactory = (ctx, inner) => innerHandler;
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(cancellationToken).ExpectedAsync<InvalidOperationException>().ConfigureAwait(false);

            Assert.AreEqual(1, openCallCounter);
        }

        [TestMethod]
        public async Task TransportRouting_TryOpenFailedWithSupportedExceptionTwoTimes_Fail()
        {
            var contextMock = Substitute.For<IPipelineContext>();
            var amqpTransportSettings = Substitute.For<ITransportSettings>();
            var mqttTransportSettings = Substitute.For<ITransportSettings>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            int openCallCounter = 0;
            innerHandler.OpenAsync(Arg.Any<CancellationToken>()).Returns(async ci =>
            {
                openCallCounter++;
                await Task.Yield();
                throw new TimeoutException();
            });
            contextMock.Get<ITransportSettings[]>().Returns(new[] { amqpTransportSettings, mqttTransportSettings });
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);
            sut.ContinuationFactory = (ctx, inner) => innerHandler;
            var cancellationToken = new CancellationToken();

            await sut.OpenAsync(cancellationToken).ExpectedAsync<TimeoutException>().ConfigureAwait(false);
            innerHandler.Received(0).Dispose();

            await sut.OpenAsync(cancellationToken).ExpectedAsync<TimeoutException>().ConfigureAwait(false);
            innerHandler.Received(1).Dispose();

            Assert.AreEqual(2, openCallCounter);
        }

        [TestMethod]
        public async Task TransportRouting_CancellationTokenCanceled_Open()
        {
            var transportSettings = Substitute.For<ITransportSettings>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new ProtocolRoutingDelegatingHandler(contextMock, null);

            var userDefinedTimeoutCancellationTokenSource = new CancellationTokenSource();
            userDefinedTimeoutCancellationTokenSource.Cancel();

            await sut.OpenAsync(userDefinedTimeoutCancellationTokenSource.Token).ExpectedAsync<OperationCanceledException>().ConfigureAwait(false);
        }
    }
}
