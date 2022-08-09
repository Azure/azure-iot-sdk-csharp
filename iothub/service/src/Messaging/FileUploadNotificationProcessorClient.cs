using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common.Exceptions;

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
        /// <remarks>
        /// May not be null.
        /// </remarks>
        public Action<Exception> _errorProcessor;

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
            _connection = new IotHubConnection(credentialProvider, options.UseWebSocketOnly, options.TransportSettings, options);
            FileNotificationReceiver = new AmqpFileNotificationReceiver(_connection);
        }

        /// <summary>
        /// Gets the AmqpFileNotificationReceiver which can deliver notifications for file upload operations.
        /// </summary>
        internal AmqpFileNotificationReceiver FileNotificationReceiver { get; }

        /// <summary>
        /// Open the AmqpFileNotificationReceiver instance.
        /// </summary>
        public virtual async Task OpenAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Opening AmqpFileNotificationReceiver", nameof(OpenAsync));

            try
            {
                await FileNotificationReceiver.OpenAsync().ConfigureAwait(false);
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
                    Logging.Exit(this, $"Opening AmqpFileNotificationReceiver", nameof(OpenAsync));
            }
        }

        /// <summary>
        /// Receive file upload notification.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task ReceiveAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(ReceiveAsync));
            try
            {
                FileNotification notification = await FileNotificationReceiver.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                if (notification != null && _fileNotificationProcessor != null)
                {
                    _fileNotificationProcessor.Invoke(notification);
                }
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ReceiveAsync)} threw an exception: {ex}", nameof(ReceiveAsync));
                if (ex is IotHubException)
                {
                    _errorProcessor?.Invoke(ex);
                }
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(ReceiveAsync));
            }
        }

        /// <summary>
        /// Close the AmqpFileNotificationReceiver instance.
        /// </summary>
        public virtual async Task CloseAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Closing AmqpFileNotificationReceiver", nameof(CloseAsync));

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
                    Logging.Exit(this, $"Closing AmqpFileNotificationReceiver", nameof(CloseAsync));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Disposing AmqpFileNotificationReceiver", nameof(Dispose));

            FileNotificationReceiver.Dispose();

            if (Logging.IsEnabled)
                Logging.Exit(this, $"Disposing AmqpFileNotificationReceiver", nameof(Dispose));
            GC.SuppressFinalize(this);
        }
    }
}
