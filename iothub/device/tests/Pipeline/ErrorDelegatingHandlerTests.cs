// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.Azure.Devices.Client.Test
{
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
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.Transport;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;

    [TestClass]
    [TestCategory("Unit")]
    public class ErrorDelegatingHandlerTests
    {
        internal static readonly HashSet<Type> NonTransientExceptions = new HashSet<Type>
        {
            typeof(MessageTooLargeException),
            typeof(DeviceMessageLockLostException),
            typeof(UnauthorizedException),
            typeof(DeviceNotFoundException),
            typeof(QuotaExceededException),
            typeof(IotHubClientException),
        };

        private const string ErrorMessage = "Error occurred.";

        private static readonly Dictionary<Type, Func<Exception>> ExceptionFactory = new Dictionary<Type, Func<Exception>>
        {
            { typeof(UnauthorizedException), () => new UnauthorizedException(ErrorMessage) },
            { typeof(DeviceNotFoundException), () => new DeviceNotFoundException(ErrorMessage) },
            { typeof(QuotaExceededException), () => new QuotaExceededException(ErrorMessage) },
            { typeof(IotHubCommunicationException), () => new IotHubCommunicationException(ErrorMessage) },
            { typeof(MessageTooLargeException), () => new MessageTooLargeException(ErrorMessage) },
            { typeof(DeviceMessageLockLostException), () => new DeviceMessageLockLostException(ErrorMessage) },
            { typeof(ServerBusyException), () => new ServerBusyException(ErrorMessage) },
            { typeof(IotHubClientException), () => new IotHubClientException(ErrorMessage) },
            { typeof(IOException), () => new IOException(ErrorMessage) },
            { typeof(TimeoutException), () => new TimeoutException(ErrorMessage) },
            { typeof(ObjectDisposedException), () => new ObjectDisposedException(ErrorMessage) },
            { typeof(OperationCanceledException), () => new OperationCanceledException(ErrorMessage) },
            { typeof(TaskCanceledException), () => new TaskCanceledException(ErrorMessage) },
            { typeof(IotHubThrottledException), () => new IotHubThrottledException(ErrorMessage, null) },
            { typeof(SocketException), () => new SocketException(1) },
            { typeof(HttpRequestException), () => new HttpRequestException() },
            { typeof(WebException), () => new WebException() },
            { typeof(AmqpException), () => new AmqpException(new Amqp.Framing.Error()) },
            { typeof(WebSocketException), () => new WebSocketException(1) },
            { typeof(TestSecurityException), () => new Exception(
                                                            "Test top level",
                                                            new Exception(
                                                                "Inner exception",
                                                                new AuthenticationException()))
            },
            { typeof(TestDerivedException), () => new TestDerivedException() },
        };

        private static readonly HashSet<Type> s_networkExceptions = new HashSet<Type>
        {
            typeof(IOException),
            typeof(SocketException),
            typeof(TimeoutException),
            typeof(OperationCanceledException),
            typeof(HttpRequestException),
            typeof(WebException),
            typeof(IotHubCommunicationException),
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
            var contextMock = Substitute.For<PipelineContext>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.OpenAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);
            innerHandler.SendEventAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);
            var sut = new ErrorDelegatingHandler(contextMock, innerHandler);

            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);
            await sut.SendEventAsync(new Message(new byte[0]), cancellationToken).ConfigureAwait(false);

            await innerHandler.Received(1).OpenAsync(cancellationToken).ConfigureAwait(false);
            await innerHandler.Received(1).SendEventAsync(Arg.Any<Message>(), cancellationToken).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ErrorHandler_TransientErrorOccuredChannelIsAlive_ChannelIsTheSame()
        {
            foreach (Type exceptionType in s_networkExceptions)
            {
                await TestExceptionThrown(exceptionType, typeof(IotHubCommunicationException)).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task ErrorHandler_SecurityErrorOccured_ChannelIsAborted()
        {
            await TestExceptionThrown(typeof(TestSecurityException), typeof(AuthenticationException)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ErrorHandler_NonTransientErrorOccured_ChannelIsRecreated()
        {
            foreach (Type exceptionType in NonTransientExceptions)
            {
                await TestExceptionThrown(exceptionType, exceptionType).ConfigureAwait(false);
            }
        }

        private static async Task TestExceptionThrown(Type thrownExceptionType, Type expectedExceptionType)
        {
            var message = new Message(new byte[0]);
            var cancellationToken = new CancellationToken();

            await OperationAsync_ExceptionThrownAndThenSucceed_OperationSuccessfullyCompleted(
                di => di.SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>()),
                di => di.SendEventAsync(message, cancellationToken),
                di => di.Received(2).SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>()),
                thrownExceptionType, expectedExceptionType).ConfigureAwait(false);

            IEnumerable<Message> messages = new[] { new Message(new byte[0]) };

            await OperationAsync_ExceptionThrownAndThenSucceed_OperationSuccessfullyCompleted(
                di => di.SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>()),
                di => di.SendEventAsync(message, cancellationToken),
                di => di.Received(2).SendEventAsync(Arg.Is(message), Arg.Any<CancellationToken>()),
                thrownExceptionType, expectedExceptionType).ConfigureAwait(false);

            await OpenAsync_ExceptionThrownAndThenSucceed_SuccessfullyOpened(
                di => di.OpenAsync(Arg.Any<CancellationToken>()),
                di => di.OpenAsync(cancellationToken),
                di => di.Received(2).OpenAsync(Arg.Any<CancellationToken>()),
                thrownExceptionType, expectedExceptionType).ConfigureAwait(false);

            string lockToken = "lockToken";

            await OperationAsync_ExceptionThrownAndThenSucceed_OperationSuccessfullyCompleted(
                di => di.CompleteMessageAsync(Arg.Is(lockToken), Arg.Any<CancellationToken>()),
                di => di.CompleteMessageAsync(lockToken, cancellationToken),
                di => di.Received(2).CompleteMessageAsync(Arg.Is(lockToken), Arg.Any<CancellationToken>()),
                thrownExceptionType, expectedExceptionType).ConfigureAwait(false);

            await OperationAsync_ExceptionThrownAndThenSucceed_OperationSuccessfullyCompleted(
                di => di.AbandonMessageAsync(Arg.Is(lockToken), Arg.Any<CancellationToken>()),
                di => di.AbandonMessageAsync(lockToken, cancellationToken),
                di => di.Received(2).AbandonMessageAsync(Arg.Is(lockToken), Arg.Any<CancellationToken>()),
                thrownExceptionType, expectedExceptionType).ConfigureAwait(false);

            await OperationAsync_ExceptionThrownAndThenSucceed_OperationSuccessfullyCompleted(
                di => di.RejectMessageAsync(Arg.Is(lockToken), Arg.Any<CancellationToken>()),
                di => di.RejectMessageAsync(lockToken, cancellationToken),
                di => di.Received(2).RejectMessageAsync(Arg.Is(lockToken), Arg.Any<CancellationToken>()),
                thrownExceptionType, expectedExceptionType).ConfigureAwait(false);

            await OperationAsync_ExceptionThrownAndThenSucceed_OperationSuccessfullyCompleted(
                di => di.ReceiveMessageAsync(Arg.Any<CancellationToken>()),
                di => di.ReceiveMessageAsync(cancellationToken),
                di => di.Received(2).ReceiveMessageAsync(Arg.Any<CancellationToken>()),
                thrownExceptionType, expectedExceptionType).ConfigureAwait(false);
        }

        private static async Task OperationAsync_ExceptionThrownAndThenSucceed_OperationSuccessfullyCompleted(
            Func<IDelegatingHandler, Task<Message>> mockSetup,
            Func<IDelegatingHandler, Task<Message>> act,
            Func<IDelegatingHandler, Task<Message>> assert,
            Type thrownExceptionType,
            Type expectedExceptionType)
        {
            var contextMock = Substitute.For<PipelineContext>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var sut = new ErrorDelegatingHandler(contextMock, innerHandler);

            //initial OpenAsync to emulate Gatekeeper behavior
            var cancellationToken = new CancellationToken();
            innerHandler.OpenAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);
            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);

            //set initial operation result that throws

            bool[] setup = { false };
            mockSetup(innerHandler).Returns(ci =>
            {
                if (setup[0])
                {
                    return Task.FromResult(new Message());
                }
                throw ExceptionFactory[thrownExceptionType]();
            });

            //act
            await ((Func<Task>)(() => act(sut))).ExpectedAsync(expectedExceptionType).ConfigureAwait(false);

            //override outcome
            setup[0] = true;//otherwise previously setup call will happen and throw;
            mockSetup(innerHandler).Returns(new Message());

            //act
            await act(sut).ConfigureAwait(false);

            //assert
            await innerHandler.Received(1).OpenAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await assert(innerHandler).ConfigureAwait(false);
        }

        private static async Task OperationAsync_ExceptionThrownAndThenSucceed_OperationSuccessfullyCompleted(
            Func<IDelegatingHandler, Task> mockSetup,
            Func<IDelegatingHandler, Task> act,
            Func<IDelegatingHandler, Task> assert,
            Type thrownExceptionType,
            Type expectedExceptionType)
        {
            var contextMock = Substitute.For<PipelineContext>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.OpenAsync(Arg.Any<CancellationToken>()).Returns(TaskHelpers.CompletedTask);
            var sut = new ErrorDelegatingHandler(contextMock, innerHandler);

            //initial OpenAsync to emulate Gatekeeper behavior
            var cancellationToken = new CancellationToken();
            await sut.OpenAsync(cancellationToken).ConfigureAwait(false);

            //set initial operation result that throws

            bool[] setup = { false };
            mockSetup(innerHandler).Returns(ci =>
            {
                if (setup[0])
                {
                    return TaskHelpers.CompletedTask; ;
                }
                throw ExceptionFactory[thrownExceptionType]();
            });

            //act
            await ((Func<Task>)(() => act(sut))).ExpectedAsync(expectedExceptionType).ConfigureAwait(false);

            //override outcome
            setup[0] = true;//otherwise previously setup call will happen and throw;
            mockSetup(innerHandler).Returns(TaskHelpers.CompletedTask);

            //act
            await act(sut).ConfigureAwait(false);

            //assert
            await innerHandler.Received(1).OpenAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await assert(innerHandler).ConfigureAwait(false);
        }

        private static async Task OpenAsync_ExceptionThrownAndThenSucceed_SuccessfullyOpened(
            Func<IDelegatingHandler, Task> mockSetup,
            Func<IDelegatingHandler, Task> act,
            Func<IDelegatingHandler, Task> assert,
            Type thrownExceptionType,
            Type expectedExceptionType)
        {
            var contextMock = Substitute.For<PipelineContext>();
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var sut = new ErrorDelegatingHandler(contextMock, innerHandler);

            //set initial operation result that throws

            bool[] setup = { false };
            mockSetup(innerHandler).Returns(ci =>
            {
                if (setup[0])
                {
                    return Task.FromResult(Guid.NewGuid());
                }
                throw ExceptionFactory[thrownExceptionType]();
            });

            //act
            await ((Func<Task>)(() => act(sut))).ExpectedAsync(expectedExceptionType).ConfigureAwait(false);

            //override outcome
            setup[0] = true;//otherwise previously setup call will happen and throw;
            mockSetup(innerHandler).Returns(TaskHelpers.CompletedTask);

            //act
            await act(sut).ConfigureAwait(false);

            //assert
            await assert(innerHandler).ConfigureAwait(false);
        }
    }
}
