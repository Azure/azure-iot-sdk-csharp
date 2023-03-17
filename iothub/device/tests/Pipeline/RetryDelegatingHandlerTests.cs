// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
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

            PipelineContext contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });
            IDelegatingHandler innerHandlerMock = Substitute.For<IDelegatingHandler>();

            innerHandlerMock
                .OpenAsync(Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        return ++callCounter == 1
                            ? throw new IotHubException("Test transient exception", isTransient: true)
                            : TaskHelpers.CompletedTask;
                    });
            innerHandlerMock.WaitForTransportClosedAsync().Returns(Task.Delay(TimeSpan.FromSeconds(10)));

            var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            // act
            await retryDelegatingHandler.OpenAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryDelegatingHandler_SendEventAsyncRetries()
        {
            // arrange
            int callCounter = 0;

            PipelineContext contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });
            IDelegatingHandler innerHandlerMock = Substitute.For<IDelegatingHandler>();
            using var message = new Message(new MemoryStream(new byte[] { 1, 2, 3 }));
            innerHandlerMock
                .SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        callCounter++;

                        Message m = t.Arg<Message>();
                        Stream stream = m.GetBodyStream();
                        if (callCounter == 1)
                        {
                            throw new IotHubException(TestExceptionMessage, isTransient: true);
                        }
                        byte[] buffer = new byte[3];
                        stream.Read(buffer, 0, 3);
                        return TaskHelpers.CompletedTask;
                    });

            var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            // act
            await retryDelegatingHandler.SendEventAsync(message, CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryDelegatingHandler_DoesNotRetryOnNotSupportedException()
        {
            // arrange
            int callCounter = 0;

            PipelineContext contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });
            IDelegatingHandler innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var memoryStream = new NotSeekableStream(new byte[] { 1, 2, 3 });
            using var message = new Message(memoryStream);
            innerHandlerMock
                .SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        callCounter++;
                        Message m = t.Arg<Message>();
                        Stream stream = m.GetBodyStream();
                        byte[] buffer = new byte[3];
                        stream.Read(buffer, 0, 3);
                        throw new IotHubException(TestExceptionMessage, isTransient: true);
                    });

            var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            // act
            NotSupportedException exception = await retryDelegatingHandler
                .SendEventAsync(message, CancellationToken.None)
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
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            using var message = new Message(new MemoryStream(new byte[] { 1, 2, 3 }));
            IEnumerable<Message> messages = new[] { message };
            innerHandlerMock
                .SendEventAsync(Arg.Is(messages), Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        Message m = t.Arg<IEnumerable<Message>>().First();
                        Stream stream = m.GetBodyStream();
                        if (++callCounter == 1)
                        {
                            throw new IotHubException(TestExceptionMessage, isTransient: true);
                        }
                        var buffer = new byte[3];
                        stream.Read(buffer, 0, 3);
                        return TaskHelpers.CompletedTask;
                    });

            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            // act
            await sut.SendEventAsync(messages, CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryMessageWithSeekableStreamHasBeenReadTransientExceptionOccuredThrows()
        {
            // arrange
            int callCounter = 0;

            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            using var message = new Message(new MemoryStream(new byte[] { 1, 2, 3 }));
            innerHandlerMock
                .SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        var m = t.Arg<Message>();
                        Stream stream = m.GetBodyStream();
                        var buffer = new byte[3];
                        stream.Read(buffer, 0, 3);
                        if (++callCounter == 1)
                        {
                            throw new IotHubException(TestExceptionMessage, isTransient: true);
                        }
                        return TaskHelpers.CompletedTask;
                    });

            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            // act
            await sut.SendEventAsync(message, CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryNonTransientErrorThrownThrows()
        {
            // arrange
            int callCounter = 0;

            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock
                .OpenAsync(Arg.Any<CancellationToken>())
                .Returns(t =>
                {
                    if (++callCounter == 1)
                    {
                        throw new InvalidOperationException("");
                    }
                    return TaskHelpers.CompletedTask;
                });

            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

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
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(t => throw new DeviceNotFoundException());

            ConnectionStatus? status = null;
            ConnectionStatusChangeReason? statusChangeReason = null;
            ConnectionStatusChangesHandler statusChangeHandler = (s, r) =>
            {
                status = s;
                statusChangeReason = r;
            };

            contextMock.ConnectionStatusChangesHandler = statusChangeHandler;

            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            // act
            await ((Func<Task>)(() => sut
                .OpenAsync(CancellationToken.None)))
                .ExpectedAsync<DeviceNotFoundException>()
                .ConfigureAwait(false);

            // assert
            status.Should().Be(ConnectionStatus.Disconnected);
            statusChangeReason.Should().Be(ConnectionStatusChangeReason.Device_Disabled);
        }

        [TestMethod]
        public async Task RetryTransientErrorThrownAfterNumberOfRetriesThrows()
        {
            // arrange
            using var cts = new CancellationTokenSource(1000);
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock
                .OpenAsync(Arg.Any<CancellationToken>())
                .Returns(t => throw new IotHubException(TestExceptionMessage, isTransient: true));

            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            IotHubException exception = await sut
                .OpenAsync(cts.Token)
                .ExpectedAsync<IotHubException>()
                .ConfigureAwait(false);

            // act

            // assert
            exception.Message.Should().Be(TestExceptionMessage);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledOpen()
        {
            // arrange
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            innerHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);

            var contextMock = Substitute.For<PipelineContext>();
            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            // act and assert
            await sut.OpenAsync(cts.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledSendEvent()
        {
            // arrange
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.SendEventAsync((Message)null, CancellationToken.None).ReturnsForAnyArgs(TaskHelpers.CompletedTask);

            var contextMock = Substitute.For<PipelineContext>();
            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act and assert
            await sut.SendEventAsync(Arg.Any<Message>(), cts.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledSendEventWithIEnumMessage()
        {
            // arrange
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.SendEventAsync((IEnumerable<Message>)null, CancellationToken.None).ReturnsForAnyArgs(TaskHelpers.CompletedTask);

            var contextMock = Substitute.For<PipelineContext>();
            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act
            await sut.SendEventAsync(new List<Message>(), cts.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);

            // assert
            await innerHandlerMock.Received(0).SendEventAsync(new List<Message>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledReceive()
        {
            // arrange
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            using var cts = new CancellationTokenSource();

            cts.Cancel();
            innerHandlerMock.ReceiveAsync(cts.Token).Returns(new Task<Message>(() => new Message(new byte[0])));
            var contextMock = Substitute.For<PipelineContext>();
            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            // act and assert
            await sut.ReceiveAsync(cts.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetrySetRetryPolicyVerifyInternalsSuccess()
        {
            // arrange
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(
                delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });
            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            var retryPolicy = new TestRetryPolicyRetryTwice();
            sut.SetRetryPolicy(retryPolicy);

            int innerHandlerCallCounter = 0;

            innerHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(t =>
               {
                   innerHandlerCallCounter++;
                   throw new IotHubCommunicationException();
               });

            // act and assert
            await sut.OpenAsync(CancellationToken.None).ExpectedAsync<IotHubCommunicationException>().ConfigureAwait(false);
            innerHandlerCallCounter.Should().Be(2);
            retryPolicy.Counter.Should().Be(2);

            var noretry = new NoRetry();
            sut.SetRetryPolicy(noretry);
            await sut.OpenAsync(CancellationToken.None).ExpectedAsync<IotHubCommunicationException>().ConfigureAwait(false);

            innerHandlerCallCounter.Should().Be(3);
            retryPolicy.Counter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledComplete()
        {
            // arrange
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            innerHandlerMock.CompleteAsync(Arg.Any<string>(), cts.Token).Returns(TaskHelpers.CompletedTask);

            var contextMock = Substitute.For<PipelineContext>();
            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            // act and assert
            await sut.CompleteAsync("", cts.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledAbandon()
        {
            // arrange
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.AbandonAsync(null, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(TaskHelpers.CompletedTask);

            var contextMock = Substitute.For<PipelineContext>();
            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act and assert
            await sut
                .AbandonAsync(Arg.Any<string>(), cts.Token)
                .ExpectedAsync<TaskCanceledException>()
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledReject()
        {
            // arrange
            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.RejectAsync(null, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(TaskHelpers.CompletedTask);

            var contextMock = Substitute.For<PipelineContext>();
            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act and assert
            await sut.RejectAsync(Arg.Any<string>(), cts.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CloseAsyncCancelPendingOpenAsync()
        {
            // arrange
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });

            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.CloseAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);
            innerHandlerMock
                .OpenAsync(Arg.Any<CancellationToken>())
                .Returns(async t =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), t.Arg<CancellationToken>());
                });

            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            // act
            Task openAsyncTask = sut.OpenAsync(CancellationToken.None);
            await sut.CloseAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            await openAsyncTask.ExpectedAsync<OperationCanceledException>().ConfigureAwait(false);
            await innerHandlerMock.Received(1).OpenAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CloseAsyncCancelPendingSendEventAsync()
        {
            // arrange
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });

            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            using var message = new Message(new MemoryStream(new byte[] { 1, 2, 3 }));

            innerHandlerMock.CloseAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);
            innerHandlerMock
                .SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>())
                .Returns(async t =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), t.Arg<CancellationToken>());
                });

            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            // act
            Task sendEventAsyncTask = sut.SendEventAsync(message, CancellationToken.None);
            await sut.CloseAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            await sendEventAsyncTask.ExpectedAsync<OperationCanceledException>().ConfigureAwait(false);
            await innerHandlerMock.Received(1).SendEventAsync(message, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OpenAsyncAfterCloseAsyncThrowsObjectDisposedException()
        {
            // arrange
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });

            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);
            innerHandlerMock.CloseAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);

            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await sut.CloseAsync(CancellationToken.None).ConfigureAwait(false);

            // act and assert
            await sut.OpenAsync(CancellationToken.None).ExpectedAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SendEventAsyncAfterCloseAsyncThrowsObjectDisposedException()
        {
            // arrange
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });

            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);
            innerHandlerMock.CloseAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);

            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await sut.CloseAsync(CancellationToken.None).ConfigureAwait(false);

            // act and assert
            await sut.SendEventAsync(Arg.Any<Message>(), CancellationToken.None).ExpectedAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OpenAsyncAfterDisposeThrowsObjectDisposedException()
        {
            // arrange
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });

            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);

            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            sut.Dispose();

            // act and assert
            await sut.OpenAsync(CancellationToken.None).ExpectedAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SendEventAsyncAfterDisposeThrowsObjectDisposedException()
        {
            // arrange
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangesHandler = new ConnectionStatusChangesHandler(delegate (ConnectionStatus status, ConnectionStatusChangeReason reason) { });

            using var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            using var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            sut.Dispose();

            // act and assert
            await sut.SendEventAsync(Arg.Any<Message>(), CancellationToken.None).ExpectedAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        private class TestRetryPolicyRetryTwice : IRetryPolicy
        {
            public int Counter { get; private set; }

            public bool ShouldRetry(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
            {
                Counter++;
                lastException.Should().BeOfType(typeof(IotHubCommunicationException));

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
