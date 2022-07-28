// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains methods that a convention based device/ module can use to send telemetry to the service,
    /// respond to commands and perform operations on its properties.
    /// </summary>
    internal partial class InternalClient
    {
        internal Func<WritableClientPropertyCollection, Task<ClientPropertyCollection>> _writableClientPropertyUpdateCallback;

        internal PayloadConvention PayloadConvention => _clientOptions.PayloadConvention ?? DefaultPayloadConvention.Instance;

        internal Task SendTelemetryAsync(TelemetryMessage telemetryMessage, CancellationToken cancellationToken)
        {
            if (telemetryMessage == null)
            {
                throw new ArgumentNullException(nameof(telemetryMessage));
            }

            if (telemetryMessage.Telemetry != null)
            {
                telemetryMessage.Telemetry.Convention = PayloadConvention;
                telemetryMessage.PayloadContentEncoding = PayloadConvention.PayloadEncoder.ContentEncoding.WebName;
                telemetryMessage.PayloadContentType = PayloadConvention.PayloadSerializer.ContentType;
            }

            return SendEventAsync(telemetryMessage, cancellationToken);
        }

        internal Task SubscribeToCommandsAsync(Func<CommandRequest, Task<CommandResponse>> callback, CancellationToken cancellationToken)
        {
            // Subscribe to methods default handler internally and use the callback received internally to invoke the user supplied command callback.

            if (callback == null)
            {
                return SetMethodDefaultHandlerAsync(null, null, cancellationToken);
            }

            var methodDefaultCallback = new MethodCallback(async (methodRequest, userContext) =>
            {
                CommandRequest commandRequest;
                if (methodRequest.Name != null
                    && methodRequest.Name.Contains(ConventionBasedConstants.ComponentLevelCommandSeparator))
                {
                    string[] split = methodRequest.Name.Split(ConventionBasedConstants.ComponentLevelCommandSeparator);
                    string componentName = split[0];
                    string commandName = split[1];
                    commandRequest = new CommandRequest(PayloadConvention, commandName, componentName, methodRequest.Data);
                }
                else
                {
                    commandRequest = new CommandRequest(payloadConvention: PayloadConvention, commandName: methodRequest.Name, payload: methodRequest.Data);
                }

                CommandResponse commandResponse = await callback.Invoke(commandRequest).ConfigureAwait(false);
                commandResponse.PayloadConvention = PayloadConvention;
                return commandResponse.GetPayloadAsBytes() != null
                    ? new MethodResponse(commandResponse.GetPayloadAsBytes(), commandResponse.Status)
                    : new MethodResponse(commandResponse.Status);
            });

            // We pass in a null context to the internal API because the updated SubscribeToCommandsAsync API
            // does not require you to pass in a user context.
            // Since SubscribeToCommandsAsync callback is invoked for all command invocations,
            // the user context passed in would be the same for all scenarios.
            // This user context can be set at a class level instead.
            return SetMethodDefaultHandlerAsync(methodDefaultCallback, null, cancellationToken);
        }

        internal async Task<ClientProperties> GetClientTwinPropertiesAsync(CancellationToken cancellationToken)
        {
            try
            {
                ClientPropertiesAsDictionary clientPropertiesDictionary = await InnerHandler
                    .GetClientTwinPropertiesAsync<ClientPropertiesAsDictionary>(cancellationToken)
                    .ConfigureAwait(false);

                return clientPropertiesDictionary.ToClientProperties(PayloadConvention);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        internal async Task<ClientPropertiesUpdateResponse> UpdateClientPropertiesAsync(ClientPropertyCollection clientProperties, CancellationToken cancellationToken)
        {
            if (clientProperties == null || !clientProperties.Any())
            {
                throw new ArgumentException("Pass in a populated ClientPropertyCollection to report.", nameof(clientProperties));
            }

            try
            {
                clientProperties.Convention = PayloadConvention;
                byte[] body = clientProperties.GetPayloadObjectBytes();
                using Stream bodyStream = new MemoryStream(body);

                return await InnerHandler.SendClientTwinPropertyPatchAsync(bodyStream, cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        internal async Task SubscribeToWritablePropertyUpdateRequestsAsync(Func<WritableClientPropertyCollection, Task<ClientPropertyCollection>> callback, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, callback, nameof(SetDesiredPropertyUpdateCallbackAsync));

            // Wait to acquire the _twinSemaphore. This ensures that concurrently invoked SetDesiredPropertyUpdateCallbackAsync calls are invoked in a thread-safe manner.
            await _twinDesiredPropertySemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (callback != null && !_twinPatchSubscribedWithService)
                {
                    await InnerHandler.EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                    _twinPatchSubscribedWithService = true;
                }
                else if (callback == null && _twinPatchSubscribedWithService)
                {
                    await InnerHandler.DisableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                }

                _writableClientPropertyUpdateCallback = callback;
                _desiredPropertyUpdateCallback = null;
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            finally
            {
                _twinDesiredPropertySemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, callback, nameof(SetDesiredPropertyUpdateCallbackAsync));
            }
        }
    }
}
