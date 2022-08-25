﻿// Copyright (c) Microsoft. All rights reserved.
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
using DotNetty.Common.Utilities;

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
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            IDelegatingHandler nextHandlerMock = Substitute.For<IDelegatingHandler>();

            nextHandlerMock
                .OpenAsync(ct)
                .Returns(t =>
                    {
                        return ++callCounter == 1
                            ? throw new IotHubClientException("Test transient exception", isTransient: true)
                            : TaskHelpers.CompletedTask;
                    });
            nextHandlerMock.WaitForTransportClosedAsync().Returns(Task.Delay(TimeSpan.FromSeconds(10)));

            var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            // act
            await retryDelegatingHandler.OpenAsync(ct).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryDelegatingHandler_SendEventAsyncRetries()
        {
            // arrange
            int callCounter = 0;

            PipelineContext contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            IDelegatingHandler nextHandlerMock = Substitute.For<IDelegatingHandler>();
            var message = new Message(new byte[] { 1, 2, 3 });
            nextHandlerMock
                .SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        if (++callCounter == 1)
                        {
                            throw new IotHubClientException(TestExceptionMessage, isTransient: true);
                        }
                        return TaskHelpers.CompletedTask;
                    });

            var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock);

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
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            IDelegatingHandler nextHandlerMock = Substitute.For<IDelegatingHandler>();
            var message = new Message(new byte[] { 1, 2, 3 });
            nextHandlerMock
                .SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        ++callCounter;
                        throw new NotSupportedException(TestExceptionMessage);
                    });

            var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock);

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
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            var message = new Message(new byte[] { 1, 2, 3 });
            IEnumerable<Message> messages = new[] { message };
            nextHandlerMock
                .SendEventAsync(Arg.Is(messages), Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        if (++callCounter == 1)
                        {
                            throw new IotHubClientException(TestExceptionMessage, isTransient: true);
                        }
                        return TaskHelpers.CompletedTask;
                    });

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

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
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            var message = new Message(new byte[] { 1, 2, 3 });
            nextHandlerMock
                .SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>())
                .Returns(t =>
                    {
                        if (++callCounter == 1)
                        {
                            throw new IotHubClientException(TestExceptionMessage, isTransient: true);
                        }
                        return TaskHelpers.CompletedTask;
                    });

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

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
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            nextHandlerMock
                .OpenAsync(Arg.Any<CancellationToken>())
                .Returns(t =>
                {
                    if (++callCounter == 1)
                    {
                        throw new InvalidOperationException("");
                    }
                    return TaskHelpers.CompletedTask;
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
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            nextHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(t => throw new IotHubClientException() { StatusCode = IotHubStatusCode.DeviceNotFound});

            ConnectionInfo connectionInfo = new ConnectionInfo();
            Action<ConnectionInfo> statusChangeHandler = (c) =>
            {
                connectionInfo = c;
            };

            contextMock.ConnectionStatusChangeHandler = statusChangeHandler;

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            // act
            Func<Task> act = async () =>
            {
                await sut
                .OpenAsync(CancellationToken.None).ConfigureAwait(false);
            };

            //assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.StatusCode.Should().Be(IotHubStatusCode.DeviceNotFound);
        }

        [TestMethod]
        public async Task RetryTransientErrorThrownAfterNumberOfRetriesThrows()
        {
            // arrange
            using var cts = new CancellationTokenSource(100);
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            nextHandlerMock
                .OpenAsync(cts.Token)
                .Returns(t => throw new IotHubClientException(TestExceptionMessage, isTransient: true));

            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            // act and assert
            IotHubClientException exception = await sut
                .OpenAsync(cts.Token)
                .ExpectedAsync<IotHubClientException>()
                .ConfigureAwait(false);
            exception.Message.Should().Be(TestExceptionMessage);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledOpen()
        {
            // arrange
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            nextHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);

            var contextMock = Substitute.For<PipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            // act and assert
            await sut.OpenAsync(cts.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledSendEvent()
        {
            // arrange
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            nextHandlerMock.SendEventAsync((Message)null, CancellationToken.None).ReturnsForAnyArgs(TaskHelpers.CompletedTask);

            var contextMock = Substitute.For<PipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act and assert
            await sut.SendEventAsync(Arg.Any<Message>(), cts.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledSendEventWithIEnumMessage()
        {
            // arrange
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            nextHandlerMock.SendEventAsync((IEnumerable<Message>)null, CancellationToken.None).ReturnsForAnyArgs(TaskHelpers.CompletedTask);

            var contextMock = Substitute.For<PipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act
            await sut.SendEventAsync(new List<Message>(), cts.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);

            // assert
            await nextHandlerMock.Received(0).SendEventAsync(new List<Message>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledReceive()
        {
            // arrange
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            using var cts = new CancellationTokenSource();

            cts.Cancel();
            nextHandlerMock.ReceiveMessageAsync(cts.Token).Returns(new Task<Message>(() => new Message(new byte[0])));
            var contextMock = Substitute.For<PipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            // act and assert
            await sut.ReceiveMessageAsync(cts.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetrySetRetryPolicyVerifyInternalsSuccess()
        {
            // arrange
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            var contextMock = Substitute.For<PipelineContext>();
            contextMock.ConnectionStatusChangeHandler = (connectionInfo) => { };
            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            var retryPolicy = new TestRetryPolicy();
            sut.SetRetryPolicy(retryPolicy);

            int nextHandlerCallCounter = 0;

            nextHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(t =>
               {
                   nextHandlerCallCounter++;
                   throw new IotHubClientException(true, IotHubStatusCode.NetworkErrors);
               });

            // act and assert
            await sut.OpenAsync(CancellationToken.None).ExpectedAsync<IotHubClientException>().ConfigureAwait(false);
            nextHandlerCallCounter.Should().Be(2);
            retryPolicy.Counter.Should().Be(2);

            var noretry = new NoRetry();
            sut.SetRetryPolicy(noretry);
            await sut.OpenAsync(CancellationToken.None).ExpectedAsync<IotHubClientException>().ConfigureAwait(false);

            nextHandlerCallCounter.Should().Be(3);
            retryPolicy.Counter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledComplete()
        {
            // arrange
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            const string lockToken = "fakeLockToken";
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            nextHandlerMock.CompleteMessageAsync(lockToken, cts.Token).Returns(TaskHelpers.CompletedTask);

            var contextMock = Substitute.For<PipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);

            // act and assert
            await sut.CompleteMessageAsync(lockToken, cts.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledAbandon()
        {
            // arrange
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            nextHandlerMock.AbandonMessageAsync(null, CancellationToken.None).ReturnsForAnyArgs(TaskHelpers.CompletedTask);

            var contextMock = Substitute.For<PipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act and assert
            await sut
                .AbandonMessageAsync(Arg.Any<string>(), cts.Token)
                .ExpectedAsync<TaskCanceledException>()
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledReject()
        {
            // arrange
            var nextHandlerMock = Substitute.For<IDelegatingHandler>();
            nextHandlerMock.RejectMessageAsync(null, CancellationToken.None).ReturnsForAnyArgs(TaskHelpers.CompletedTask);

            var contextMock = Substitute.For<PipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, nextHandlerMock);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act and assert
            await sut.RejectMessageAsync(Arg.Any<string>(), cts.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        private class TestRetryPolicy : IRetryPolicy
        {
            public int Counter { get; private set; }

            public bool ShouldRetry(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
            {
                Counter++;
                lastException.Should().BeOfType(typeof(IotHubClientException));

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
