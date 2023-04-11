// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ConnectionStateDelegatingHandlerTests
    {
        [TestMethod]
        public async Task ConnectionStateDelegatingHandler_CloseAsyncBeforeOpen_Ok()
        {
            // arrange
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(1),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };
            using var sut = new ConnectionStateDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act and assert
            await sut.CloseAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConnectionStateDelegatingHandler_OpenAsync_SynchronizesToOneInnerCallToOpen()
        {
            // arrange
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientNoRetry(),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };

            int callCounter = 0;
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCounter++;
                    return Task.CompletedTask;
                });
            // Artificial wait to simulate the transport protocol level async operations.
            // Without this, the transport layer will immediately transition to Closed, which will cause the "Open" flow to be executed again.
            // The internal thread pool will determine how the parallel OpenAsync() calls in act are scheduled.
            nextHandlerMock
                .Setup(x => x.WaitForTransportClosedAsync())
                .Returns(() => Task.Delay(TimeSpan.FromSeconds(5)));

            using var sut = new ConnectionStateDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act
            // Simutaneously run two calls to open
            await Task
                .WhenAll(
                    sut.OpenAsync(CancellationToken.None),
                    sut.OpenAsync(CancellationToken.None))
                .ConfigureAwait(false);

            // assert
            callCounter.Should().Be(1);
        }

        [TestMethod]
        public async Task ConnectionStateDelegatingHandler_OpenAsync_AfterCancelled_CanBeReopened()
        {
            // arrange
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(1),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };

            var nextHandlerMock = new Mock<IDelegatingHandler>();
            bool shouldThrow = true;

            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                    shouldThrow
                        ? throw new OperationCanceledException()
                        : Task.CompletedTask);

            using var sut = new ConnectionStateDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act and assert
            Func<Task> actual = () => sut.OpenAsync(CancellationToken.None);
            await actual
                .Should()
                .ThrowAsync<OperationCanceledException>()
                .ConfigureAwait(false); ;

            // arrange again
            shouldThrow = false;

            // act and assert again
            actual = () => sut.OpenAsync(CancellationToken.None);
            await actual
                .Should()
                .NotThrowAsync()
                .ConfigureAwait(false); ;
        }

        [TestMethod]
        public async Task ConnectionStateDelegatingHandler_OpenAsync_AfterIotHubClientException_CanBeReopened()
        {
            // arrange
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };

            var nextHandlerMock = new Mock<IDelegatingHandler>();
            bool shouldThrow = true;

            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                    shouldThrow
                        ? throw new IotHubClientException()
                        : Task.CompletedTask);

            using var sut = new ConnectionStateDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act and assert
            Func<Task> actual = () => sut.OpenAsync(CancellationToken.None);
            await actual
                .Should()
                .ThrowAsync<IotHubClientException>()
                .ConfigureAwait(false);

            // arrange again
            shouldThrow = false;

            // act and assert again
            actual = () => sut.OpenAsync(CancellationToken.None);
            await actual
                .Should()
                .NotThrowAsync()
                .ConfigureAwait(false); ;
        }

        [TestMethod]
        public async Task ConnectionStateDelegatingHandler_CloseAsync_CancelsPendingOpenAsync()
        {
            // arrange
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };

            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(async (CancellationToken ct) =>
                {
                    // Artificial wait to simulate the transport protocol level operations.
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                });

            using var sut = new ConnectionStateDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act
            Task openTask = sut.OpenAsync(CancellationToken.None);
            await sut.CloseAsync(CancellationToken.None).ConfigureAwait(false);

            // assert

            Func<Task> openFunc = async () => await openTask.ConfigureAwait(false);
            await openFunc.Should().ThrowAsync<OperationCanceledException>();
            nextHandlerMock.Verify(
                x => x.OpenAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ConnectionStateDelegatingHandler_CloseAsync_CancelsPendingSendTelemetryAsync()
        {
            // arrange
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };

            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()))
                .Returns(async (TelemetryMessage message, CancellationToken ct) =>
                {
                    // Artificial wait to simulate the transport protocol level operations.
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                });
            // Artificial wait to simulate the transport protocol level operations.
            nextHandlerMock
                .Setup(x => x.WaitForTransportClosedAsync())
                .Returns(() => Task.Delay(TimeSpan.FromSeconds(5)));

            using var sut = new ConnectionStateDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act
            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            Task sendTelemetryTask = sut.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), CancellationToken.None);
            await sut.CloseAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            Func<Task> sendTelemetryFunc = async () => await sendTelemetryTask.ConfigureAwait(false);
            await sendTelemetryFunc.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            nextHandlerMock.Verify(
                x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ConnectionStateDelegatingHandler_OpenAsyncAfterCloseAsync_Succeeds()
        {
            // arrange
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };

            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);

            using var sut = new ConnectionStateDelegatingHandler(contextMock, nextHandlerMock.Object);

            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await sut.CloseAsync(CancellationToken.None).ConfigureAwait(false);

            // act
            Func<Task> open = () => sut.OpenAsync(CancellationToken.None);

            // assert
            await open.Should().NotThrowAsync().ConfigureAwait(false); ;
        }

        [TestMethod]
        public async Task ConnectionStateDelegatingHandler_SendTelemetryAsyncAfterCloseAsync_Throws()
        {
            // arrange
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };

            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);

            using var sut = new ConnectionStateDelegatingHandler(contextMock, nextHandlerMock.Object);

            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await sut.CloseAsync(CancellationToken.None).ConfigureAwait(false);

            // act
            Func<Task> sendTelemetry = () => sut.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), CancellationToken.None);

            // assert
            await sendTelemetry.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        }
    }
}
