using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#message-feedback"/>.
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
        /// <remarks>
        /// May not be null.
        /// </remarks>
        public Action<Exception> _errorProcessor;

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
            _connection = new IotHubConnection(credentialProvider, options.UseWebSocketOnly, options.TransportSettings, options);
            FeedbackReceiver = new AmqpFeedbackReceiver(_connection);
        }

        /// <summary>
        /// Gets the AmqpFeedbackReceiver which can deliver acknowledgments for messages sent to a device/module from IoT hub.
        /// </summary>
        internal AmqpFeedbackReceiver FeedbackReceiver { get; }

        /// <summary>
        /// Open the FeedbackReceiver instance.
        /// </summary>
        public virtual async Task OpenAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Opening FeedbackReceiver", nameof(OpenAsync));
            try
            {
                await FeedbackReceiver.OpenAsync().ConfigureAwait(false);
                ReceivingAmqpLink receivingLink = await FeedbackReceiver.FaultTolerantReceivingLink.GetReceivingLinkAsync().ConfigureAwait(false);
                receivingLink.RegisterMessageListener(OnFeedbackMessageReceivedAsync);
            }
            catch(Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(OpenAsync)} threw an exception: {ex}", nameof(OpenAsync));
                if (ex is IotHubException || ex is IOException)
                {
                    _errorProcessor?.Invoke(ex);
                }
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Opening FeedbackReceiver", nameof(OpenAsync));
            }
        }

        /// <summary>
        /// Close the FeedbackReceiver instance.
        /// </summary>
        public virtual async Task CloseAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Closing FeedbackReceiver", nameof(CloseAsync));

            try
            {
                await FeedbackReceiver.CloseAsync().ConfigureAwait(false);
                await _connection.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(CloseAsync)} threw an exception: {ex}", nameof(CloseAsync));
                if (ex is IotHubException || ex is IOException)
                {
                    _errorProcessor?.Invoke(ex);
                }
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Closing FeedbackReceiver", nameof(CloseAsync));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Disposing FeedbackReceiver", nameof(Dispose));

            FeedbackReceiver.Dispose();

            if (Logging.IsEnabled)
                Logging.Exit(this, $"Disposing FeedbackReceiver", nameof(Dispose));
            GC.SuppressFinalize(this);
        }

        private async void OnFeedbackMessageReceivedAsync(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, amqpMessage, nameof(OnFeedbackMessageReceivedAsync));

            try
            {
                if (amqpMessage != null && _messageFeedbackProcessor != null)
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
                        _messageFeedbackProcessor.Invoke(feedbackBatch);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(OnFeedbackMessageReceivedAsync)} threw an exception: {ex}", nameof(OnFeedbackMessageReceivedAsync));
                if (ex is IotHubException || ex is IOException)
                {
                    _errorProcessor?.Invoke(ex);
                }
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, amqpMessage, nameof(OnFeedbackMessageReceivedAsync));
            }
        }
    }
}