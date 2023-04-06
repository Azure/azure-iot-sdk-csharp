// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        { }

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        internal FileUploadNotificationProcessorClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            RetryHandler retryHandler,
            AmqpConnectionHandler amqpConnection)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _internalRetryHandler = retryHandler;
            _amqpConnection = amqpConnection;
        }

        internal FileUploadNotificationProcessorClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            IotHubServiceClientOptions options,
            RetryHandler retryHandler)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _internalRetryHandler = retryHandler;
            _amqpConnection = new AmqpConnectionHandler(
                credentialProvider,
                options.Protocol,
                AmqpsConstants.FileUploadNotificationsAddress,
                options,
                OnConnectionClosed,
                OnNotificationMessageReceivedAsync);
        }

        /// <summary>
        /// The async callback to be executed each time file upload notification is received from the service.
        /// </summary>
        /// <remarks>
        /// Must not be null. To stop notifications, call <see cref="CloseAsync(CancellationToken)"/>.
        /// <para>
        /// This call is awaited by the client and the return value is used to complete or abandon the notification.
        /// Abandoned notifications will be redelivered to a subscribed client, including this one.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code language="csharp">
        /// serviceClient.FileUploadNotificationProcessor.FileUploadNotificationProcessor = OnFileUploadNotificationReceivedAsync;
        /// await serviceClient.FileUploadNotificationProcessor.OpenAsync();
        ///
        /// //...
        ///
        /// public Task&lt;AcknowledgementType&gt; OnFileUploadNotificationReceivedAsync(FileUploadNotification fileUploadNotification)
        /// {
        ///    Console.WriteLine($"Received file upload notification from device {fileUploadNotification.DeviceId}")
        /// 
        ///    // Make necessary calls to inspect/manage uploaded blob.
        /// 
        ///    return Task.FromResult(AcknowledgementType.Complete);
        /// }
        /// </code>
        /// </example>
        public Func<FileUploadNotification, Task<AcknowledgementType>> FileUploadNotificationProcessor { get; set; }

        /// <summary>
        /// The callback to be executed when the connection is lost.
        /// </summary>
        /// <example>
        /// <code language="csharp">
        /// serviceClient.FileUploadNotificationProcessor.ErrorProcessor = OnConnectionLostAsync;
        /// await serviceClient.FileUploadNotificationProcessor.OpenAsync();
        ///
        /// //...
        ///
        /// public async Task OnConnectionLostAsync(FileUploadNotificationError error)
        /// {
        ///    Console.WriteLine("File upload notification processor connection lost. Error: {error.Exception.Message}");
        ///    
        ///    // Add reconnection logic as needed, for example:
        ///    await serviceClient.FileUploadNotificationProcessor.OpenAsync();
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// This callback will not receive events once <see cref="CloseAsync(CancellationToken)"/> is called. 
        /// This callback will start receiving events again once <see cref="OpenAsync(CancellationToken)"/> is called.
        /// This callback will persist across any number of open/close/open calls, so it does not need to be set before each open call.
        /// </remarks>
        public Func<FileUploadNotificationError, Task> ErrorProcessor { get; set; }

        /// <summary>
        /// Open the connection and start receiving file upload notifications.
        /// </summary>
        /// <remarks>
        /// Callback for file upload notifications must be set before opening the connection.
        /// </remarks>
        /// <exception cref="IotHubServiceException">If an error occurs when communicating with IoT hub service.</exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
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
            catch (Exception ex) when (Logging.IsEnabled)
            {
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
        /// <exception cref="IotHubServiceException">If an error occurs when communicating with IoT hub service.</exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
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
            catch (Exception ex) when (Logging.IsEnabled)
            {
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
                        AmqpClientHelper.ValidateContentType(amqpMessage, AmqpsConstants.FileNotificationContentType);
                        FileUploadNotification fileUploadNotification = await AmqpClientHelper
                            .GetObjectFromAmqpMessageAsync<FileUploadNotification>(amqpMessage)
                            .ConfigureAwait(false);
                        AcknowledgementType ack = await FileUploadNotificationProcessor.Invoke(fileUploadNotification).ConfigureAwait(false);
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

                if (ErrorProcessor != null)
                {
                    try
                    {
                        await ErrorProcessor.Invoke(new FileUploadNotificationError(ex)).ConfigureAwait(false);
                    }
                    catch (Exception ex3)
                    {
                        if (Logging.IsEnabled)
                            Logging.Error(this, $"{nameof(OnNotificationMessageReceivedAsync)} threw an exception during error process invoke: {ex3}", nameof(OnNotificationMessageReceivedAsync));

                        // silently fail
                    }
                }

                try
                {
                    await _amqpConnection.AbandonMessageAsync(amqpMessage.DeliveryTag).ConfigureAwait(false);
                }
                catch (Exception ex2)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, $"{nameof(OnNotificationMessageReceivedAsync)} threw an exception during message abandon: {ex2}", nameof(OnNotificationMessageReceivedAsync));

                    // silently fail
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
                IotHubServiceException mappedException = AmqpClientHelper.GetIotHubExceptionFromAmqpException(exception);
                ErrorProcessor?.Invoke(new FileUploadNotificationError(mappedException));

                if (Logging.IsEnabled)
                {
                    Logging.Error(this, $"{nameof(sender)}.{nameof(OnConnectionClosed)} threw an exception: {mappedException}", nameof(OnConnectionClosed));
                }
            }
            else
            {
                var defaultException = new IOException("AMQP connection was lost", ((AmqpObject)sender).TerminalException);
                var error = new FileUploadNotificationError(defaultException);
                ErrorProcessor?.Invoke(error);

                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(sender)}.{nameof(OnConnectionClosed)} threw an exception: {defaultException}", nameof(OnConnectionClosed));
            }
        }
    }
}
