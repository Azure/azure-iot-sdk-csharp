// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Common.Extensions;

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
        private readonly IotHubConnection _connection;
        private readonly AmqpFileNotificationReceiver _fileNotificationReceiver;

        /// <summary>
        /// The callback to be executed each time file upload notification is received from the service.
        /// </summary>
        /// <remarks>
        /// May not be null.
        /// </remarks>
        public Func<FileNotification, AcknowledgementType> FileNotificationProcessor;

        /// <summary>
        /// The callback to be executed when the connection is lost.
        /// </summary>
        public Action<ErrorContext> ErrorProcessor;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected FileUploadNotificationProcessorClient()
        {
        }

        internal FileUploadNotificationProcessorClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            IotHubServiceClientOptions options)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _connection = new IotHubConnection(credentialProvider, options.UseWebSocketOnly, options);
            _fileNotificationReceiver = new AmqpFileNotificationReceiver(_connection);
        }

        /// <summary>
        /// Open the connection and start receiving file upload notifications.
        /// </summary>
        /// <remarks>
        /// Callback for file upload notifications must be set before opening the connection.
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
        public virtual async Task OpenAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Opening FileUploadNotificationProcessorClient", nameof(OpenAsync));

            try
            {
                if (FileNotificationProcessor == null)
                {
                    throw new Exception("Callback for file upload notifications must be set before opening the connection.");
                }
                await _fileNotificationReceiver.OpenAsync().ConfigureAwait(false);
                ReceivingAmqpLink receivingAmqpLink = await _fileNotificationReceiver.FaultTolerantReceivingLink.GetReceivingLinkAsync().ConfigureAwait(false);
                receivingAmqpLink.RegisterMessageListener(OnNotificationMessageReceivedAsync);
                receivingAmqpLink.Session.Connection.Closed += ConnectionClosed;
                receivingAmqpLink.Session.Closed += ConnectionClosed;
                receivingAmqpLink.Closed += ConnectionClosed;
                await _fileNotificationReceiver.ReceiveAsync(CancellationToken.None).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Opening FileUploadNotificationProcessorClient", nameof(OpenAsync));
            }
        }

        /// <summary>
        /// Close the connection and stop receiving file upload notifications.
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
                Logging.Enter(this, $"Closing FileUploadNotificationProcessorClient", nameof(CloseAsync));

            try
            {
                await _fileNotificationReceiver.CloseAsync().ConfigureAwait(false);
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
                    Logging.Exit(this, $"Closing FileUploadNotificationProcessorClient", nameof(CloseAsync));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Disposing FileUploadNotificationProcessorClient", nameof(Dispose));

            _fileNotificationReceiver.Dispose();

            if (Logging.IsEnabled)
                Logging.Exit(this, $"Disposing FileUploadNotificationProcessorClient", nameof(Dispose));
            GC.SuppressFinalize(this);
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
                        AmqpClientHelper.ValidateContentType(amqpMessage, CommonConstants.FileNotificationContentType);
                        FileNotification fileNotification = await AmqpClientHelper.GetObjectFromAmqpMessageAsync<FileNotification>(amqpMessage).ConfigureAwait(false);
                        fileNotification.LockToken = amqpMessage.DeliveryTag.Array.ToString();

                        AcknowledgementType ack = FileNotificationProcessor.Invoke(fileNotification);
                        switch(ack)
                        {
                            case AcknowledgementType.Abandon:
                                await _fileNotificationReceiver.AbandonAsync(fileNotification, CancellationToken.None).ConfigureAwait(false);
                                break;
                            case AcknowledgementType.Complete:
                                await _fileNotificationReceiver.CompleteAsync(fileNotification, CancellationToken.None).ConfigureAwait(false);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(OnNotificationMessageReceivedAsync)} threw an exception: {ex}", nameof(OnNotificationMessageReceivedAsync));
                if (ex is IotHubException || ex is IOException)
                {
                    if (ex is IotHubException)
                        ErrorProcessor?.Invoke(new ErrorContext((IotHubException)ex));
                    else
                        ErrorProcessor?.Invoke(new ErrorContext((IOException)ex));
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, amqpMessage, nameof(OnNotificationMessageReceivedAsync));
            }
        }
            
        private void ConnectionClosed(object sender, EventArgs e)
        {
            IotHubException ex = new IotHubException(e.ToString());
            if (Logging.IsEnabled)
                Logging.Error(this, $"{nameof(sender) + '.' + nameof(ConnectionClosed)} threw an exception: {ex}", nameof(ConnectionClosed));
            ErrorProcessor?.Invoke(new ErrorContext(ex));
        }
    }
}
