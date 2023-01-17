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
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };

            var nextHandlerMock = new Mock<IDelegatingHandler>();

            nextHandlerMock
                .Setup(x => x.OpenAsync(ct))
                .Returns(() =>
                    {
                        return ++callCounter == 1
                            ? throw new IotHubClientException("Test transient exception")
                            {
                                IsTransient = true,
                            }
                            : Task.CompletedTask;
                    });
            nextHandlerMock
                .Setup(x => x.WaitForTransportClosedAsync())
                .Returns(() => Task.Delay(TimeSpan.FromSeconds(10)));

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

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

            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };

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
                            throw new IotHubClientException(TestExceptionMessage)
                            {
                                IsTransient = true,
                            };
                        }
                        return Task.CompletedTask;
                    });

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

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

            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };

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

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act and assert
            await retryDelegatingHandler.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            Func<Task> telemetry = () => retryDelegatingHandler.SendTelemetryAsync(message, CancellationToken.None);
            
            var exception = await telemetry.Should()
                .ThrowAsync<NotSupportedException>()
                .ConfigureAwait(false);
            callCounter.Should().Be(1);
        }

        [TestMethod]
        public async Task RetryOneMessageHasBeenTouchedTransientExceptionOccuredSuccess()
        {
            // arrange
            int callCounter = 0;

            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };
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
                            throw new IotHubClientException(TestExceptionMessage)
                            {
                                IsTransient = true,
                            };
                        }
                        return Task.CompletedTask;
                    });

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act
            await retryDelegatingHandler.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await retryDelegatingHandler.SendTelemetryBatchAsync(messages, CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryMessageWithSeekableStreamHasBeenReadTransientExceptionOccuredThrows()
        {
            // arrange
            int callCounter = 0;

            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            var message = new TelemetryMessage(new byte[] { 1, 2, 3 });
            IEnumerable<TelemetryMessage> messages = new[] { message };

            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);
            nextHandlerMock
                .Setup(x => x.SendTelemetryBatchAsync(messages, It.IsAny<CancellationToken>()))
                .Returns(() => ++callCounter == 1
                            ? throw new IotHubClientException(TestExceptionMessage) { IsTransient = true }
                            : Task.CompletedTask);

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act
            await retryDelegatingHandler.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await retryDelegatingHandler.SendTelemetryBatchAsync(messages, CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryNonTransientErrorThrownThrows()
        {
            // arrange
            int callCounter = 0;

            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };
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

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // arrange
            Func<Task> open = () => retryDelegatingHandler.OpenAsync(CancellationToken.None);

            await open.Should()
                .ThrowAsync<InvalidOperationException>()
                .ConfigureAwait(false);

            // act
            callCounter.Should().Be(1);
        }

        [TestMethod]
        public async Task DeviceNotFoundExceptionReturnsDeviceDisabledStatus()
        {
            // arrange
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(1),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => throw new IotHubClientException("", IotHubClientErrorCode.DeviceNotFound));

            ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> statusChangeHandler = (c) =>
            {
                connectionStatusInfo = c;
            };

            contextMock.ConnectionStatusChangeHandler = statusChangeHandler;

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act and assert for exception type
            Func<Task> open = () => retryDelegatingHandler.OpenAsync(CancellationToken.None);

            var exception = await open.Should()
                .ThrowAsync<IotHubClientException>()
                .ConfigureAwait(false);

            // assert for exception status code
            exception.Which.ErrorCode.Should().Be(IotHubClientErrorCode.DeviceNotFound);

            // assert for connection status
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.Disconnected);
            connectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.DeviceDisabled);
        }

        [TestMethod]
        public async Task OperationCanceledExceptionThrownAfterNumberOfRetriesThrows()
        {
            // arrange
            using var cts = new CancellationTokenSource(100);
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientExponentialBackoffRetryPolicy(0, TimeSpan.FromHours(12), true),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.OpenAsync(cts.Token))
                .Returns(() => throw new IotHubClientException(TestExceptionMessage)
                {
                    IsTransient = true,
                });

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act and assert
            Func<Task> open = () => retryDelegatingHandler.OpenAsync(cts.Token);

            var result = await open.Should()
                .ThrowAsync<OperationCanceledException>()
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledOpen()
        {
            // arrange
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };
            var nextHandlerMock = new Mock<IDelegatingHandler>();
            var ct = new CancellationToken(true);
            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act and assert
            Func<Task> open = () => retryDelegatingHandler.OpenAsync(ct);

            await open.Should()
                .ThrowAsync<OperationCanceledException>()
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledSendEvent()
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
            nextHandlerMock
                .Setup(x => x.SendTelemetryBatchAsync(null, CancellationToken.None))
                .Returns(() => Task.CompletedTask);

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);
            await retryDelegatingHandler.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            var ct = new CancellationToken(true);

            // act and assert
            Func<Task> sendTelemetry = () => retryDelegatingHandler.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), ct);

            await sendTelemetry
                .Should().ThrowAsync<OperationCanceledException>()
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RetryCancellationTokenCanceledSendEventWithIEnumMessage()
        {
            // arrange
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };

            var nextHandlerMock = new Mock<IDelegatingHandler>();
            nextHandlerMock
                .Setup(x => x.OpenAsync(CancellationToken.None))
                .Returns(() => Task.CompletedTask);
            nextHandlerMock
                .Setup(x => x.SendTelemetryBatchAsync(null, CancellationToken.None))
                .Returns(() => Task.CompletedTask);

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);
            await retryDelegatingHandler.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            var ct = new CancellationToken(true);
            var telemetry = new List<TelemetryMessage>(0);

            // act and assert
            Func<Task> sendTelemetry = () => retryDelegatingHandler.SendTelemetryBatchAsync(telemetry, ct);

            await sendTelemetry
                .Should().ThrowAsync<OperationCanceledException>()
                .ConfigureAwait(false);

            nextHandlerMock.Verify(
                x => x.SendTelemetryBatchAsync(It.IsAny<IEnumerable<TelemetryMessage>>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [TestMethod]
        public async Task RetrySetRetryPolicyVerifyInternalsSuccess()
        {
            // arrange
            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { }
            };

            var nextHandlerMock = new Mock<IDelegatingHandler>();

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            var retryPolicy = new TestRetryPolicy();
            retryDelegatingHandler.SetRetryPolicy(retryPolicy);

            int nextHandlerCallCounter = 0;

            nextHandlerMock
                .Setup(x => x.OpenAsync(CancellationToken.None))
                .Returns(() =>
                   {
                       nextHandlerCallCounter++;
                       throw new IotHubClientException("", IotHubClientErrorCode.NetworkErrors);
                   });

            // act and assert
            Func<Task> open = () => retryDelegatingHandler.OpenAsync(CancellationToken.None);

            var exception = await open.Should().ThrowAsync<IotHubClientException>().ConfigureAwait(false);
            exception.Which.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            nextHandlerCallCounter.Should().Be(2);
            retryPolicy.Counter.Should().Be(2);

            retryDelegatingHandler.SetRetryPolicy(new IotHubClientNoRetry());

            exception = await open.Should().ThrowAsync<IotHubClientException>().ConfigureAwait(false);
            exception.Which.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            nextHandlerCallCounter.Should().Be(3);
            retryPolicy.Counter.Should().Be(2);
        }

        [TestMethod]
        public async Task RetryDelegatingHandler_RefreshTokenAsync_DoesNotRetryOnNonNetworkErrorCode()
        {
            // arrange
            int callCounter = 0;

            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { },
            };

            var nextHandlerMock = new Mock<IDelegatingHandler>();

            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);

            nextHandlerMock
                .Setup(x => x.RefreshSasTokenAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    if (++callCounter == 1)
                    {
                        throw new IotHubClientException("Device not found", IotHubClientErrorCode.DeviceNotFound);
                    }
                    return Task.FromResult(DateTime.UtcNow);
                });

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act
            await retryDelegatingHandler.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            Func<Task> refreshToken = () => retryDelegatingHandler.RefreshSasTokenAsync(CancellationToken.None);

            // assert
            await refreshToken.Should()
                .ThrowAsync<IotHubClientException>()
                .ConfigureAwait(false);
            callCounter.Should().Be(1);
        }

        [TestMethod]
        public async Task RetryDelegatingHandler_RefreshTokenAsync_Retries()
        {
            // arrange
            int callCounter = 0;

            var contextMock = new PipelineContext
            {
                RetryPolicy = new IotHubClientTestRetryPolicy(2),
                ConnectionStatusChangeHandler = (connectionStatusInfo) => { },
            };

            var nextHandlerMock = new Mock<IDelegatingHandler>();

            nextHandlerMock
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);

            nextHandlerMock
                .Setup(x => x.RefreshSasTokenAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    if (++callCounter == 1)
                    {
                        throw new IotHubClientException("network error", IotHubClientErrorCode.NetworkErrors);
                    }
                    return Task.FromResult(DateTime.UtcNow);
                });

            using var retryDelegatingHandler = new RetryDelegatingHandler(contextMock, nextHandlerMock.Object);

            // act
            await retryDelegatingHandler.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await retryDelegatingHandler.RefreshSasTokenAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            callCounter.Should().Be(2);
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
