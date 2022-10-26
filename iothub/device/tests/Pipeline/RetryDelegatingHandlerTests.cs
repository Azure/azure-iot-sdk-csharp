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
using NSubstitute;
using FluentAssertions;

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
            PipelineContext contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };
            IDelegatingHandler nextHandlerMock = Substitute.For<IDelegatingHandler>();

            nextHandlerMock
                .OpenAsync(ct)
                .Returns(t =>
                    {
                        return ++callCounter == 1
                            ? throw new IotHubClientException("Test transient exception", isTransient: true)
                            : Task.CompletedTask;
                    });
            nextHandlerMock.WaitForTransportClosedAsync().Returns(Task.Delay(TimeSpan.FromSeconds(10)));

            var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock);

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

            PipelineContext contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };
            IDelegatingHandler nextHandlerMock = Substitute.For<IDelegatingHandler>();
            var message = new TelemetryMessage(new byte[] { 1, 2, 3 });
            nextHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            nextHandlerMock
                .SendTelemetryAsync(Arg.Is(message), Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        if (++callCounter == 1)
                        {
                            throw new IotHubClientException(TestExceptionMessage, isTransient: true);
                        }
                        return Task.CompletedTask;
                    });

            var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            // act
            await retryDelegatingHandler.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await retryDelegatingHandler.SendTelemetryAsync(message, CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryDelegatingHandler_DoesNotRetryOnNotSupportedException()
        {
            // arrange
            int callCounter = 0;

            PipelineContext contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };
            IDelegatingHandler nextHandlerMock = Substitute.For<IDelegatingHandler>();
            var message = new TelemetryMessage(new byte[] { 1, 2, 3 });
            nextHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            nextHandlerMock
                .SendTelemetryAsync(Arg.Is(message), Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        ++callCounter;
                        throw new NotSupportedException(TestExceptionMessage);
                    });

            var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            // act
            await retryDelegatingHandler.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            var exception = await retryDelegatingHandler
                .SendTelemetryAsync(message, CancellationToken.None)
                .ExpectedAsync<NotSupportedException>()
                .ConfigureAwait(false);

            // assert
            callCounter.Should().Be(1);
        }

        [TestMethod]
        public async Task RetryOneMessageHasBeenTouchedTransientExceptionOccuredSuccess()
        {
            // arrange
            int callCounter = 0;

            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            var message = new TelemetryMessage(new byte[] { 1, 2, 3 });
            IEnumerable<TelemetryMessage> messages = new[] { message };
            nextHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            nextHandlerMock
                .SendTelemetryAsync(Arg.Is(messages), Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        if (++callCounter == 1)
                        {
                            throw new IotHubClientException(TestExceptionMessage, isTransient: true);
                        }
                        return Task.CompletedTask;
                    });

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            // act
            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await sut.SendTelemetryAsync(messages, CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryMessageWithSeekableStreamHasBeenReadTransientExceptionOccuredThrows()
        {
            // arrange
            int callCounter = 0;

            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            var message = new TelemetryMessage(new byte[] { 1, 2, 3 });
            nextHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            nextHandlerMock
                .SendTelemetryAsync(Arg.Is(message), Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        if (++callCounter == 1)
                        {
                            throw new IotHubClientException(TestExceptionMessage, isTransient: true);
                        }
                        return Task.CompletedTask;
                    });

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            // act
            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await sut.SendTelemetryAsync(message, CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryNonTransientErrorThrownThrows()
        {
            // arrange
            int callCounter = 0;

            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            nextHandlerMock
                .OpenAsync(CancellationToken.None)
                .Returns(t =>
                {
                    if (++callCounter == 1)
                    {
                        throw new InvalidOperationException("");
                    }
                    return Task.CompletedTask;
                });

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            // arrange
            await sut.OpenAsync(CancellationToken.None).ExpectedAsync<InvalidOperationException>().ConfigureAwait(false);

            // act
            callCounter.Should().Be(1);
        }

        [TestMethod]
        public async Task DeviceNotFoundExceptionReturnsDeviceDisabledStatus()
        {
            // arrange
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            nextHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(t => throw new IotHubClientException(IotHubClientErrorCode.DeviceNotFound));

            ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> statusChangeHandler = (c) =>
            {
                connectionStatusInfo = c;
            };

            contextMock.ConnectionStatusChangeHandler = statusChangeHandler;

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

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
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            nextHandlerMock
                .OpenAsync(cts.Token)
                .Returns(t => throw new IotHubClientException(TestExceptionMessage, isTransient: true));

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

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
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            nextHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            // act and assert
            await sut.OpenAsync(cts.Token).ExpectedAsync<OperationCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledSendEvent()
        {
            // arrange
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            nextHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            nextHandlerMock.SendTelemetryAsync((TelemetryMessage)null, CancellationToken.None).ReturnsForAnyArgs(Task.CompletedTask);

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);
            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act and assert
            await sut.SendTelemetryAsync(Arg.Any<TelemetryMessage>(), cts.Token).ExpectedAsync<OperationCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledSendEventWithIEnumMessage()
        {
            // arrange
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            nextHandlerMock.OpenAsync(CancellationToken.None).Returns(Task.CompletedTask);
            nextHandlerMock.SendTelemetryAsync((IEnumerable<TelemetryMessage>)null, CancellationToken.None).ReturnsForAnyArgs(Task.CompletedTask);

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);
            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act
            await sut.SendTelemetryAsync(new List<TelemetryMessage>(), cts.Token).ExpectedAsync<OperationCanceledException>().ConfigureAwait(false);

            // assert
            await nextHandlerMock.Received(0).SendTelemetryAsync(new List<TelemetryMessage>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetrySetRetryPolicyVerifyInternalsSuccess()
        {
            // arrange
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionStatusInfo) => { };
            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            var retryPolicy = new TestRetryPolicy();
            sut.SetRetryPolicy(retryPolicy);

            int nextHandlerCallCounter = 0;

            nextHandlerMock.OpenAsync(CancellationToken.None).Returns(t =>
               {
                   nextHandlerCallCounter++;
                   throw new IotHubClientException(IotHubClientErrorCode.NetworkErrors);
               });

            // act and assert
            var exception = await sut.OpenAsync(CancellationToken.None).ExpectedAsync<IotHubClientException>().ConfigureAwait(false);
            exception.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            nextHandlerCallCounter.Should().Be(2);
            retryPolicy.Counter.Should().Be(2);

            var noretry = new IotHubClientNoRetry();
            sut.SetRetryPolicy(noretry);

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
