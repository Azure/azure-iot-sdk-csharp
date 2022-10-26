// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Amqp;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> for receiving file upload notifications.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload#service-file-upload-notifications"/>.
    public class FileUploadNotificationProcessorClient : IDisposable
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly AmqpConnectionHandler _amqpConnection;
        private readonly RetryHandler _internalRetryHandler;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected FileUploadNotificationProcessorClient()
        {
        }

        internal FileUploadNotificationProcessorClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            IotHubServiceClientOptions options,
            IRetryPolicy retryPolicy)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _internalRetryHandler = new RetryHandler(retryPolicy);
            _amqpConnection = new AmqpConnectionHandler(
                credentialProvider,
                options.Protocol,
                AmqpsConstants.FileUploadNotificationsAddress,
                options,
                OnConnectionClosed,
                OnNotificationMessageReceivedAsync);
        }

        /// <summary>
        /// The callback to be executed each time file upload notification is received from the service.
        /// </summary>
        /// <remarks>
        /// Must not be null.
        /// </remarks>
        /// <example>
        /// serviceClient.FileUploadNotificationProcessor.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;
        /// serviceClient.FileUploadNotificationProcessor.OpenAsync();
        ///
        /// //...
        ///
        /// public AcknowledgementType OnFileUploadNotificationReceived(FileUploadNotification fileUploadNotification)
        /// {
        ///    Console.WriteLine($"Received file upload notification from device {fileUploadNotification.DeviceId}")
        ///    return AcknowledgementType.Complete;
        /// }
        /// </example>
        public Func<FileUploadNotification, AcknowledgementType> FileUploadNotificationProcessor { get; set; }

        /// <summary>
        /// The callback to be executed when the connection is lost.
        /// </summary>
        /// <example>
        /// serviceClient.FileUploadNotificationProcessor.ErrorProcessor = OnConnectionLost;
        /// serviceClient.FileUploadNotificationProcessor.OpenAsync();
        ///
        /// //...
        ///
        /// public void OnConnectionLost(ErrorContext errorContext)
        /// {
        ///    // Add reconnection logic as needed
        ///    Console.WriteLine("File upload notification processor connection lost")
        /// }
        /// </example>
        public Action<ErrorContext> ErrorProcessor { get; set; }

        /// <summary>
        /// Open the connection and start receiving file upload notifications.
        /// </summary>
        /// <remarks>
        /// Callback for file upload notifications must be set before opening the connection.
        /// </remarks>
        /// <exception cref="IotHubServiceException"> with <see cref="HttpStatusCode.RequestTimeout"/>If the client operation times out before the response is returned.</exception>
        /// <exception cref="IotHubServiceException">If an error occurs when communicating with IoT hub service.</exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <exception cref="SocketException">If a socket error occurs.</exception>
        /// <exception cref="WebSocketException">If an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">If an I/O error occurs.</exception>
        public virtual async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Opening FileUploadNotificationProcessorClient.", nameof(OpenAsync));

            if (FileUploadNotificationProcessor == null)
            {
                throw new InvalidOperationException("Callback for file upload notifications must be set before opening the connection.");
            }

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
                    Logging.Error(this, $"Opening FileUploadNotificationProcessorClient threw an exception: {ex}", nameof(OpenAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Opening FileUploadNotificationProcessorClient.", nameof(OpenAsync));
            }
        }

        /// <summary>
        /// Close the connection and stop receiving file upload notifications.
        /// </summary>
        /// <remarks>
        /// The instance can be re-opened after closing.
        /// </remarks>
        /// <exception cref="IotHubServiceException"> with <see cref="HttpStatusCode.RequestTimeout"/>If the client operation times out before the response is returned.</exception>
        /// <exception cref="IotHubServiceException">If an error occurs when communicating with IoT hub service.</exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <exception cref="SocketException">If a socket error occurs.</exception>
        /// <exception cref="WebSocketException">If an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">If an I/O error occurs.</exception>
        public virtual async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Closing FileUploadNotificationProcessorClient.", nameof(CloseAsync));

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
                    Logging.Error(this, $"Closing FileUploadNotificationProcessorClient threw an exception: {ex}", nameof(CloseAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Closing FileUploadNotificationProcessorClient.", nameof(CloseAsync));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _amqpConnection?.Dispose();
        }

        private async void OnNotificationMessageReceivedAsync(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, amqpMessage, nameof(OnNotificationMessageReceivedAsync));

            try
            {
                if (amqpMessage != null)
                {
                    using (amqpMessage)
                    {
                        AmqpClientHelper.ValidateContentType(amqpMessage, AmqpsConstants.FileNotificationContentType);
                        FileUploadNotification fileUploadNotification = await AmqpClientHelper
                            .GetObjectFromAmqpMessageAsync<FileUploadNotification>(amqpMessage)
                            .ConfigureAwait(false);
                        AcknowledgementType ack = FileUploadNotificationProcessor.Invoke(fileUploadNotification);
                        if (ack == AcknowledgementType.Complete)
                        {
                            await _amqpConnection.CompleteMessageAsync(amqpMessage.DeliveryTag).ConfigureAwait(false);
                        }
                        else if (ack == AcknowledgementType.Abandon)
                        {
                            await _amqpConnection.AbandonMessageAsync(amqpMessage.DeliveryTag).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(OnNotificationMessageReceivedAsync)} threw an exception: {ex}", nameof(OnNotificationMessageReceivedAsync));

                try
                {
                    if (ex is IotHubServiceException hubEx)
                    {
                        ErrorProcessor?.Invoke(new ErrorContext(hubEx));
                    }
                    else if (ex is IOException ioEx)
                    {
                        ErrorProcessor?.Invoke(new ErrorContext(ioEx));
                    }

                    await _amqpConnection.AbandonMessageAsync(amqpMessage.DeliveryTag).ConfigureAwait(false);
                }
                catch (Exception ex2)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, $"{nameof(OnNotificationMessageReceivedAsync)} threw an exception during cleanup: {ex2}", nameof(OnNotificationMessageReceivedAsync));
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, amqpMessage, nameof(OnNotificationMessageReceivedAsync));
            }
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
    }
}
