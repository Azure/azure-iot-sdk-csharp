// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Extensions;

namespace Microsoft.Azure.Devices
{
    internal sealed class AmqpFeedbackReceiver : FeedbackReceiver<FeedbackBatch>, IDisposable
    {
        private readonly string _receivingPath;

        public AmqpFeedbackReceiver(IotHubConnection iotHubConnection)
        {
            Connection = iotHubConnection;
            OpenTimeout = IotHubConnection.DefaultOpenTimeout;
            OperationTimeout = IotHubConnection.DefaultOperationTimeout;
            _receivingPath = AmqpClientHelper.GetReceivingPath(EndpointKind.Feedback);
            FaultTolerantReceivingLink = new FaultTolerantAmqpObject<ReceivingAmqpLink>(CreateReceivingLinkAsync, Connection.CloseLink);
        }

        public TimeSpan OpenTimeout { get; private set; }

        public TimeSpan OperationTimeout { get; private set; }

        public IotHubConnection Connection { get; private set; }

        public FaultTolerantAmqpObject<ReceivingAmqpLink> FaultTolerantReceivingLink { get; private set; }

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

        public override async Task<FeedbackBatch> ReceiveAsync(CancellationToken cancellationToken)
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
                        AmqpClientHelper.ValidateContentType(amqpMessage, CommonConstants.BatchedFeedbackContentType);
                        IEnumerable<FeedbackRecord> records = await AmqpClientHelper
                            .GetObjectFromAmqpMessageAsync<IEnumerable<FeedbackRecord>>(amqpMessage).ConfigureAwait(false);

                        return new FeedbackBatch
                        {
                            EnqueuedTime = (DateTime)amqpMessage.MessageAnnotations.Map[MessageSystemPropertyNames.EnqueuedTime],
                            DeliveryTag = amqpMessage.DeliveryTag,
                            Records = records,
                            UserId = Encoding.UTF8.GetString(amqpMessage.Properties.UserId.Array, amqpMessage.Properties.UserId.Offset, amqpMessage.Properties.UserId.Count)
                        };
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

        public override Task CompleteAsync(FeedbackBatch feedback, CancellationToken cancellationToken)
        {
            return AmqpClientHelper.DisposeMessageAsync(
                FaultTolerantReceivingLink,
                feedback.DeliveryTag,
                AmqpConstants.AcceptedOutcome,
                false, // Feedback messages are sent by the service one at a time, so batching the acks is pointless
                cancellationToken);
        }

        public override Task AbandonAsync(FeedbackBatch feedback, CancellationToken cancellationToken)
        {
            return AmqpClientHelper.DisposeMessageAsync(
                FaultTolerantReceivingLink,
                feedback.DeliveryTag,
                AmqpConstants.ReleasedOutcome,
                false, // Feedback messages are sent by the service one at a time, so batching the acks is pointless
                cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            FaultTolerantReceivingLink.Dispose();
        }
    }
}
