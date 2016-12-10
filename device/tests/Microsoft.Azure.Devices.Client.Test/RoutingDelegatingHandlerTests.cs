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
    public class RoutingDelegatingHandlerTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        [TestCategory("Owner [mtuchkov]")]
        public async Task TransportRouting_FirstTrySucceed_Open()
        {
            var contextMock = Substitute.For<IPipelineContext>();
            var amqpTransportSettings = Substitute.For<ITransportSettings>();
            var mqttTransportSettings = Substitute.For<ITransportSettings>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.OpenAsync(Arg.Is(false), Arg.Any<CancellationToken>()).Returns(TaskConstants.Completed);
            contextMock.Get<ITransportSettings[]>().Returns(new[] { amqpTransportSettings, mqttTransportSettings });
            var sut = new ProtocolRoutingDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandler;
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(false, cancellationToken);
            
            await innerHandler.Received(1).OpenAsync(Arg.Is(false), Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        [TestCategory("Owner [mtuchkov]")]
        public async Task TransportRouting_TryOpenFailedWithUnsupportedException_FailOnFirstTry()
        {
            var contextMock = Substitute.For<IPipelineContext>();
            var amqpTransportSettings = Substitute.For<ITransportSettings>();
            var mqttTransportSettings = Substitute.For<ITransportSettings>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.CloseAsync().Returns(TaskConstants.Completed);
            int openCallCounter = 0;
            innerHandler.OpenAsync(Arg.Is(false), Arg.Any<CancellationToken>()).Returns(async ci =>
            {
                openCallCounter++;
                await Task.Yield();
                throw new InvalidOperationException();
            });
            contextMock.Get<ITransportSettings[]>().Returns(new[] { amqpTransportSettings, mqttTransportSettings });
            var sut = new ProtocolRoutingDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandler;
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(Arg.Is(false), cancellationToken).ExpectedAsync<InvalidOperationException>();

            await innerHandler.Received(1).CloseAsync();

            Assert.AreEqual(1, openCallCounter);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        [TestCategory("Owner [mtuchkov]")]
        public async Task TransportRouting_TryOpenFailedWithSupportedExceptionTwoTimes_Fail()
        {
            var contextMock = Substitute.For<IPipelineContext>();
            var amqpTransportSettings = Substitute.For<ITransportSettings>();
            var mqttTransportSettings = Substitute.For<ITransportSettings>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.CloseAsync().Returns(TaskConstants.Completed);
            int openCallCounter = 0;
            innerHandler.OpenAsync(Arg.Is(false), Arg.Any<CancellationToken>()).Returns(async ci =>
            {
                openCallCounter++;
                await Task.Yield();
                throw new TimeoutException();
            });
            contextMock.Get<ITransportSettings[]>().Returns(new[] { amqpTransportSettings, mqttTransportSettings });
            var sut = new ProtocolRoutingDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandler;
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(Arg.Is(false), cancellationToken).ExpectedAsync<IotHubCommunicationException>();

            await innerHandler.Received(2).CloseAsync();

            Assert.AreEqual(2, openCallCounter);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        [TestCategory("Owner [mtuchkov]")]
        public async Task TransportRouting_TryOpenFailedWithSupportedExceptionFirstTimes_SuccessOnSecondTry()
        {
            await TransportRouting_TryOpenFailedWithSupportedExceptionFirstTimes_SuccessOnSecondTry(() => new TimeoutException());
            await TransportRouting_TryOpenFailedWithSupportedExceptionFirstTimes_SuccessOnSecondTry(() => new SocketException(1));
            await TransportRouting_TryOpenFailedWithSupportedExceptionFirstTimes_SuccessOnSecondTry(() => new IotHubCommunicationException(string.Empty));
            await TransportRouting_TryOpenFailedWithSupportedExceptionFirstTimes_SuccessOnSecondTry(() => new AggregateException(new TimeoutException()));
            await TransportRouting_TryOpenFailedWithSupportedExceptionFirstTimes_SuccessOnSecondTry(() => new AggregateException(new SocketException(1)));
            await TransportRouting_TryOpenFailedWithSupportedExceptionFirstTimes_SuccessOnSecondTry(() => new AggregateException(new IotHubCommunicationException(string.Empty)));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        [TestCategory("Owner [jasminel]")]
        public async Task TransportRouting_CancellationTokenCanceled_Open()
        {
            var transportSettings = Substitute.For<ITransportSettings>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.OpenAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(TaskConstants.Completed);
            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new ProtocolRoutingDelegatingHandler(contextMock);

            var userDefinedTimeoutCancellationTokenSource = new CancellationTokenSource();
            userDefinedTimeoutCancellationTokenSource.Cancel();
            await sut.OpenAsync(false, userDefinedTimeoutCancellationTokenSource.Token);

            await innerHandler.Received(0).OpenAsync(Arg.Any<bool>(), userDefinedTimeoutCancellationTokenSource.Token);
        }
        
        static async Task TransportRouting_TryOpenFailedWithSupportedExceptionFirstTimes_SuccessOnSecondTry(Func<Exception> exceptionFactory)
        {
            var contextMock = Substitute.For<IPipelineContext>();
            var amqpTransportSettings = Substitute.For<ITransportSettings>();
            var mqttTransportSettings = Substitute.For<ITransportSettings>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.CloseAsync().Returns(TaskConstants.Completed);
            int openCallCounter = 0;
            innerHandler.OpenAsync(Arg.Is(false), Arg.Any<CancellationToken>()).Returns(async ci =>
            {
                openCallCounter++;
                await Task.Yield();
                if (openCallCounter == 1)
                {
                    throw exceptionFactory();
                }
            });
            contextMock.Get<ITransportSettings[]>().Returns(new[] { amqpTransportSettings, mqttTransportSettings });
            var sut = new ProtocolRoutingDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandler;
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(Arg.Is(false), cancellationToken);

            await innerHandler.Received(1).CloseAsync();

            Assert.AreEqual(2, openCallCounter);
        }
    }
}