// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Common;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.Transport;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;

    [TestClass]
    [TestCategory("Unit")]
    public class RetryDelegatingHandlerTests
    {
        public const string TestExceptionMessage = "Test exception";

        [TestMethod]
        public async Task RetryTransientErrorOccuredRetried()
        {
            int callCounter = 0;

            var contextMock = Substitute.For<IPipelineContext>();
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(t =>
            {
                callCounter++;

                if (callCounter == 1)
                {
                    throw new IotHubException("Test transient exception", isTransient: true);
                }
                return Task.CompletedTask;
            });

            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);

            Assert.AreEqual(2, callCounter);
        }

        [TestMethod]
        public async Task RetryMessageHasBeenTouchedTransientExceptionOccuredSuccess()
        {
            int callCounter = 0;

            var contextMock = Substitute.For<IPipelineContext>();
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var message = new Message(new MemoryStream(new byte[] {1,2,3}));
            innerHandlerMock.SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>()).Returns(t =>
            {
                callCounter++;

                var m = t.Arg<Message>();
                Stream stream = m.GetBodyStream();
                if (callCounter == 1)
                {
                    throw new IotHubException(TestExceptionMessage, isTransient: true);
                }
                var buffer = new byte[3];
                stream.Read(buffer, 0, 3);
                return Task.CompletedTask;
            });

            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            var cancellationToken = new CancellationToken();
            await sut.SendEventAsync(message, cancellationToken).ConfigureAwait(false);

            Assert.AreEqual(2, callCounter);
        }

        [TestMethod]
        public async Task Retry_NonSeekableStream_NotSupportedException()
        {
            int callCounter = 0;

            var contextMock = Substitute.For<IPipelineContext>();
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var memoryStream = new NotSeekableStream(new byte[] {1,2,3});
            var message = new Message(memoryStream);
            innerHandlerMock.SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>()).Returns(t =>
            {
                callCounter++;
                var m = t.Arg<Message>();
                Stream stream = m.GetBodyStream();
                var buffer = new byte[3];
                stream.Read(buffer, 0, 3);
                throw new IotHubException(TestExceptionMessage, isTransient: true);
            });

            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            var cancellationToken = new CancellationToken();
            NotSupportedException exception = await sut.SendEventAsync(message, cancellationToken).ExpectedAsync<NotSupportedException>().ConfigureAwait(false);

            Assert.AreEqual(callCounter, 1);
        }

        [TestMethod]
        public async Task RetryOneMessageHasBeenTouchedTransientExceptionOccuredSuccess()
        {
            int callCounter = 0;

            var contextMock = Substitute.For<IPipelineContext>();
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var message = new Message(new MemoryStream(new byte[] {1,2,3}));
            IEnumerable<Message> messages = new[] { message };
            innerHandlerMock.SendEventAsync(Arg.Is(messages), Arg.Any<CancellationToken>()).Returns(t =>
            {
                callCounter++;

                Message m = t.Arg<IEnumerable<Message>>().First();
                Stream stream = m.GetBodyStream();
                if (callCounter == 1)
                {
                    throw new IotHubException(TestExceptionMessage, isTransient: true);
                }
                var buffer = new byte[3];
                stream.Read(buffer, 0, 3);
                return Task.CompletedTask;
            });

            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            var cancellationToken = new CancellationToken();
            await sut.SendEventAsync(messages, cancellationToken).ConfigureAwait(false);

            Assert.AreEqual(2, callCounter);
        }

        [TestMethod]
        public async Task RetryMessageWithSeekableStreamHasBeenReadTransientExceptionOccuredThrows()
        {
            int callCounter = 0;

            var contextMock = Substitute.For<IPipelineContext>();
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var message = new Message(new MemoryStream(new byte[] {1,2,3}));
            innerHandlerMock.SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>()).Returns(t =>
            {
                callCounter++;
                var m = t.Arg<Message>();
                Stream stream = m.GetBodyStream();
                var buffer = new byte[3];
                stream.Read(buffer, 0, 3);
                if (callCounter == 1)
                {
                    throw new IotHubException(TestExceptionMessage, isTransient: true);
                }
                return Task.CompletedTask;
            });

            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            var cancellationToken = new CancellationToken();
            await sut.SendEventAsync(message, cancellationToken).ConfigureAwait(false);

            Assert.AreEqual(callCounter, 2);
        }

        [TestMethod]
        public async Task RetryNonTransientErrorThrownThrows()
        {
            int callCounter = 0;

            var contextMock = Substitute.For<IPipelineContext>();
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(t =>
            {
                callCounter++;

                if (callCounter == 1)
                {
                    throw new InvalidOperationException("");
                }
                return Task.CompletedTask;
            });

            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(cancellationToken).ExpectedAsync<InvalidOperationException>().ConfigureAwait(false);

            Assert.AreEqual(callCounter, 1);
        }

        [TestMethod]
        public async Task RetryTransientErrorThrownAfterNumberOfRetriesThrows()
        {
            var contextMock = Substitute.For<IPipelineContext>();
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(t =>
            {
                throw new IotHubException(TestExceptionMessage, isTransient: true);
            });

            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            using (var cts = new CancellationTokenSource(100))
            {
                IotHubException exception = await sut.OpenAsync(cts.Token).ExpectedAsync<IotHubException>().ConfigureAwait(false);
                Assert.AreEqual(TestExceptionMessage, exception.Message);
            }
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledOpen()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Cancel();
            innerHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            await sut.OpenAsync(cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledSendEvent()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.SendEventAsync((Message)null, CancellationToken.None).ReturnsForAnyArgs(Task.CompletedTask);

            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            await sut.SendEventAsync(Arg.Any<Message>(), cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledSendEventWithIEnumMessage()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.SendEventAsync((IEnumerable<Message>)null, CancellationToken.None).ReturnsForAnyArgs(Task.CompletedTask);

            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            await sut.SendEventAsync(new List<Message>(), cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);

            await innerHandlerMock.Received(0).SendEventAsync(new List<Message>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledReceive()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Cancel();
            innerHandlerMock.ReceiveAsync(cancellationTokenSource.Token).Returns(new Task<Message>(() => new Message(new byte[0])));
            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            await sut.ReceiveAsync(cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        private class TestRetryPolicy : IRetryPolicy
        {
            public int Counter
            {
                get;
                private set;
            }

            public bool ShouldRetry(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
            {
                Counter++;
                Assert.IsInstanceOfType(lastException, typeof(IotHubCommunicationException));

                retryInterval = TimeSpan.MinValue;
                if (Counter < 2) return true;
                return false;
            }
        }

        [TestMethod]
        public async Task RetrySetRetryPolicyVerifyInternalsSuccess()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            var retryPolicy = new TestRetryPolicy();
            sut.SetRetryPolicy(retryPolicy);

            int innerHandlerCallCounter = 0;

            innerHandlerMock.OpenAsync(Arg.Any<CancellationToken>()).Returns(t =>
               {
                   innerHandlerCallCounter++;
                   return Task.FromException(new IotHubCommunicationException());
               });

            await sut.OpenAsync(CancellationToken.None).ExpectedAsync<IotHubCommunicationException>().ConfigureAwait(false);
            Assert.AreEqual(2, innerHandlerCallCounter);
            Assert.AreEqual(2, retryPolicy.Counter);

            var noretry = new NoRetry();
            sut.SetRetryPolicy(noretry);
            await sut.OpenAsync(CancellationToken.None).ExpectedAsync<IotHubCommunicationException>().ConfigureAwait(false);

            Assert.AreEqual(3, innerHandlerCallCounter);
            Assert.AreEqual(2, retryPolicy.Counter);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledComplete()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Cancel();
            innerHandlerMock.CompleteAsync(Arg.Any<string>(), cancellationTokenSource.Token).Returns(Task.CompletedTask);

            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);

            await sut.CompleteAsync("", cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledAbandon()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.AbandonAsync(null, CancellationToken.None).ReturnsForAnyArgs(Task.CompletedTask);

            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            await sut.AbandonAsync(Arg.Any<string>(), cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledReject()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.RejectAsync(null, CancellationToken.None).ReturnsForAnyArgs(Task.CompletedTask);

            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock, innerHandlerMock);
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            await sut.RejectAsync(Arg.Any<string>(), cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>().ConfigureAwait(false);
        }

        class NotSeekableStream : MemoryStream
        {
            public override bool CanSeek => false;

            public NotSeekableStream(byte[] buffer):base(buffer)
            {
                
            }

            public override long Length
            {
                get { throw new NotSupportedException(); }
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override long Position
            {
                get
                {
                    throw new NotSupportedException();
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public override long Seek(long offset, SeekOrigin loc)
            {
                throw new NotSupportedException();
            }
        }
    }
}