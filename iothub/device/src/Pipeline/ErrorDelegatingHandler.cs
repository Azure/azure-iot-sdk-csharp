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
            typeof(HttpRequestException),
            typeof(WebException),
            typeof(WebSocketException),
        };

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.OpenAsync(cancellationToken));
        }

        public override Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.EnableReceiveMessageAsync(cancellationToken));
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

        public override Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.EnableTwinPatchAsync(cancellationToken));
        }

        public override Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.DisableTwinPatchAsync(cancellationToken));
        }

        public override Task<TwinProperties> GetTwinAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.GetTwinAsync(cancellationToken));
        }

        public override Task<long> UpdateReportedPropertiesAsync(ReportedProperties reportedProperties, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken));
        }

        public override Task SendTelemetryBatchAsync(IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.SendTelemetryBatchAsync(messages, cancellationToken));
        }

        public override Task SendTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(() => base.SendTelemetryAsync(message, cancellationToken));
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
                        throw new IotHubClientException("TLS authentication error.", innerException)
                        {
                            ErrorCode = IotHubClientErrorCode.TlsAuthenticationError,
                        };
                    }
                    // For historic reasons, part of the Error handling is done within the transport handlers.
                    else if (ex is IotHubClientException hubEx
                        && hubEx.ErrorCode is IotHubClientErrorCode.NetworkErrors)
                    {
                        throw;
                    }
                    else if (IsNetworkExceptionChain(ex))
                    {
                        throw new IotHubClientException("Transient network error occurred; please retry.", ex)
                        {
                            ErrorCode = IotHubClientErrorCode.NetworkErrors,
                        };
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
