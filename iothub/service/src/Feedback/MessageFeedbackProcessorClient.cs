// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Amqp;
using Microsoft.Azure.Devices.Common.Exceptions;

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
        private readonly AmqpConnectionHandler _amqpConnection;

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
            _amqpConnection = new AmqpConnectionHandler(
                credentialProvider,
                options.Protocol,
                AmqpsConstants.FeedbackMessageAddress,
                options,
                OnConnectionClosed,
                OnFeedbackMessageReceivedAsync);
        }

        /// <summary>
        /// The callback to be executed each time message feedback is received from the service.
        /// </summary>
        /// <remarks>
        /// Must not be null.
        /// </remarks>
        /// <example>
        /// serviceClient.MessageFeedbackProcessor.MessageFeedbackProcessor = OnFeedbackReceived;
        /// serviceClient.MessageFeedbackProcessor.OpenAsync();
        ///
        /// //...
        ///
        /// public AcknowledgementType OnFeedbackReceived(FeedbackBatch feedbackBatch)
        /// {
        ///    foreach (FeedbackRecord record in feedback.Records)
        ///    {
        ///        Console.WriteLine($"Received feedback from device {record.DeviceId}")
        ///    }
        ///
        ///    return AcknowledgementType.Complete;
        /// }
        /// </example>
        public Func<FeedbackBatch, AcknowledgementType> MessageFeedbackProcessor { get; set; }

        /// <summary>
        /// The callback to be executed when the connection is lost.
        /// </summary>
        /// <example>
        /// serviceClient.MessageFeedbackProcessor.ErrorProcessor = OnConnectionLost;
        /// serviceClient.MessageFeedbackProcessor.OpenAsync();
        ///
        /// //...
        ///
        /// public void OnConnectionLost(ErrorContext errorContext)
        /// {
        ///    // Add reconnection logic as needed
        ///    Console.WriteLine("Feedback message processor connection lost")
        /// }
        /// </example>
        public Action<ErrorContext> ErrorProcessor { get; set; }

        /// <summary>
        /// Open the connection and start receiving acknowledgements for messages sent.
        /// </summary>
        /// <remarks>
        /// Callback for message feedback must be set before opening the connection.
        /// </remarks>
        /// <exception cref="IotHubServiceClient"> with <see cref="HttpStatusCode.RequestTimeout"/>Thrown if the client operation times out before the response is returned.</exception>
        /// <exception cref="IotHubServiceClient"> with <see cref="HttpStatusCode.RequestTimeout"/>Thrown when the operation has been canceled. The inner exception will be
        /// <see cref="OperationCanceledException"/>.</exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="IotHubServiceException">Thrown if an error occurs when communicating with IoT hub service.
        /// If <see cref="IotHubServiceException.IsTransient"/> is set to <c>true</c> then it is a transient exception.
        /// If <see cref="IotHubServiceException.IsTransient"/> is set to <c>false</c> then it is a non-transient exception.</exception>
        public virtual async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Opening MessageFeedbackProcessorClient", nameof(OpenAsync));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (MessageFeedbackProcessor == null)
                {
                    throw new Exception("Callback for message feedback must be set before opening the connection.");
                }

                await _amqpConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
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
        /// The instance can be re-opened after closing.
        /// </remarks>
        /// <exception cref="IotHubServiceClient"> with <see cref="HttpStatusCode.RequestTimeout"/>Thrown if the client operation times out before the response is returned.</exception>
        /// <exception cref="IotHubServiceClient"> with <see cref="HttpStatusCode.RequestTimeout"/>Thrown when the operation has been canceled. The inner exception will be
        /// <see cref="OperationCanceledException"/>.</exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="IotHubServiceException">Thrown if an error occurs when communicating with IoT hub service.
        /// If <see cref="IotHubServiceException.IsTransient"/> is set to <c>true</c> then it is a transient exception.
        /// If <see cref="IotHubServiceException.IsTransient"/> is set to <c>false</c> then it is a non-transient exception.</exception>
        public virtual async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Closing MessageFeedbackProcessorClient", nameof(CloseAsync));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _amqpConnection.CloseAsync(cancellationToken).ConfigureAwait(false);
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
                            .GetObjectFromAmqpMessageAsync<IEnumerable<FeedbackRecord>>(amqpMessage)
                            .ConfigureAwait(false);

                        var feedbackBatch = new FeedbackBatch
                        {
                            EnqueuedTime = (DateTime)amqpMessage.MessageAnnotations.Map[MessageSystemPropertyNames.EnqueuedTime],
                            Records = records,
                            IotHubHostName = Encoding.UTF8.GetString(
                                amqpMessage.Properties.UserId.Array,
                                amqpMessage.Properties.UserId.Offset,
                                amqpMessage.Properties.UserId.Count)
                        };

                        AcknowledgementType ack = MessageFeedbackProcessor.Invoke(feedbackBatch);
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
                    Logging.Error(this, $"{nameof(OnFeedbackMessageReceivedAsync)} threw an exception: {ex}", nameof(OnFeedbackMessageReceivedAsync));

                if (ex is IotHubServiceException hubEx)
                {
                    ErrorProcessor?.Invoke(new ErrorContext(hubEx));
                }
                else if (ex is IOException ioEx)
                {
                    ErrorProcessor?.Invoke(new ErrorContext(ioEx));
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
                ErrorContext errorContext = AmqpClientHelper.GetErrorContextFromException(exception);
                ErrorProcessor?.Invoke(errorContext);
                Exception exceptionToLog = errorContext.IotHubServiceException;

                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(sender) + '.' + nameof(OnConnectionClosed)} threw an exception: {exceptionToLog}", nameof(OnConnectionClosed));
            }
            else
            {
                var defaultException = new IotHubServiceException("AMQP connection was lost.", ((AmqpObject)sender).TerminalException);
                var errorContext = new ErrorContext(defaultException);
                ErrorProcessor?.Invoke(errorContext);

                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(sender) + '.' + nameof(OnConnectionClosed)} threw an exception: {defaultException}", nameof(OnConnectionClosed));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _amqpConnection?.Dispose();
        }
    }
}
