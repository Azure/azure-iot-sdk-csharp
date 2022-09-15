// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class ErrorDelegatingHandler : DefaultDelegatingHandler
    {
        public ErrorDelegatingHandler(PipelineContext context, IDelegatingHandler innerHandler)
            : base(context, innerHandler)
        {
        }

        private static readonly HashSet<Type> s_networkExceptions = new HashSet<Type>
        {
            typeof(IOException),
            typeof(SocketException),
            typeof(ClosedChannelException),
            typeof(HttpRequestException),
            typeof(WebException),
            typeof(WebSocketException),
        };

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.OpenAsync(cancellationToken));
        }

        public override Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.ReceiveMessageAsync(cancellationToken));
        }

        public override Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.EnableReceiveMessageAsync(cancellationToken));
        }

        // This is to ensure that if device connects over MQTT with CleanSession flag set to false,
        // then any message sent while the device was disconnected is delivered on the callback.
        public override Task EnsurePendingMessagesAreDeliveredAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.EnsurePendingMessagesAreDeliveredAsync(cancellationToken));
        }

        public override Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.DisableReceiveMessageAsync(cancellationToken));
        }

        public override Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.EnableMethodsAsync(cancellationToken));
        }

        public override Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.DisableMethodsAsync(cancellationToken));
        }

        public override Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.EnableEventReceiveAsync(isAnEdgeModule, cancellationToken));
        }

        public override Task DisableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.DisableEventReceiveAsync(isAnEdgeModule, cancellationToken));
        }

        public override Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.EnableTwinPatchAsync(cancellationToken));
        }

        public override Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.DisableTwinPatchAsync(cancellationToken));
        }

        public override Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.SendTwinGetAsync(cancellationToken));
        }

        public override Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.SendTwinPatchAsync(reportedProperties, cancellationToken));
        }

        public override Task AbandonMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.AbandonMessageAsync(lockToken, cancellationToken));
        }

        public override Task CompleteMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.CompleteMessageAsync(lockToken, cancellationToken));
        }

        public override Task RejectMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.RejectMessageAsync(lockToken, cancellationToken));
        }

        public override Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.SendEventAsync(messages, cancellationToken));
        }

        public override Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.SendEventAsync(message, cancellationToken));
        }

        public override Task SendMethodResponseAsync(DirectMethodResponse methodResponse, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.SendMethodResponseAsync(methodResponse, cancellationToken));
        }

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
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                && singleException.HResult == unchecked((int)0x80072F8F)
                // CURLE_SSL_CACERT (60): Peer certificate cannot be authenticated with known CA certificates.
                || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && singleException.HResult == 60
                || singleException is AuthenticationException)
            {
                return true;
            }

            return false;
        }

        private static bool IsNetwork(Exception singleException)
        {
            return s_networkExceptions.Any(baseExceptionType => baseExceptionType.IsInstanceOfType(singleException));
        }

        private Task ExecuteWithErrorHandlingAsync(Func<Task> asyncOperation)
        {
            return ExecuteWithErrorHandlingAsync(async () =>
            {
                await asyncOperation().ConfigureAwait(false);
                return false;
            });
        }

        private async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> asyncOperation)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"{nameof(ErrorDelegatingHandler)}.{nameof(ExecuteWithErrorHandlingAsync)}");

                try
                {
                    return await asyncOperation().ConfigureAwait(false);
                }
                catch (Exception ex) when (!Fx.IsFatal(ex))
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, $"Exception caught: {ex}");

                    if (IsSecurityExceptionChain(ex))
                    {
                        Exception innerException = (ex is IotHubClientException) ? ex.InnerException : ex;
                        throw new AuthenticationException("TLS authentication error.", innerException);
                    }
                    // For historic reasons, part of the Error handling is done within the transport handlers.
                    else if (ex is IotHubClientException hubEx && hubEx.StatusCode is IotHubStatusCode.NetworkErrors)
                    {
                        throw;
                    }
                    else if (IsNetworkExceptionChain(ex))
                    {
                        throw new IotHubClientException("Transient network error occurred, please retry.", IotHubStatusCode.NetworkErrors, ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(ErrorDelegatingHandler)}.{nameof(ExecuteWithErrorHandlingAsync)}");
            }
        }
    }
}
