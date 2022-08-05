using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the AmqpFileNotificationReceiver instance.
        /// </summary>
        public virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"Disposing AmqpFileNotificationReceiver", nameof(Dispose));

                FileNotificationReceiver.Dispose();

                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Disposing AmqpFileNotificationReceiver", nameof(Dispose));
            }
        }
    }
}
