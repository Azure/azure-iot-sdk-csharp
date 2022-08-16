using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Common.Extensions;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> for handling file upload notications.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload#service-file-upload-notifications"/>.
    public class FileUploadNotificationProcessorClient : IDisposable
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly IotHubConnection _connection;

        /// <summary>
        /// The callback to be executed each time file upload notification is received from the service.
        /// </summary>
        /// <remarks>
        /// May not be null.
        /// </remarks>
        public Func<FileNotification, DeliveryAcknowledgement> _fileNotificationProcessor;

        /// <summary>
        /// The callback to be executed when the connection is lost.
        /// </summary>
        public Action<ErrorContext> _errorProcessor;

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
            FileNotificationReceiver = new AmqpFileNotificationReceiver(_connection);
        }

        /// <summary>
        /// Gets the AmqpFileNotificationReceiver which receives notifications for file upload operations.
        /// </summary>
        internal AmqpFileNotificationReceiver FileNotificationReceiver;

        /// <summary>
        /// Open the connection and start receiving file upload notifications.
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
                Logging.Enter(this, $"Opening FileUploadNotificationProcessorClient", nameof(OpenAsync));

            try
            {
                if (_fileNotificationProcessor == null)
                {
                    throw new Exception("Callback for message feedback {0} must be set before opening the connection.".FormatInvariant(_fileNotificationProcessor));
                }
                await FileNotificationReceiver.OpenAsync().ConfigureAwait(false);
                ReceivingAmqpLink receivingAmqpLink = await FileNotificationReceiver.FaultTolerantReceivingLink.GetReceivingLinkAsync().ConfigureAwait(false);
                receivingAmqpLink.RegisterMessageListener(OnNotificationMessageReceivedAsync);
                receivingAmqpLink.Session.Connection.Closed += ConnectionClosed;
                await FileNotificationReceiver.ReceiveAsync(CancellationToken.None).ConfigureAwait(false);
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
                await FileNotificationReceiver.CloseAsync().ConfigureAwait(false);
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

            FileNotificationReceiver.Dispose();

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
                        fileNotification.LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString();

                        DeliveryAcknowledgement ack = _fileNotificationProcessor.Invoke(fileNotification);
                        switch(ack)
                        {
                            case DeliveryAcknowledgement.NegativeOnly:
                                await FileNotificationReceiver.AbandonAsync(fileNotification, CancellationToken.None).ConfigureAwait(false);
                                break;
                            case DeliveryAcknowledgement.PositiveOnly:
                                await FileNotificationReceiver.CompleteAsync(fileNotification, CancellationToken.None).ConfigureAwait(false);
                                break;
                            case DeliveryAcknowledgement.Full:
                                await FileNotificationReceiver.AbandonAsync(fileNotification, CancellationToken.None).ConfigureAwait(false);
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
                        _errorProcessor?.Invoke(new ErrorContext((IotHubException)ex));
                    else
                        _errorProcessor?.Invoke(new ErrorContext((IOException)ex));
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
            _errorProcessor?.Invoke(new ErrorContext(ex));
        }
    }
}
