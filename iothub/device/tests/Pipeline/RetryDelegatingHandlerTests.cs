// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Moq;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class RetryDelegatingHandlerTests
    {
        public const string TestExceptionMessage = "Test exception";

        [TestMethod]
        public async Task RetryDelegatingHandler_OpenAsyncRetries()
        {
            // arrange
            int callCounter = 0;

            var ct = CancellationToken.None;
            PipelineContext contextMock = new PipelineContext();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };

            var nextHandlerMock = new Mock<IDelegatingHandler>();

            nextHandlerMock
                .Setup(x => x.OpenAsync(ct))
                .Returns(() =>
                    {
                        return ++callCounter == 1
                            ? throw new IotHubClientException("Test transient exception", isTransient: true)
                            : Task.CompletedTask;
                    });
            nextHandlerMock
                .Setup(x => x.WaitForTransportClosedAsync())
                .Returns(() => Task.Delay(TimeSpan.FromSeconds(10)));

            var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act
            await retryDelegatingHandler.OpenAsync(ct).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryDelegatingHandler_SendTelemetryAsyncRetries()
        {
            // arrange
            int callCounter = 0;

            PipelineContext contextMock = new PipelineContext();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };

            var nextHandlerMock = new Mock<IDelegatingHandler>();
            var message = new TelemetryMessage(new byte[] { 1, 2, 3 });
            IEnumerable<TelemetryMessage> messages = new[] { message };

            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);
            
            nextHandlerMock
                .Setup(x => x.SendTelemetryBatchAsync(messages, It.IsAny<CancellationToken>()))
                .Returns(() =>
                    {
                        if (++callCounter == 1)
                        {
                            throw new IotHubClientException(TestExceptionMessage, isTransient: true);
                        }
                        return Task.CompletedTask;
                    });

            var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act
            await retryDelegatingHandler.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await retryDelegatingHandler.SendTelemetryBatchAsync(messages, CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryDelegatingHandler_DoesNotRetryOnNotSupportedException()
        {
            // arrange
            int callCounter = 0;
            var message = new TelemetryMessage(new byte[] { 1, 2, 3 });

            var contextMock = new PipelineContext();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { }; // avoid NRE

            var nextHandlerMock = new Mock<IDelegatingHandler>();

            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);

            nextHandlerMock
                .Setup(x => x.SendTelemetryAsync(message, It.IsAny<CancellationToken>()))
                .Returns(() =>
                    {
                        ++callCounter;
                        throw new NotSupportedException(TestExceptionMessage);
                    });

            var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act and assert
            await retryDelegatingHandler.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            var exception = await retryDelegatingHandler
                .SendTelemetryAsync(message, CancellationToken.None)
                .ExpectedAsync<NotSupportedException>()
                .ConfigureAwait(false);
            callCounter.Should().Be(1);
        }

        [TestMethod]
        public async Task RetryOneMessageHasBeenTouchedTransientExceptionOccuredSuccess()
        {
            // arrange
            int callCounter = 0;

            var contextMock = new PipelineContext();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            var message = new TelemetryMessage(new byte[] { 1, 2, 3 });
            IEnumerable<TelemetryMessage> messages = new[] { message };

            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);
            nextHandlerMock
                .Setup(x => x.SendTelemetryBatchAsync((IEnumerable<TelemetryMessage>)messages, It.IsAny<CancellationToken>()))
                .Returns(() =>
                    {
                        if (++callCounter == 1)
                        {
                            throw new IotHubClientException(TestExceptionMessage, isTransient: true);
                        }
                        return Task.CompletedTask;
                    });

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act
            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await sut.SendTelemetryBatchAsync(messages, CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryMessageWithSeekableStreamHasBeenReadTransientExceptionOccuredThrows()
        {
            // arrange
            int callCounter = 0;

            var contextMock = new PipelineContext();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            var message = new TelemetryMessage(new byte[] { 1, 2, 3 });
            IEnumerable<TelemetryMessage> messages = new[] { message };

            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);
            nextHandlerMock
                .Setup(x => x.SendTelemetryBatchAsync(messages, It.IsAny<CancellationToken>()))
                .Returns(() =>
                    {
                        if (++callCounter == 1)
                        {
                            throw new IotHubClientException(TestExceptionMessage, isTransient: true);
                        }
                        return Task.CompletedTask;
                    });

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act
            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await sut.SendTelemetryBatchAsync(messages, CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryNonTransientErrorThrownThrows()
        {
            // arrange
            int callCounter = 0;

            var contextMock = new PipelineContext();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.OpenAsync(CancellationToken.None))
                .Returns(() =>
                {
                    if (++callCounter == 1)
                    {
                        throw new InvalidOperationException("");
                    }
                    return Task.CompletedTask;
                });

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // arrange
            await sut.OpenAsync(CancellationToken.None).ExpectedAsync<InvalidOperationException>().ConfigureAwait(false);

            // act
            callCounter.Should().Be(1);
        }

        [TestMethod]
        public async Task DeviceNotFoundExceptionReturnsDeviceDisabledStatus()
        {
            // arrange
            var contextMock = new PipelineContext();
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => throw new IotHubClientException(IotHubClientErrorCode.DeviceNotFound));

            ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> statusChangeHandler = (c) =>
            {
                connectionStatusInfo = c;
            };

            contextMock.ConnectionStatusChangeHandler = statusChangeHandler;

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act and assert for exception type
            var exception = await sut
                .OpenAsync(CancellationToken.None)
                .ExpectedAsync<IotHubClientException>()
                .ConfigureAwait(false);

            // assert for exception status code
            exception.ErrorCode.Should().Be(IotHubClientErrorCode.DeviceNotFound);

            // assert for connection status
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.Disconnected);
            connectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.DeviceDisabled);
        }

        [TestMethod]
        public async Task OperationCanceledExceptionThrownAfterNumberOfRetriesThrows()
        {
            // arrange
            using var cts = new CancellationTokenSource(100);
            var contextMock = new PipelineContext();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.OpenAsync(cts.Token))
                .Returns(() => throw new IotHubClientException(TestExceptionMessage, isTransient: true));

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act and assert
            Func<Task> open = () => sut.OpenAsync(cts.Token);

            var result = await open.Should()
                .ThrowAsync<OperationCanceledException>()
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledOpen()
        {
            // arrange
            var contextMock = new PipelineContext();
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act and assert
            await sut.OpenAsync(cts.Token).ExpectedAsync<OperationCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledSendEvent()
        {
            // arrange
            var contextMock = new PipelineContext();
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);
            nextHandlerMock
                .Setup(x => x.SendTelemetryBatchAsync(null, CancellationToken.None))
                .Returns(() => Task.CompletedTask);

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);
            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act and assert
            await sut.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), cts.Token).ExpectedAsync<OperationCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledSendEventWithIEnumMessage()
        {
            // arrange
            var contextMock = new PipelineContext();
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { }; // avoid NRE

            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.OpenAsync(CancellationToken.None))
                .Returns(() => Task.CompletedTask);
            nextHandlerMock
                .Setup(x => x.SendTelemetryBatchAsync(null, CancellationToken.None))
                .Returns(() => Task.CompletedTask);

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);
            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            var ct = new CancellationToken(true);
            var telemetry = new List<TelemetryMessage>(0);

            // act and assert
            await sut.SendTelemetryBatchAsync(telemetry, ct).ExpectedAsync<OperationCanceledException>().ConfigureAwait(false);
            nextHandlerMock.Verify(
                x => x.SendTelemetryBatchAsync(It.IsAny<IEnumerable<TelemetryMessage>>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [TestMethod]
        public async Task RetrySetRetryPolicyVerifyInternalsSuccess()
        {
            // arrange
            var contextMock = new PipelineContext();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { }; // avoid NRE

            var nextHandlerMock = new Mock<IDelegatingHandler>();

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            var retryPolicy = new TestRetryPolicy();
            sut.SetRetryPolicy(retryPolicy);

            int nextHandlerCallCounter = 0;

            nextHandlerMock
                .Setup(x => x.OpenAsync(CancellationToken.None))
                .Returns(() =>
                   {
                       nextHandlerCallCounter++;
                       throw new IotHubClientException(IotHubClientErrorCode.NetworkErrors);
                   });

            // act and assert
            var exception = await sut.OpenAsync(CancellationToken.None).ExpectedAsync<IotHubClientException>().ConfigureAwait(false);
            exception.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            nextHandlerCallCounter.Should().Be(2);
            retryPolicy.Counter.Should().Be(2);

            sut.SetRetryPolicy(new IotHubClientNoRetry());

            exception = await sut.OpenAsync(CancellationToken.None).ExpectedAsync<IotHubClientException>().ConfigureAwait(false);
            exception.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            nextHandlerCallCounter.Should().Be(3);
            retryPolicy.Counter.Should().Be(2);
        }

        private class TestRetryPolicy : IIotHubClientRetryPolicy
        {
            public uint Counter { get; private set; }

            public bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryInterval)
            {
                Counter++;
                lastException.Should().BeOfType(typeof(IotHubClientException));
                ((IotHubClientException)lastException).ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);

                retryInterval = TimeSpan.MinValue;
                return Counter < 2;
            }
        }

        private class NotSeekableStream : MemoryStream
        {
            public override bool CanSeek => false;

            public NotSeekableStream(byte[] buffer) : base(buffer)
            {
            }

            public override long Length
            {
                get => throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin loc)
            {
                throw new NotSupportedException();
            }
        }
    }
}
