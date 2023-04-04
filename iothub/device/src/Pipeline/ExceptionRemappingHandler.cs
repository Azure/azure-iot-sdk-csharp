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
    // Except for well-known and expected exception types (e.g. OperationCanceledException, ObjectDisposedException)
    // identified by Fx.IsFatal(), we wish to remap these to an IotHubClientException.
    internal sealed class ExceptionRemappingHandler : DefaultDelegatingHandler
    {
        private static readonly HashSet<Type> s_networkExceptions = new()
        {
            typeof(IOException),
            typeof(SocketException),
            typeof(HttpRequestException),
            typeof(WebException),
            typeof(WebSocketException),
        };

        public ExceptionRemappingHandler(PipelineContext context, IDelegatingHandler innerHandler)
            : base(context, innerHandler)
        {
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithExceptionRemappingAsync(() => base.OpenAsync(cancellationToken));
        }

        public override Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithExceptionRemappingAsync(() => base.EnableReceiveMessageAsync(cancellationToken));
        }

        public override Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithExceptionRemappingAsync(() => base.DisableReceiveMessageAsync(cancellationToken));
        }

        public override Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithExceptionRemappingAsync(() => base.EnableMethodsAsync(cancellationToken));
        }

        public override Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithExceptionRemappingAsync(() => base.DisableMethodsAsync(cancellationToken));
        }

        public override Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithExceptionRemappingAsync(() => base.EnableTwinPatchAsync(cancellationToken));
        }

        public override Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            return ExecuteWithExceptionRemappingAsync(() => base.DisableTwinPatchAsync(cancellationToken));
        }

        public override Task<TwinProperties> GetTwinAsync(CancellationToken cancellationToken)
        {
            return RunWithExceptionRemappingAsync(() => base.GetTwinAsync(cancellationToken));
        }

        public override Task<DateTime> RefreshSasTokenAsync(CancellationToken cancellationToken)
        {
            return RunWithExceptionRemappingAsync(() => base.RefreshSasTokenAsync(cancellationToken));
        }

        public override Task<long> UpdateReportedPropertiesAsync(ReportedProperties reportedProperties, CancellationToken cancellationToken)
        {
            return RunWithExceptionRemappingAsync(() => base.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken));
        }

        public override Task SendTelemetryAsync(IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken)
        {
            return ExecuteWithExceptionRemappingAsync(() => base.SendTelemetryAsync(messages, cancellationToken));
        }

        public override Task SendTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
        {
            return ExecuteWithExceptionRemappingAsync(() => base.SendTelemetryAsync(message, cancellationToken));
        }

        public override Task SendMethodResponseAsync(DirectMethodResponse methodResponse, CancellationToken cancellationToken)
        {
            return ExecuteWithExceptionRemappingAsync(() => base.SendMethodResponseAsync(methodResponse, cancellationToken));
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

        private Task ExecuteWithExceptionRemappingAsync(Func<Task> asyncOperation)
        {
            return RunWithExceptionRemappingAsync(async () =>
            {
                await asyncOperation().ConfigureAwait(false);
                return false;
            });
        }

        private async Task<T> RunWithExceptionRemappingAsync<T>(Func<Task<T>> asyncOperation)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(ExceptionRemappingHandler)}.{nameof(ExecuteWithExceptionRemappingAsync)}");

            try
            {
                return await asyncOperation().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not IotHubClientException && !Fx.IsFatal(ex))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Exception caught: {ex}");

                if (IsNetworkExceptionChain(ex))
                {
                    throw new IotHubClientException("A transient network error occurred; please retry.", IotHubClientErrorCode.NetworkErrors, ex);
                }

                if (IsSecurityExceptionChain(ex))
                {
                    Exception innerException = (ex is IotHubClientException) ? ex.InnerException : ex;
                    throw new IotHubClientException("TLS authentication error.", IotHubClientErrorCode.TlsAuthenticationError, innerException);
                }

                if (Logging.IsEnabled)
                    Logging.Error(this, $"Unmapped exception {ex.GetType()}");

                throw new IotHubClientException("An unexpected exception occurred. See the inner exception for more details.", IotHubClientErrorCode.Unknown, ex);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(ExceptionRemappingHandler)}.{nameof(ExecuteWithExceptionRemappingAsync)}");
            }
        }
    }
}
