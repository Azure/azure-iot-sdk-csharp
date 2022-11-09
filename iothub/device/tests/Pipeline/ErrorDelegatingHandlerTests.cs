// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class ErrorDelegatingHandlerTests
    {
        internal static readonly HashSet<Type> s_nonTransientExceptions = new HashSet<Type>
        {
            typeof(IotHubClientException),
        };

        private const string ErrorMessage = "Error occurred.";

        private static readonly Dictionary<Type, Func<Exception>> s_exceptionFactory = new Dictionary<Type, Func<Exception>>
        {
            { typeof(IotHubClientException), () => new IotHubClientException(ErrorMessage) },
            { typeof(IOException), () => new IOException(ErrorMessage) },
            { typeof(ObjectDisposedException), () => new ObjectDisposedException(ErrorMessage) },
            { typeof(OperationCanceledException), () => new OperationCanceledException(ErrorMessage) },
            { typeof(TaskCanceledException), () => new TaskCanceledException(ErrorMessage) },
            { typeof(SocketException), () => new SocketException(1) },
            { typeof(HttpRequestException), () => new HttpRequestException() },
            { typeof(WebException), () => new WebException() },
            { typeof(AmqpException), () => new AmqpException(new Azure.Amqp.Framing.Error()) },
            { typeof(WebSocketException), () => new WebSocketException(1) },
            { typeof(TestSecurityException), () => new Exception("Test top level", new Exception("Inner exception", new AuthenticationException())) },
            { typeof(TestDerivedException), () => new TestDerivedException() },
        };

        private static readonly HashSet<Type> s_networkExceptions = new HashSet<Type>
        {
            typeof(IOException),
            typeof(SocketException),
            typeof(HttpRequestException),
            typeof(WebException),
            typeof(WebSocketException),
            typeof(TestDerivedException),
        };

        public class TestDerivedException : SocketException
        {
        }

        public class TestSecurityException : Exception
        {
            public TestSecurityException()
            {
            }

            public TestSecurityException(string message) : base(message)
            {
            }

            public TestSecurityException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }

        [TestMethod]
        public async Task ErrorHandler_NoErrors_Success()
        {
            var contextMock = new PipelineContext();
            var innerHandler = new Mock<IDelegatingHandler>();
            innerHandler.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            innerHandler.Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var sut = new ErrorDelegatingHandler(contextMock, innerHandler.Object);

            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);
            await sut.SendTelemetryAsync(new TelemetryMessage(new byte[0]), cancellationToken).ConfigureAwait(false);

            innerHandler.Verify(x => x.OpenAsync(cancellationToken), Times.Once);
            innerHandler.Verify(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), cancellationToken), Times.Once);
        }

        [TestMethod]
        public async Task ErrorHandler_TransientErrorOccuredChannelIsAlive_ChannelIsTheSame()
        {
            foreach (Type exceptionType in s_networkExceptions)
            {
                List<IotHubClientException> exceptionList = await TestExceptionThrownAsync(exceptionType).ConfigureAwait(false);

                foreach (Exception ex in exceptionList)
                {
                    var hubEx = ex as IotHubClientException;
                    hubEx.Should().NotBeNull();
                    hubEx.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
                    hubEx.IsTransient.Should().BeTrue();
                }
            }
        }

        [TestMethod]
        public async Task ErrorHandler_SecurityErrorOccured_ChannelIsAborted()
        {
            List<IotHubClientException> exceptionList = await TestExceptionThrownAsync(typeof(TestSecurityException)).ConfigureAwait(false);

            foreach (Exception ex in exceptionList)
            {
                if (ex is IotHubClientException hubEx)
                {
                    hubEx.ErrorCode.Should().Be(IotHubClientErrorCode.TlsAuthenticationError);
                    hubEx.IsTransient.Should().BeFalse();
                }
            }
        }

        [TestMethod]
        public async Task ErrorHandler_NonTransientErrorOccured_ChannelIsRecreated()
        {
            foreach (Type exceptionType in s_nonTransientExceptions)
            {
                await TestExceptionThrownAsync(exceptionType).ConfigureAwait(false);
            }
        }

        private static async Task<List<IotHubClientException>> TestExceptionThrownAsync(Type thrownExceptionType)
        {
            var exceptionList = new List<IotHubClientException>();

            exceptionList.Add(
                await OperationAsync_ExceptionThrownAndThenSucceed_OperationSuccessfullyCompleted(thrownExceptionType).ConfigureAwait(false));

            exceptionList.Add(
                await OpenAsync_ExceptionThrownAndThenSucceed_SuccessfullyOpened(thrownExceptionType).ConfigureAwait(false));

            return exceptionList;
        }

        private static async Task<IotHubClientException> OperationAsync_ExceptionThrownAndThenSucceed_OperationSuccessfullyCompleted(Type thrownExceptionType)
        {
            var contextMock = new PipelineContext();
            var innerHandler = new Mock<IDelegatingHandler>();
            innerHandler.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var sut = new ErrorDelegatingHandler(contextMock, innerHandler.Object);

            //initial OpenAsync to emulate Gatekeeper behavior
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);

            //set initial operation result that throws

            var message = new TelemetryMessage(new byte[0]);
            bool[] setup = { false };
            innerHandler.Setup(x => x.SendTelemetryAsync(message, It.IsAny<CancellationToken>())).Returns(() =>
            {
                if (setup[0])
                {
                    return Task.CompletedTask;
                }
                throw s_exceptionFactory[thrownExceptionType]();
            });

            //act and assert
            Func<Task> telemetry = () => sut.SendTelemetryAsync(message, CancellationToken.None);

            var ex = await telemetry.Should()
                .ThrowAsync<IotHubClientException>()
                .ConfigureAwait(false);

            //override outcome
            setup[0] = true;//otherwise previously setup call will happen and throw;
            innerHandler.Setup(x => x.SendTelemetryAsync(message, It.IsAny<CancellationToken>())).Returns(() => Task.CompletedTask);

            //act
            await sut.SendTelemetryAsync(message, cancellationToken).ConfigureAwait(false);

            //assert
            innerHandler.Verify(x => x.OpenAsync(It.IsAny<CancellationToken>()));
            innerHandler.Verify(x => x.SendTelemetryAsync(message, It.IsAny<CancellationToken>()), Times.Exactly(2));

            return ex.Which;
        }

        private static async Task<IotHubClientException> OpenAsync_ExceptionThrownAndThenSucceed_SuccessfullyOpened(Type thrownExceptionType)
        {
            var contextMock = new PipelineContext();
            var innerHandler = new Mock<IDelegatingHandler>();
            var sut = new ErrorDelegatingHandler(contextMock, innerHandler.Object);

            //set initial operation result that throws

            bool[] setup = { false };
            innerHandler.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>())).Returns(() =>
            {
                if (setup[0])
                {
                    return Task.FromResult(Guid.NewGuid());
                }
                throw s_exceptionFactory[thrownExceptionType]();
            });

            //act
            Func<Task> open = () => sut.OpenAsync(CancellationToken.None);

            var ex = await open.Should()
                .ThrowAsync<IotHubClientException>()
                .ConfigureAwait(false);

            //override outcome
            setup[0] = true;//otherwise previously setup call will happen and throw;
            innerHandler.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>())).Returns(() => Task.CompletedTask);

            //act
            await sut.OpenAsync(CancellationToken.None).ConfigureAwait(false);

            //assert
            innerHandler.Verify(x => x.OpenAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

            return ex.Which;
        }
    }
}
