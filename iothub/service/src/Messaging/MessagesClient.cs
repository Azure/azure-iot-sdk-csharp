// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Amqp;
using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> for sending cloud-to-device and cloud-to-module messages.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d"/>.
    public class MessagesClient : IDisposable
    {
        private const string SendingPath = "/messages/deviceBound";
        private const string PurgeMessageQueueFormat = "/devices/{0}/commands";

        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly AmqpConnectionHandler _amqpConnection;
        private readonly IotHubServiceClientOptions _clientOptions;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;
        private readonly RetryHandler _internalRetryHandler;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected MessagesClient()
        {
        }

        internal MessagesClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            HttpClient httpClient,
            HttpRequestMessageFactory httpRequestMessageFactory,
            IotHubServiceClientOptions options,
            RetryHandler retryHandler)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
            _clientOptions = options;
            _internalRetryHandler = retryHandler;
            _amqpConnection = new AmqpConnectionHandler(
                credentialProvider,
                options.Protocol,
                AmqpsConstants.CloudToDeviceMessageAddress,
                options,
                OnConnectionClosed);
        }

        /// <summary>
        /// The callback to be executed when the connection is lost.
        /// </summary>
        /// <example>
        /// <code language="csharp">
        /// serviceClient.Messaging.ErrorProcessor = OnConnectionLost;
        /// serviceClient.Messaging.OpenAsync();
        ///
        /// //...
        ///
        /// public void OnConnectionLost(ErrorContext errorContext)
        /// {
        ///    // Add reconnection logic as needed
        ///    Console.WriteLine("Messaging client connection lost")
        /// }
        /// </code>
        /// </example>
        public Action<ErrorContext> ErrorProcessor { get; set; }

        /// <summary>
        /// Open the connection. Must be done before any cloud-to-device messages can be sent.
        /// </summary>
        /// <exception cref="IotHubServiceException"> with <see cref="HttpStatusCode.RequestTimeout"/>If the client operation times out before the response is returned.</exception>
        /// <exception cref="IotHubServiceException">If an error occurs when communicating with IoT hub service.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Opening MessagingClient.", nameof(OpenAsync));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await _amqpConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Opening MessagingClient threw an exception: {ex}", nameof(OpenAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Opening MessagingClient.", nameof(OpenAsync));
            }
        }

        /// <summary>
        /// Close the connection.
        /// </summary>
        /// <remarks>
        /// The instance can be re-opened after closing.
        /// </remarks>
        /// <exception cref="IotHubServiceException"> with <see cref="HttpStatusCode.RequestTimeout"/>If the client operation times out before the response is returned.</exception>
        /// <exception cref="IotHubServiceException">If an error occurs when communicating with IoT hub service.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Closing MessagingClient.", nameof(CloseAsync));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await _amqpConnection.CloseAsync(cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Closing MessagingClient threw an exception: {ex}", nameof(CloseAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Closing MessagingClient.", nameof(CloseAsync));
            }
        }

        /// <summary>
        /// Send a cloud-to-device message to the specified device.
        /// </summary>
        /// <remarks>
        /// In order to receive feedback messages on the service client, set the <see cref="Message.Ack"/> property to an appropriate value
        /// and use <see cref="IotHubServiceClient.MessageFeedback"/>.
        /// </remarks>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="message">The cloud-to-device message.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="deviceId"/> or <paramref name="message"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="deviceId"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.</exception>
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task SendAsync(string deviceId, Message message, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Sending message with Id [{message?.MessageId}] for device {deviceId}", nameof(SendAsync));

            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNull(message, nameof(message));

            CheckConnectionIsOpen();

            cancellationToken.ThrowIfCancellationRequested();

            CheckAddMessageId(ref message);

            using AmqpMessage amqpMessage = MessageConverter.MessageToAmqpMessage(message);
            amqpMessage.Properties.To = $"/devices/{WebUtility.UrlEncode(deviceId)}/messages/deviceBound";

            try
            {
                Outcome outcome = null;
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            outcome = await _amqpConnection.SendAsync(amqpMessage, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Outcome was: {outcome?.DescriptorName}", nameof(SendAsync));

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpClientHelper.GetExceptionFromOutcome(outcome);
                }
            }
            catch (Exception ex) when (ex is not TimeoutException && !Fx.IsFatal(ex))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Sending message with Id [{message?.MessageId}] for device {deviceId} threw an exception: {ex}", nameof(SendAsync));

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
        /// <exception cref="ArgumentNullException">When the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> or <paramref name="message"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.</exception>
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task SendAsync(string deviceId, string moduleId, Message message, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Sending message with Id [{message?.MessageId}] for device {deviceId}, module {moduleId}", nameof(SendAsync));

            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));
            Argument.AssertNotNull(message, nameof(message));

            CheckConnectionIsOpen();

            cancellationToken.ThrowIfCancellationRequested();

            CheckAddMessageId(ref message);

            using AmqpMessage amqpMessage = MessageConverter.MessageToAmqpMessage(message);
            amqpMessage.Properties.To = $"/devices/{WebUtility.UrlEncode(deviceId)}/modules/{WebUtility.UrlEncode(moduleId)}/messages/deviceBound";
            try
            {
                Outcome outcome = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            outcome = await _amqpConnection.SendAsync(amqpMessage, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Outcome was: {outcome?.DescriptorName}", nameof(SendAsync));

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpClientHelper.GetExceptionFromOutcome(outcome);
                }
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Sending message with Id [{message?.MessageId}] for device {deviceId}, module {moduleId} threw an exception: {ex}", nameof(SendAsync));

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
        /// <exception cref="ArgumentNullException">When the provided <paramref name="deviceId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="deviceId"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">If IoT hub responded to the request with a non-successful status code.
        /// For example, if the provided request was throttled, <see cref="IotHubServiceException"/> wit.
        /// <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.</exception>
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<PurgeMessageQueueResult> PurgeMessageQueueAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Purging message queue for device: {deviceId}", nameof(PurgeMessageQueueAsync));

            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var purgeUri = new Uri(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        PurgeMessageQueueFormat,
                        deviceId),
                    UriKind.Relative);

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Delete, purgeUri, _credentialProvider);
                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<PurgeMessageQueueResult>(response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Purging message queue for device {deviceId} threw an exception: {ex}", nameof(PurgeMessageQueueAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Purging message queue for device: {deviceId}", nameof(PurgeMessageQueueAsync));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _amqpConnection?.Dispose();
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            if (((AmqpObject)sender).TerminalException is AmqpException exception)
            {
                ErrorContext errorContext = AmqpClientHelper.GetErrorContextFromException(exception);
                ErrorProcessor?.Invoke(errorContext);
                Exception exceptionToLog = errorContext.IotHubServiceException;
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(sender)}.{nameof(OnConnectionClosed)} threw an exception: {exceptionToLog}", nameof(OnConnectionClosed));
            }
            else
            {
                var defaultException = new IotHubServiceException("AMQP connection was lost", ((AmqpObject)sender).TerminalException);
                ErrorContext errorContext = new ErrorContext(defaultException);
                ErrorProcessor?.Invoke(errorContext);
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(sender)}.{nameof(OnConnectionClosed)} threw an exception: {defaultException}", nameof(OnConnectionClosed));
            }
        }

        private void CheckConnectionIsOpen()
        {
            if (!_amqpConnection.IsOpen)
            {
                throw new IotHubServiceException("Must open client before sending messages.");
            }
        }

        private void CheckAddMessageId(ref Message message)
        {
            if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset && message.MessageId == null)
            {
                message.MessageId = Guid.NewGuid().ToString();
            }
        }
    }
}
