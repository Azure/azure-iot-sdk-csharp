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
    /// Subclient of <see cref="IotHubServiceClient"/> for handling cloud-to-device message feedback.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d"/>.
    public class MessageFeedbackProcessorClient : IDisposable
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly IotHubConnection _connection;


        /// <summary>
        /// The callback to be executed each time message feedback is received from the service.
        /// </summary>
        /// <remarks>
        /// May not be null.
        /// </remarks>
        public Func<FeedbackBatch, DeliveryAcknowledgement> _messageFeedbackProcessor;

        /// <summary>
        /// The callback to be executed when the connection is lost.
        /// </summary>
        public Action<ErrorContext> _errorProcessor;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected MessageFeedbackProcessorClient()
        {
        }

        internal MessageFeedbackProcessorClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            IotHubServiceClientOptions options)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _connection = new IotHubConnection(credentialProvider, options.UseWebSocketOnly, options);
            FeedbackReceiver = new AmqpFeedbackReceiver(_connection);
        }

        /// <summary>
        /// Gets the AmqpFeedbackReceiver which receives acknowledgments for messages sent to a device/module from IoT hub.
        /// </summary>
        internal AmqpFeedbackReceiver FeedbackReceiver;

        /// <summary>
        /// Open the connection and start receiving acknowledgments for messages sent.
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
                Logging.Enter(this, $"Opening MessageFeedbackProcessorClient", nameof(OpenAsync));
            try
            {
                if(_messageFeedbackProcessor == null)
                {
                    throw new Exception("Callback for message feedback {0} must be set before opening the connection.".FormatInvariant(_messageFeedbackProcessor));
                }
                await FeedbackReceiver.OpenAsync().ConfigureAwait(false);
                ReceivingAmqpLink receivingAmqpLink = await FeedbackReceiver.FaultTolerantReceivingLink.GetReceivingLinkAsync().ConfigureAwait(false);
                receivingAmqpLink.RegisterMessageListener(OnFeedbackMessageReceivedAsync);
                receivingAmqpLink.Session.Connection.Closed += ConnectionClosed;
                await FeedbackReceiver.ReceiveAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(OpenAsync)} threw an exception: {ex}", nameof(OpenAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Opening MessageFeedbackProcessorClient", nameof(OpenAsync));
            }
        }

        /// <summary>
        /// Close the connection and stop receiving acknowledgments for messages sent.
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
                Logging.Enter(this, $"Closing MessageFeedbackProcessorClient", nameof(CloseAsync));

            try
            {
                await FeedbackReceiver.CloseAsync().ConfigureAwait(false);
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
                    Logging.Exit(this, $"Closing MessageFeedbackProcessorClient", nameof(CloseAsync));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Disposing MessageFeedbackProcessorClient", nameof(Dispose));

            FeedbackReceiver.Dispose();

            if (Logging.IsEnabled)
                Logging.Exit(this, $"Disposing MessageFeedbackProcessorClient", nameof(Dispose));
            GC.SuppressFinalize(this);
        }

        private async void OnFeedbackMessageReceivedAsync(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, amqpMessage, nameof(OnFeedbackMessageReceivedAsync));

            try
            {
                if (amqpMessage != null)
                {
                    using (amqpMessage)
                    {
                        AmqpClientHelper.ValidateContentType(amqpMessage, CommonConstants.BatchedFeedbackContentType);
                        IEnumerable<FeedbackRecord> records = await AmqpClientHelper
                            .GetObjectFromAmqpMessageAsync<IEnumerable<FeedbackRecord>>(amqpMessage).ConfigureAwait(false);

                        FeedbackBatch feedbackBatch = new FeedbackBatch
                        {
                            EnqueuedTime = (DateTime)amqpMessage.MessageAnnotations.Map[MessageSystemPropertyNames.EnqueuedTime],
                            LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString(),
                            Records = records,
                            UserId = Encoding.UTF8.GetString(amqpMessage.Properties.UserId.Array, amqpMessage.Properties.UserId.Offset, amqpMessage.Properties.UserId.Count)
                        };
                        DeliveryAcknowledgement ack = _messageFeedbackProcessor.Invoke(feedbackBatch);
                        switch (ack)
                        {
                            case DeliveryAcknowledgement.NegativeOnly:
                                await FeedbackReceiver.AbandonAsync(feedbackBatch, CancellationToken.None).ConfigureAwait(false);
                                break;
                            case DeliveryAcknowledgement.PositiveOnly:
                                await FeedbackReceiver.CompleteAsync(feedbackBatch, CancellationToken.None).ConfigureAwait(false);
                                break;
                            case DeliveryAcknowledgement.Full:
                                await FeedbackReceiver.AbandonAsync(feedbackBatch, CancellationToken.None).ConfigureAwait(false);
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
                    Logging.Error(this, $"{nameof(OnFeedbackMessageReceivedAsync)} threw an exception: {ex}", nameof(OnFeedbackMessageReceivedAsync));
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
                    Logging.Exit(this, amqpMessage, nameof(OnFeedbackMessageReceivedAsync));
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