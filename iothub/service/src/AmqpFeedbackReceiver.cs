﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Extensions;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices
{
    internal sealed class AmqpFeedbackReceiver : FeedbackReceiver<FeedbackBatch>, IDisposable
    {
        private readonly FaultTolerantAmqpObject<ReceivingAmqpLink> _faultTolerantReceivingLink;
        private readonly string _receivingPath;

        public AmqpFeedbackReceiver(IotHubConnection iotHubConnection)
        {
            Connection = iotHubConnection;
            OpenTimeout = IotHubConnection.DefaultOpenTimeout;
            OperationTimeout = IotHubConnection.DefaultOperationTimeout;
            _receivingPath = AmqpClientHelper.GetReceivingPath(EndpointKind.Feedback);
            _faultTolerantReceivingLink = new FaultTolerantAmqpObject<ReceivingAmqpLink>(CreateReceivingLinkAsync, Connection.CloseLink);
        }

        public TimeSpan OpenTimeout { get; private set; }

        public TimeSpan OperationTimeout { get; private set; }

        public IotHubConnection Connection { get; private set; }

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

        [Obsolete("Use ReceiveAsync(CancellationToken cancellationToken).")]
        public override Task<FeedbackBatch> ReceiveAsync()
        {
            return ReceiveAsync(OperationTimeout);
        }

        [Obsolete("Use ReceiveAsync(CancellationToken cancellationToken).")]
        public override async Task<FeedbackBatch> ReceiveAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return await ReceiveAsync(cts.Token).ConfigureAwait(false);
        }

        public override async Task<FeedbackBatch> ReceiveAsync(CancellationToken cancellationToken)
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
                        AmqpClientHelper.ValidateContentType(amqpMessage, CommonConstants.BatchedFeedbackContentType);
                        IEnumerable<FeedbackRecord> records = await AmqpClientHelper
                            .GetObjectFromAmqpMessageAsync<IEnumerable<FeedbackRecord>>(amqpMessage).ConfigureAwait(false);

                        return new FeedbackBatch
                        {
                            EnqueuedTime = (DateTime)amqpMessage.MessageAnnotations.Map[MessageSystemPropertyNames.EnqueuedTime],
                            LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString(),
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

        [Obsolete("Use CompleteAsync(FeeddbackBatch feedback, CancellationToken cancellationToken).")]
        public override Task CompleteAsync(FeedbackBatch feedback)
        {
            return AmqpClientHelper.DisposeMessageAsync(
                _faultTolerantReceivingLink,
                feedback.LockToken,
                AmqpConstants.AcceptedOutcome,
                true);
        }

        public override Task CompleteAsync(FeedbackBatch feedback, CancellationToken cancellationToken)
        {
            return AmqpClientHelper.DisposeMessageAsync(
                _faultTolerantReceivingLink,
                feedback.LockToken,
                AmqpConstants.AcceptedOutcome,
                true,
                cancellationToken);
        }

        [Obsolete("Use AbandonAsync(FeedbackBatch feedback, CancellationToken cancellationToken).")]
        public override Task AbandonAsync(FeedbackBatch feedback)
        {
            return AmqpClientHelper.DisposeMessageAsync(
                _faultTolerantReceivingLink,
                feedback.LockToken,
                AmqpConstants.ReleasedOutcome,
                true);
        }

        public override Task AbandonAsync(FeedbackBatch feedback, CancellationToken cancellationToken)
        {
            return AmqpClientHelper.DisposeMessageAsync(
                _faultTolerantReceivingLink,
                feedback.LockToken,
                AmqpConstants.ReleasedOutcome,
                true,
                cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _faultTolerantReceivingLink.Dispose();
        }
    }
}
