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
    internal sealed class AmqpFileNotificationReceiver : FileNotificationReceiver<FileNotification>, IDisposable
    {
        private readonly FaultTolerantAmqpObject<ReceivingAmqpLink> _faultTolerantReceivingLink;
        private readonly string _receivingPath;

        public AmqpFileNotificationReceiver(IotHubConnection iotHubConnection)
        {
            Connection = iotHubConnection;
            OpenTimeout = IotHubConnection.DefaultOpenTimeout;
            OperationTimeout = IotHubConnection.DefaultOperationTimeout;
            _receivingPath = AmqpClientHelper.GetReceivingPath(EndpointKind.FileNotification);
            _faultTolerantReceivingLink = new FaultTolerantAmqpObject<ReceivingAmqpLink>(CreateReceivingLinkAsync, Connection.CloseLink);
        }

        public TimeSpan OpenTimeout { get; }

        public TimeSpan OperationTimeout { get; }

        public IotHubConnection Connection { get; }

        public Task OpenAsync()
        {
            Logging.Enter(this, nameof(OpenAsync));

            try
            {
                return _faultTolerantReceivingLink.GetReceivingLinkAsync();
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
                return _faultTolerantReceivingLink.CloseAsync();
            }
            finally
            {
                Logging.Exit(this, nameof(CloseAsync));
            }
        }

        public override async Task<FileNotification> ReceiveAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, nameof(ReceiveAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                ReceivingAmqpLink receivingLink = await _faultTolerantReceivingLink.GetReceivingLinkAsync().ConfigureAwait(false);
                AmqpMessage amqpMessage = await receivingLink.ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);

                Logging.Info(this, $"Message received is [{amqpMessage}]", nameof(ReceiveAsync));

                if (amqpMessage != null)
                {
                    using (amqpMessage)
                    {
                        AmqpClientHelper.ValidateContentType(amqpMessage, CommonConstants.FileNotificationContentType);

                        FileNotification fileNotification = await AmqpClientHelper.GetObjectFromAmqpMessageAsync<FileNotification>(amqpMessage).ConfigureAwait(false);
                        fileNotification.LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString();

                        return fileNotification;
                    }
                }

                return null;
            }
            catch (Exception exception)
            {
                Logging.Error(this, exception, nameof(ReceiveAsync));

                if (exception.IsFatal())
                {
                    throw;
                }

                throw AmqpClientHelper.ToIotHubClientContract(exception);
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

        public override Task CompleteAsync(FileNotification fileNotification, CancellationToken cancellationToken)
        {
            return AmqpClientHelper.DisposeMessageAsync(
                _faultTolerantReceivingLink,
                fileNotification.LockToken,
                AmqpConstants.AcceptedOutcome,
                false,
                cancellationToken);
        }

        public override Task AbandonAsync(FileNotification fileNotification, CancellationToken cancellationToken)
        {
            return AmqpClientHelper.DisposeMessageAsync(
                _faultTolerantReceivingLink,
                fileNotification.LockToken,
                AmqpConstants.ReleasedOutcome,
                false,
                cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _faultTolerantReceivingLink.Dispose();
        }
    }
}
