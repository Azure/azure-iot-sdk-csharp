﻿// Copyright (c) Microsoft. All rights reserved.
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
    public class RetryDelegatingHandlerTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext testcontext)
        {
            //To avoid too long test executiuon due to multiple retries if something fails
            RetryDelegatingHandler.RetryCount = 3;
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_TransientErrorOccured_Retried()
        {
            int callCounter = 0;

            var contextMock = Substitute.For<IPipelineContext>();
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.OpenAsync(true, Arg.Any<CancellationToken>()).Returns(t =>
            {
                callCounter++;

                if (callCounter == 1)
                {
                    throw new IotHubClientTransientException("");
                }
                return TaskConstants.Completed;
            });

            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;

            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(true, cancellationToken);

            Assert.AreEqual(2, callCounter);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_MessageHasBeenTouchedTransientExceptionOccured_Success()
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
                    throw new IotHubClientTransientException("");
                }
                var buffer = new byte[3];
                stream.Read(buffer, 0, 3);
                return TaskConstants.Completed;
            });

            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;

            var cancellationToken = new CancellationToken();
            await sut.SendEventAsync(message, cancellationToken);

            Assert.AreEqual(2, callCounter);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_MessageHasBeenReadTransientExceptionOccured_Throws()
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
                throw new IotHubClientTransientException("");
            });

            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;

            var cancellationToken = new CancellationToken();
            await sut.SendEventAsync(message, cancellationToken).ExpectedAsync<IotHubClientTransientException>();

            Assert.AreEqual(callCounter, 1);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_OneMessageHasBeenTouchedTransientExceptionOccured_Success()
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
                    throw new IotHubClientTransientException("");
                }
                var buffer = new byte[3];
                stream.Read(buffer, 0, 3);
                return TaskConstants.Completed;
            });

            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;
            var cancellationToken = new CancellationToken();
            await sut.SendEventAsync(messages, cancellationToken);

            Assert.AreEqual(2, callCounter);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_OneMessageHasBeenReadTransientExceptionOccured_Throws()
        {
            int callCounter = 0;

            var contextMock = Substitute.For<IPipelineContext>();
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var memoryStream = new NotSeekableStream(new byte[] {1,2,3});
            var message = new Message(memoryStream);
            var messages = new[] { message };
            innerHandlerMock.SendEventAsync(Arg.Is(messages), Arg.Any<CancellationToken>()).Returns(t =>
            {
                callCounter++;
                Message m = t.Arg<IEnumerable<Message>>().First();
                Stream stream = m.GetBodyStream();
                var buffer = new byte[3];
                stream.Read(buffer, 0, 3);
                throw new IotHubClientTransientException("");
            });

            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;
            var cancellationToken = new CancellationToken();
            await sut.SendEventAsync(messages, cancellationToken).ExpectedAsync<IotHubClientTransientException>();

            Assert.AreEqual(callCounter, 1);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_MessageWithSeekableStreamHasBeenReadTransientExceptionOccured_Throws()
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
                    throw new IotHubClientTransientException("");
                }
                return TaskConstants.Completed;
            });

            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;
            var cancellationToken = new CancellationToken();
            await sut.SendEventAsync(message, cancellationToken);

            Assert.AreEqual(callCounter, 2);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_NonTransientErrorThrown_Throws()
        {
            int callCounter = 0;

            var contextMock = Substitute.For<IPipelineContext>();
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.OpenAsync(true, Arg.Any<CancellationToken>()).Returns(t =>
            {
                callCounter++;

                if (callCounter == 1)
                {
                    throw new InvalidOperationException("");
                }
                return TaskConstants.Completed;
            });

            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(true, cancellationToken).ExpectedAsync<InvalidOperationException>();

            Assert.AreEqual(callCounter, 1);
        }

        [TestMethod]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_TransientErrorThrownAfterNumberOfRetries_Throws()
        {
            var contextMock = Substitute.For<IPipelineContext>();
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.OpenAsync(Arg.Is(true), Arg.Any<CancellationToken>()).Returns(t =>
            {
                throw new IotHubClientTransientException("");
            });

            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(true, cancellationToken).ExpectedAsync<IotHubClientTransientException>();
        }

        [TestMethod]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_CancellationTokenCanceled_Open()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Cancel();
            innerHandlerMock.OpenAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(TaskConstants.Completed);

            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;

            await sut.OpenAsync(true, cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>();
        }

        [TestMethod]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_CancellationTokenCanceled_SendEvent()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.SendEventAsync((Message)null, CancellationToken.None).ReturnsForAnyArgs(TaskConstants.Completed);

            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            await sut.SendEventAsync(Arg.Any<Message>(), cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>();
        }

        [TestMethod]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_CancellationTokenCanceled_SendEventWithIEnumMessage()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.SendEventAsync((IEnumerable<Message>)null, CancellationToken.None).ReturnsForAnyArgs(TaskConstants.Completed);

            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            await sut.SendEventAsync(new List<Message>(), cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>();

            await innerHandlerMock.Received(0).SendEventAsync(new List<Message>(), Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_CancellationTokenCanceled_Receive()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Cancel();
            innerHandlerMock.ReceiveAsync(cancellationTokenSource.Token).Returns(new Task<Message>(() => new Message(new byte[0])));
            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;

            await sut.ReceiveAsync(cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>();
        }

        [TestMethod]
        [TestCategory("DelegatingHandlers")]
        public void Retry_SetRetryPolicyVerifyInternals_Success ()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;

            var exponentialBackoff = new ExponentialBackoff(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10));
            sut.SetRetryPolicy(exponentialBackoff);

            Assert.AreEqual(typeof(RetryDelegatingHandler.IotHubTransientErrorIgnoreStrategy), sut.internalRetryPolicy.ErrorDetectionStrategy.GetType());
            Assert.AreEqual(typeof(RetryDelegatingHandler.IotHubRuntimeOperationRetryStrategy), sut.internalRetryPolicy.RetryStrategy.GetType());
            var iotHubRuntimeOperationRetryStrategy = (RetryDelegatingHandler.IotHubRuntimeOperationRetryStrategy) sut.internalRetryPolicy.RetryStrategy;
            Assert.AreEqual(typeof(TransientFaultHandling.RetryStrategyWrapper), iotHubRuntimeOperationRetryStrategy.retryStrategy.GetType());
            Assert.AreSame(exponentialBackoff, ((TransientFaultHandling.RetryStrategyWrapper)iotHubRuntimeOperationRetryStrategy.retryStrategy).retryPolicy);

            var noretry = new NoRetry();
            sut.SetRetryPolicy(noretry);

            Assert.AreEqual(typeof(RetryDelegatingHandler.IotHubTransientErrorIgnoreStrategy), sut.internalRetryPolicy.ErrorDetectionStrategy.GetType());
            Assert.AreEqual(typeof(RetryDelegatingHandler.IotHubRuntimeOperationRetryStrategy), sut.internalRetryPolicy.RetryStrategy.GetType());
            iotHubRuntimeOperationRetryStrategy = (RetryDelegatingHandler.IotHubRuntimeOperationRetryStrategy)sut.internalRetryPolicy.RetryStrategy;
            Assert.AreEqual(typeof(TransientFaultHandling.RetryStrategyWrapper), iotHubRuntimeOperationRetryStrategy.retryStrategy.GetType());
            Assert.AreSame(noretry, ((TransientFaultHandling.RetryStrategyWrapper)iotHubRuntimeOperationRetryStrategy.retryStrategy).retryPolicy);
        }

        [TestMethod]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_CancellationTokenCanceled_Complete()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Cancel();
            innerHandlerMock.CompleteAsync(Arg.Any<string>(), cancellationTokenSource.Token).Returns(TaskConstants.Completed);

            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;

            await sut.CompleteAsync("", cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>();
        }

        [TestMethod]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_CancellationTokenCanceled_Abandon()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.AbandonAsync(null, CancellationToken.None).ReturnsForAnyArgs(TaskConstants.Completed);

            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            await sut.AbandonAsync(Arg.Any<string>(), cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>();
        }

        [TestMethod]
        [TestCategory("DelegatingHandlers")]
        public async Task Retry_CancellationTokenCanceled_Reject()
        {
            var innerHandlerMock = Substitute.For<IDelegatingHandler>();
            innerHandlerMock.RejectAsync(null, CancellationToken.None).ReturnsForAnyArgs(TaskConstants.Completed);

            var contextMock = Substitute.For<IPipelineContext>();
            var sut = new RetryDelegatingHandler(contextMock);
            sut.ContinuationFactory = c => innerHandlerMock;
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            await sut.RejectAsync(Arg.Any<string>(), cancellationTokenSource.Token).ExpectedAsync<TaskCanceledException>();
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