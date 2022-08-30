// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Azure.Devices.Common.Extensions;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> for receiving cloud-to-device message feedback.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d"/>.
    public class MessageFeedbackProcessorClient : IDisposable
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly IotHubConnection _connection;
        private readonly AmqpFeedbackReceiver _feedbackReceiver;

        /// <summary>
        /// The callback to be executed each time message feedback is received from the service.
        /// </summary>
        /// <remarks>
        /// May not be null.
        /// </remarks>
        public Func<FeedbackBatch, AcknowledgementType> MessageFeedbackProcessor;

        /// <summary>
        /// The callback to be executed when the connection is lost.
        /// </summary>
        public Action<ErrorContext> ErrorProcessor;

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
            _connection = new IotHubConnection(credentialProvider, options.Transport == TransportType.WebSocket, options);
            _feedbackReceiver = new AmqpFeedbackReceiver(_connection);
        }

        /// <summary>
        /// Open the connection and start receiving acknowledgments for messages sent.
        /// </summary>
        /// <remarks>
        /// Callback for message feedback must be set before opening the connection.
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
                Logging.Enter(this, $"Opening MessageFeedbackProcessorClient", nameof(OpenAsync));
            try
            {
                if (MessageFeedbackProcessor == null)
                {
                    throw new Exception("Callback for message feedback must be set before opening the connection.");
                }
                await _feedbackReceiver.OpenAsync().ConfigureAwait(false);
                ReceivingAmqpLink receivingAmqpLink = await _feedbackReceiver.FaultTolerantReceivingLink.GetReceivingLinkAsync().ConfigureAwait(false);
                receivingAmqpLink.RegisterMessageListener(OnFeedbackMessageReceivedAsync);
                receivingAmqpLink.Session.Connection.Closed += OnConnectionClosed;
                receivingAmqpLink.Session.Closed += OnConnectionClosed;
                receivingAmqpLink.Closed += OnConnectionClosed;
                await _feedbackReceiver.ReceiveAsync(CancellationToken.None).ConfigureAwait(false);
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
                await _feedbackReceiver.CloseAsync().ConfigureAwait(false);
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

            _feedbackReceiver.Dispose();

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
                        AmqpClientHelper.ValidateContentType(amqpMessage, AmqpsConstants.BatchedFeedbackContentType);
                        IEnumerable<FeedbackRecord> records = await AmqpClientHelper
                            .GetObjectFromAmqpMessageAsync<IEnumerable<FeedbackRecord>>(amqpMessage).ConfigureAwait(false);

                        FeedbackBatch feedbackBatch = new FeedbackBatch
                        {
                            EnqueuedTime = (DateTime)amqpMessage.MessageAnnotations.Map[MessageSystemPropertyNames.EnqueuedTime],
                            DeliveryTag = amqpMessage.DeliveryTag,
                            Records = records,
                            UserId = Encoding.UTF8.GetString(amqpMessage.Properties.UserId.Array, amqpMessage.Properties.UserId.Offset, amqpMessage.Properties.UserId.Count)
                        };
                        AcknowledgementType ack = MessageFeedbackProcessor.Invoke(feedbackBatch);
                        switch (ack)
                        {
                            case AcknowledgementType.Abandon:
                                await _feedbackReceiver.AbandonAsync(feedbackBatch, CancellationToken.None).ConfigureAwait(false);
                                break;

                            case AcknowledgementType.Complete:
                                await _feedbackReceiver.CompleteAsync(feedbackBatch, CancellationToken.None).ConfigureAwait(false);
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
                        ErrorProcessor?.Invoke(new ErrorContext((IotHubException)ex));
                    else
                        ErrorProcessor?.Invoke(new ErrorContext((IOException)ex));
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, amqpMessage, nameof(OnFeedbackMessageReceivedAsync));
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
