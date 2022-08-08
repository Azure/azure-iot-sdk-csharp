using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Transport types supported by MessagingClient.
    /// </summary>
    /// <remarks>
    /// Amqp and Amqp over WebSocket only.
    /// </remarks>
    public enum TransportType
    {
        /// <summary>
        /// Advanced Message Queuing Protocol transport.
        /// </summary>
        Amqp,

        /// <summary>
        /// Advanced Message Queuing Protocol transport over WebSocket only.
        /// </summary>
        Amqp_WebSocket_Only
    }

    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> for sending cloud to device and cloud to module messages.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#message-feedback"/>.
    public class MessagingClient : IDisposable
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly IotHubConnection _connection;
        private readonly IotHubServiceClientOptions _clientOptions;
        private readonly FaultTolerantAmqpObject<SendingAmqpLink> _faultTolerantSendingLink;
        private readonly TimeSpan _openTimeout;
        private readonly TimeSpan _operationTimeout;

        private const string _sendingPath = "/messages/deviceBound";


        private int _sendingDeliveryTag;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected MessagingClient()
        {
        }

        internal MessagingClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            IotHubServiceClientOptions options)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _clientOptions = options;
            _connection = new IotHubConnection(credentialProvider, options.UseWebSocketOnly, options.TransportSettings, options);
            _openTimeout = IotHubConnection.DefaultOpenTimeout;
            _operationTimeout = IotHubConnection.DefaultOperationTimeout;
            _faultTolerantSendingLink = new FaultTolerantAmqpObject<SendingAmqpLink>(CreateSendingLinkAsync, _connection.CloseLink);
            
        }

        /// <summary>
        /// Open the MessagingClient instance.
        /// </summary>
        public virtual async Task OpenAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Opening MessagingClient", nameof(OpenAsync));
            try
            {
                using var ctx = new CancellationTokenSource(_openTimeout);
                await _faultTolerantSendingLink.OpenAsync(ctx.Token).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Opening MessagingClient", nameof(OpenAsync));
            }
        }

        /// <summary>
        /// Close the MessagingClient instance.
        /// </summary>
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
        /// <param name="timeout">The operation timeout, which defaults to 1 minute if unspecified.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="message"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided <paramref name="deviceId"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        public virtual async Task SendAsync(string deviceId, Message message, TimeSpan? timeout = null)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Sending message with Id [{message?.MessageId}] for device {deviceId}", nameof(SendAsync));

            Argument.RequireNotNullOrEmpty(deviceId, nameof(deviceId));
            Argument.RequireNotNull(message, nameof(message));

            if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset && message.MessageId == null)
            {
                message.MessageId = Guid.NewGuid().ToString();
            }

            if (message.IsBodyCalled)
            {
                message.ResetBody();
            }

            timeout ??= _operationTimeout;

            using AmqpMessage amqpMessage = MessageConverter.MessageToAmqpMessage(message);
            amqpMessage.Properties.To = "/devices/" + WebUtility.UrlEncode(deviceId) + "/messages/deviceBound";

            try
            {
                SendingAmqpLink sendingLink = await GetSendingLinkAsync().ConfigureAwait(false);
                Outcome outcome = await sendingLink
                    .SendMessageAsync(amqpMessage, IotHubConnection.GetNextDeliveryTag(ref _sendingDeliveryTag), AmqpConstants.NullBinary, timeout.Value)
                    .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Outcome was: {outcome?.DescriptorName}", nameof(SendAsync));

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }
            catch (Exception ex) when (!(ex is TimeoutException) && !ex.IsFatal())
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
        /// <param name="timeout">The operation timeout, which defaults to 1 minute if unspecified.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> or <paramref name="message"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        public virtual async Task SendAsync(string deviceId, string moduleId, Message message, TimeSpan? timeout = null)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Sending message with Id [{message?.MessageId}] for device {deviceId}, module {moduleId}", nameof(SendAsync));

            Argument.RequireNotNullOrEmpty(deviceId, nameof(deviceId));
            Argument.RequireNotNullOrEmpty(moduleId, nameof(moduleId));
            Argument.RequireNotNull(message, nameof(message));

            if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset && message.MessageId == null)
            {
                message.MessageId = Guid.NewGuid().ToString();
            }

            if (message.IsBodyCalled)
            {
                message.ResetBody();
            }

            timeout ??= _operationTimeout;

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
                        timeout.Value)
                    .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Outcome was: {outcome?.DescriptorName}", nameof(SendAsync));

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
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
                    sendingLink = await _faultTolerantSendingLink.GetOrCreateAsync(_openTimeout).ConfigureAwait(false);
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
    }
}
