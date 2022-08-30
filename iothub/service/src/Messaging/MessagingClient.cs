// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> for sending cloud-to-device and cloud-to-module messages.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d"/>.
    public class MessagingClient : IDisposable
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly IotHubConnection _connection;
        private readonly IotHubServiceClientOptions _clientOptions;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;
        private readonly FaultTolerantAmqpObject<SendingAmqpLink> _faultTolerantSendingLink;

        private const string _sendingPath = "/messages/deviceBound";
        private const string PurgeMessageQueueFormat = "/devices/{0}/commands";
        private int _sendingDeliveryTag;

        /// <summary>
        /// The callback to be executed when the connection is lost.
        /// </summary>
        public Action<ErrorContext> ErrorProcessor;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected MessagingClient()
        {
        }

        internal MessagingClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            HttpClient httpClient,
            HttpRequestMessageFactory httpRequestMessageFactory,
            IotHubServiceClientOptions options)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
            _clientOptions = options;
            _connection = new IotHubConnection(credentialProvider, options.Transport == TransportType.WebSocket, options);
            _faultTolerantSendingLink = new FaultTolerantAmqpObject<SendingAmqpLink>(CreateSendingLinkAsync, _connection.CloseLink);
        }

        /// <summary>
        /// Open this instance. Must be done before any cloud-to-device messages can be sent.
        /// </summary>
        /// <exception cref="IotHubCommunicationException">Thrown if the client encounters a transient retriable exception. </exception>
        /// <exception cref="IotHubCommunicationException">Thrown when the operation has been canceled. The inner exception will be
        /// <see cref="OperationCanceledException"/>.</exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="IotHubException">Thrown if an error occurs when communicating with IoT hub service.
        /// If <see cref="IotHubException.IsTransient"/> is set to <c>true</c> then it is a transient exception.
        /// If <see cref="IotHubException.IsTransient"/> is set to <c>false</c> then it is a non-transient exception.</exception>
        public virtual async Task OpenAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Opening MessagingClient", nameof(OpenAsync));
            try
            {
                using var ctx = new CancellationTokenSource();
                await _faultTolerantSendingLink.OpenAsync(ctx.Token).ConfigureAwait(false);
                SendingAmqpLink sendingLink = await GetSendingLinkAsync().ConfigureAwait(false);
                sendingLink.Session.Connection.Closed += OnConnectionClosed;
                sendingLink.Session.Closed += OnConnectionClosed;
                sendingLink.Closed += OnConnectionClosed;
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(OpenAsync)} threw an exception: {ex}", nameof(OpenAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Opening MessagingClient", nameof(OpenAsync));
            }
        }

        /// <summary>
        /// Close this instance.
        /// </summary>
        /// <remarks>
        /// The instance can be re-opened after closing and before disposing.
        /// </remarks>
        /// <exception cref="IotHubCommunicationException">Thrown if the client encounters a transient retriable exception. </exception>
        /// <exception cref="IotHubCommunicationException">Thrown when the operation has been canceled. The inner exception will be
        /// <see cref="OperationCanceledException"/>.</exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="IotHubException">Thrown if an error occurs when communicating with IoT hub service.
        /// If <see cref="IotHubException.IsTransient"/> is set to <c>true</c> then it is a transient exception.
        /// If <see cref="IotHubException.IsTransient"/> is set to <c>false</c> then it is a non-transient exception.</exception>
        public virtual async Task CloseAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Closing MessagingClient", nameof(CloseAsync));

            try
            {
                await _faultTolerantSendingLink.CloseAsync().ConfigureAwait(false);
                await _connection.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(CloseAsync)} threw an exception: {ex}", nameof(CloseAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Closing MessagingClient", nameof(CloseAsync));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Disposing MessagingClient", nameof(Dispose));

            _faultTolerantSendingLink.Dispose();
            _connection.Dispose();

            if (Logging.IsEnabled)
                Logging.Exit(this, $"Disposing MessagingClient", nameof(Dispose));
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Send a cloud-to-device message to the specified device.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="message">The cloud-to-device message.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="message"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided <paramref name="deviceId"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task SendAsync(string deviceId, Message message, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Sending message with Id [{message?.MessageId}] for device {deviceId}", nameof(SendAsync));

            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNull(message, nameof(message));

            if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset && message.MessageId == null)
            {
                message.MessageId = Guid.NewGuid().ToString();
            }

            using AmqpMessage amqpMessage = MessageConverter.MessageToAmqpMessage(message);
            amqpMessage.Properties.To = "/devices/" + WebUtility.UrlEncode(deviceId) + "/messages/deviceBound";

            try
            {
                SendingAmqpLink sendingLink = await GetSendingLinkAsync().ConfigureAwait(false);
                Outcome outcome = await sendingLink
                    .SendMessageAsync(amqpMessage, IotHubConnection.GetNextDeliveryTag(ref _sendingDeliveryTag), AmqpConstants.NullBinary, cancellationToken)
                    .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Outcome was: {outcome?.DescriptorName}", nameof(SendAsync));

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }
            catch (Exception ex) when (ex is not TimeoutException && !Fx.IsFatal(ex))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(SendAsync)} threw an exception: {ex}", nameof(SendAsync));

                throw AmqpClientHelper.ToIotHubClientContract(ex);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Sending message [{message?.MessageId}] for device {deviceId}", nameof(SendAsync));
            }
        }

        /// <summary>
        /// Send a cloud-to-device message to the specified module.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="moduleId">The module identifier for the target module.</param>
        /// <param name="message">The cloud-to-module message.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> or <paramref name="message"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task SendAsync(string deviceId, string moduleId, Message message, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Sending message with Id [{message?.MessageId}] for device {deviceId}, module {moduleId}", nameof(SendAsync));

            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));
            Argument.AssertNotNull(message, nameof(message));

            if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset && message.MessageId == null)
            {
                message.MessageId = Guid.NewGuid().ToString();
            }

            using AmqpMessage amqpMessage = MessageConverter.MessageToAmqpMessage(message);
            amqpMessage.Properties.To = "/devices/" + WebUtility.UrlEncode(deviceId) + "/modules/" + WebUtility.UrlEncode(moduleId) + "/messages/deviceBound";
            try
            {
                SendingAmqpLink sendingLink = await GetSendingLinkAsync().ConfigureAwait(false);
                Outcome outcome = await sendingLink
                    .SendMessageAsync(
                        amqpMessage,
                        IotHubConnection.GetNextDeliveryTag(ref _sendingDeliveryTag),
                        AmqpConstants.NullBinary,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Outcome was: {outcome?.DescriptorName}", nameof(SendAsync));

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(SendAsync)} threw an exception: {ex}", nameof(SendAsync));

                throw AmqpClientHelper.ToIotHubClientContract(ex);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Sending message with Id [{message?.MessageId}] for device {deviceId}, module {moduleId}", nameof(SendAsync));
            }
        }

        /// <summary>
        /// Removes all cloud-to-device messages from a device's queue.
        /// </summary>
        /// <remarks>
        /// This call is made over HTTP. There is no need to call <see cref="OpenAsync"/> before calling this method.
        /// </remarks>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The <see cref="PurgeMessageQueueResult"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="deviceId"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<PurgeMessageQueueResult> PurgeMessageQueueAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Purging message queue for device: {deviceId}", nameof(PurgeMessageQueueAsync));

            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Delete, GetPurgeMessageQueueAsyncUri(deviceId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<PurgeMessageQueueResult>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(PurgeMessageQueueAsync)} threw an exception: {ex}", nameof(PurgeMessageQueueAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Purging message queue for device: {deviceId}", nameof(PurgeMessageQueueAsync));
            }
        }

        private static Uri GetPurgeMessageQueueAsyncUri(string deviceId)
        {
            return new Uri(PurgeMessageQueueFormat.FormatInvariant(deviceId), UriKind.Relative);
        }

        private Task<SendingAmqpLink> CreateSendingLinkAsync(TimeSpan timeout)
        {
            return _connection.CreateSendingLinkAsync(_sendingPath, timeout);
        }

        private async Task<SendingAmqpLink> GetSendingLinkAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"_faultTolerantSendingLink = {_faultTolerantSendingLink?.GetHashCode()}", nameof(GetSendingLinkAsync));

            try
            {
                if (!_faultTolerantSendingLink.TryGetOpenedObject(out SendingAmqpLink sendingLink))
                {
                    sendingLink = await _faultTolerantSendingLink.GetOrCreateAsync(new TimeSpan(Int32.MaxValue)).ConfigureAwait(false);
                }

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Retrieved SendingAmqpLink [{sendingLink?.Name}]", nameof(GetSendingLinkAsync));

                return sendingLink;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"_faultTolerantSendingLink = {_faultTolerantSendingLink?.GetHashCode()}", nameof(GetSendingLinkAsync));
            }
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            if (((AmqpObject)sender).TerminalException is AmqpException exception)
            {
                ErrorContext errorContext = AmqpErrorMapper.GetErrorContextFromException(exception);
                ErrorProcessor?.Invoke(errorContext);
                Exception exceptionToLog = errorContext.IOException != null ? errorContext.IOException : errorContext.IotHubException;
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(sender) + '.' + nameof(OnConnectionClosed)} threw an exception: {exceptionToLog}", nameof(OnConnectionClosed));
            }
            else
            {
                var defaultException = new IOException("AMQP connection was lost", ((AmqpObject)sender).TerminalException);
                ErrorContext errorContext = new ErrorContext(defaultException);
                ErrorProcessor?.Invoke(errorContext);
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(sender) + '.' + nameof(OnConnectionClosed)} threw an exception: {defaultException}", nameof(OnConnectionClosed));
            }
        }
    }
}
