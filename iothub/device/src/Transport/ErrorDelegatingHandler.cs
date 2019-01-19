// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System.Security.Authentication;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class ErrorDelegatingHandler : DefaultDelegatingHandler
    {
        public ErrorDelegatingHandler(IPipelineContext context, IDelegatingHandler innerHandler) : base(context, innerHandler)
        {
        }

        private static readonly HashSet<Type> s_networkExceptions = new HashSet<Type>
        {
            typeof(IOException),
            typeof(SocketException),
            typeof(TimeoutException),
            typeof(OperationCanceledException),
            typeof(HttpRequestException),
            typeof(WebException),
            typeof(AmqpException),
            typeof(WebSocketException),
        };

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.OpenAsync(cancellationToken));
        }

        public override Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.ReceiveAsync(cancellationToken));
        }

        public override Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.ReceiveAsync(timeout, cancellationToken));
        }

        public override Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.EnableMethodsAsync(cancellationToken));
        }

        public override Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.DisableMethodsAsync(cancellationToken));
        }

        public override Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.EnableEventReceiveAsync(cancellationToken));
        }

        public override Task DisableEventReceiveAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.DisableEventReceiveAsync(cancellationToken));
        }

        public override Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.EnableTwinPatchAsync(cancellationToken));
        }
        
        public override Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.SendTwinGetAsync(cancellationToken));
        }
        
        public override Task SendTwinPatchAsync(TwinCollection reportedProperties,  CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.SendTwinPatchAsync(reportedProperties, cancellationToken));
        }

        public override Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.AbandonAsync(lockToken, cancellationToken));
        }

        public override Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.CompleteAsync(lockToken, cancellationToken));
        }

        public override Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.RejectAsync(lockToken, cancellationToken));
        }

        public override Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.SendEventAsync(messages, cancellationToken));
        }

        public override Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.SendEventAsync(message, cancellationToken));
        }

        public override Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.SendMethodResponseAsync(methodResponse, cancellationToken));
        }

        #region Device Streaming
        public override Task EnableStreamsAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.EnableStreamsAsync(cancellationToken));
        }

        public override Task DisableStreamsAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.DisableStreamsAsync(cancellationToken));
        }

        public override Task<DeviceStreamRequest> WaitForDeviceStreamRequestAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.WaitForDeviceStreamRequestAsync(cancellationToken));
        }

        public override Task AcceptDeviceStreamRequestAsync(DeviceStreamRequest request, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.AcceptDeviceStreamRequestAsync(request, cancellationToken));
        }

        public override Task RejectDeviceStreamRequestAsync(DeviceStreamRequest request, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.RejectDeviceStreamRequestAsync(request, cancellationToken));
        }
        #endregion Device Streaming

        private static bool IsNetworkExceptionChain(Exception exceptionChain)
        {
            return exceptionChain.Unwind(true).Any(e => IsNetwork(e) && !IsTlsSecurity(e));
        }

        private static bool IsSecurityExceptionChain(Exception exceptionChain)
        {
            return exceptionChain.Unwind(true).Any(e => IsTlsSecurity(e));
        }

        private static bool IsTlsSecurity(Exception singleException)
        {
            if (// WinHttpException (0x80072F8F): A security error occurred.
                (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && (singleException.HResult == unchecked((int)0x80072F8F))) ||
                // CURLE_SSL_CACERT (60): Peer certificate cannot be authenticated with known CA certificates.
                (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && (singleException.HResult == 60)) ||
                singleException is AuthenticationException)
            {
                return true;
            }

            return false;
        }

        private static bool IsNetwork(Exception singleException)
        {
            return s_networkExceptions.Contains(singleException.GetType());
        }

        private Task ExecuteWithErrorHandlingAsync(Func<Task> asyncOperation)
        {
            return ExecuteWithErrorHandlingAsync<bool>(async () => { await asyncOperation().ConfigureAwait(false); return false; });
        }

        private async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> asyncOperation)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(ErrorDelegatingHandler)}.{nameof(ExecuteWithErrorHandlingAsync)}");

                try
                {
                    return await asyncOperation().ConfigureAwait(false);
                }
                catch (Exception exception) when (!exception.IsFatal())
                {
                    if (Logging.IsEnabled) Logging.Error(this, $"Exception caught: {exception}");

                    if (IsSecurityExceptionChain(exception))
                    {
                        Exception innerException = (exception is IotHubException) ? exception.InnerException : exception;
                        throw new AuthenticationException("TLS authentication error.", innerException);
                    }
                    // For historic reasons, part of the Error handling is done within the transport handlers.
                    else if (exception is IotHubCommunicationException)
                    {
                        throw;
                    }
                    else if (IsNetworkExceptionChain(exception))
                    {
                        throw new IotHubCommunicationException("Transient network error occurred, please retry.", exception);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(ErrorDelegatingHandler)}.{nameof(ExecuteWithErrorHandlingAsync)}");
            }
        }
    }
}
