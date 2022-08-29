// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Extensions;

namespace Microsoft.Azure.Devices
{
    internal sealed class AmqpFileNotificationReceiver : IDisposable
    {
        private readonly string _receivingPath;

        public AmqpFileNotificationReceiver(IotHubConnection iotHubConnection)
        {
            Connection = iotHubConnection;
            OpenTimeout = IotHubConnection.DefaultOpenTimeout;
            OperationTimeout = IotHubConnection.DefaultOperationTimeout;
            _receivingPath = AmqpClientHelper.GetReceivingPath(EndpointKind.FileNotification);
            FaultTolerantReceivingLink = new FaultTolerantAmqpObject<ReceivingAmqpLink>(CreateReceivingLinkAsync, Connection.CloseLink);
        }

        public TimeSpan OpenTimeout { get; }

        public TimeSpan OperationTimeout { get; }

        public IotHubConnection Connection { get; }

        public FaultTolerantAmqpObject<ReceivingAmqpLink> FaultTolerantReceivingLink { get; }

        public Task OpenAsync()
        {
            Logging.Enter(this, nameof(OpenAsync));

            try
            {
                return FaultTolerantReceivingLink.GetReceivingLinkAsync();
            }
            finally
            {
                Logging.Exit(this, nameof(OpenAsync));
            }
        }

        public Task CloseAsync()
        {
            Logging.Enter(this, nameof(CloseAsync));

            try
            {
                return FaultTolerantReceivingLink.CloseAsync();
            }
            finally
            {
                Logging.Exit(this, nameof(CloseAsync));
            }
        }

        public async Task<FileUploadNotification> ReceiveAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, nameof(ReceiveAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                ReceivingAmqpLink receivingLink = await FaultTolerantReceivingLink.GetReceivingLinkAsync().ConfigureAwait(false);
                AmqpMessage amqpMessage = await receivingLink.ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);

                Logging.Info(this, $"Message received is [{amqpMessage}]", nameof(ReceiveAsync));

                if (amqpMessage != null)
                {
                    using (amqpMessage)
                    {
                        AmqpClientHelper.ValidateContentType(amqpMessage, AmqpsConstants.FileNotificationContentType);

                        FileUploadNotification fileNotification = await AmqpClientHelper.GetObjectFromAmqpMessageAsync<FileUploadNotification>(amqpMessage).ConfigureAwait(false);
                        fileNotification.DeliveryTag = amqpMessage.DeliveryTag;

                        return fileNotification;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logging.Error(this, ex, nameof(ReceiveAsync));

                if (Fx.IsFatal(ex))
                {
                    throw;
                }

                throw AmqpClientHelper.ToIotHubClientContract(ex);
            }
            finally
            {
                Logging.Exit(this, nameof(ReceiveAsync));
            }
        }

        private Task<ReceivingAmqpLink> CreateReceivingLinkAsync(TimeSpan timeout)
        {
            Logging.Enter(this, timeout, nameof(CreateReceivingLinkAsync));

            try
            {
                return Connection.CreateReceivingLinkAsync(_receivingPath, timeout, 0);
            }
            finally
            {
                Logging.Exit(this, timeout, nameof(CreateReceivingLinkAsync));
            }
        }

        public Task CompleteAsync(FileUploadNotification fileNotification, CancellationToken cancellationToken)
        {
            return AmqpClientHelper.DisposeMessageAsync(
                FaultTolerantReceivingLink,
                fileNotification.DeliveryTag,
                AmqpConstants.AcceptedOutcome,
                false,
                cancellationToken);
        }

        public Task AbandonAsync(FileUploadNotification fileNotification, CancellationToken cancellationToken)
        {
            return AmqpClientHelper.DisposeMessageAsync(
                FaultTolerantReceivingLink,
                fileNotification.DeliveryTag,
                AmqpConstants.ReleasedOutcome,
                false,
                cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            FaultTolerantReceivingLink.Dispose();
        }
    }
}
