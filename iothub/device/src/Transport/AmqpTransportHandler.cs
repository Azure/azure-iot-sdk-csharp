// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Amqp.Framing;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Shared;
    using System.Collections.Concurrent;
    using Newtonsoft.Json;
    using System.Diagnostics;

    sealed class AmqpTransportHandler : TransportHandler
    {
        const string InputNameKey = "x-opt-input-name";
        public const string ResponseStatusName = "status";
        static readonly IotHubConnectionCache TcpConnectionCache = new IotHubConnectionCache();
        static readonly IotHubConnectionCache WsConnectionCache = new IotHubConnectionCache();
        readonly string deviceId;
        readonly string moduleId;
        readonly Client.FaultTolerantAmqpObject<SendingAmqpLink> faultTolerantEventSendingLink;
        readonly Client.FaultTolerantAmqpObject<ReceivingAmqpLink> faultTolerantDeviceBoundReceivingLink;
        volatile Client.FaultTolerantAmqpObject<SendingAmqpLink> faultTolerantMethodSendingLink;
        volatile Client.FaultTolerantAmqpObject<ReceivingAmqpLink> faultTolerantMethodReceivingLink;
        volatile Client.FaultTolerantAmqpObject<SendingAmqpLink> faultTolerantTwinSendingLink;
        volatile Client.FaultTolerantAmqpObject<ReceivingAmqpLink> faultTolerantTwinReceivingLink;
        volatile Client.FaultTolerantAmqpObject<ReceivingAmqpLink> faultTolerantEventReceivingLink;
        readonly IotHubConnectionString iotHubConnectionString;
        readonly TimeSpan openTimeout;
        readonly TimeSpan operationTimeout;
        readonly uint prefetchCount;

        Func<MethodRequestInternal, Task> methodReceivedListener;
        Func<string, Message, Task> eventReceivedListener;
        Action<TwinCollection> onDesiredStatePatchListener;
        internal delegate void OnConnectionClosedDelegate(object sender, EventArgs e);

        string methodConnectionCorrelationId = Guid.NewGuid().ToString("N");
        string twinConnectionCorrelationId = Guid.NewGuid().ToString("N");

        private string methodSendingLinkName;
        private string methodReceivingLinkName;
        private string twinSendingLinkName;
        private string twinReceivingLinkName;
        private string eventReceivingLinkName;

        const int ResponseTimeoutInSeconds = 300;

        ConcurrentDictionary<string, TaskCompletionSource<AmqpMessage>> twinResponseCompletions = new ConcurrentDictionary<string, TaskCompletionSource<AmqpMessage>>();

        ProductInfo productInfo;

#pragma warning disable CA1810 // Initialize reference type static fields inline: We use the static ctor to have init-once semantics.
        static AmqpTransportHandler()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            try
            {
                AmqpTrace.Provider = new AmqpTransportLog();
            }
            catch (Exception ex)
            {
                // Do not throw from static ctor.
                if (Logging.IsEnabled) Logging.Error(null, ex, nameof(AmqpTransportHandler));
            }
        }

        internal AmqpTransportHandler(
            IPipelineContext context,
            IotHubConnectionString connectionString,
            AmqpTransportSettings transportSettings,
            Func<MethodRequestInternal, Task> onMethodCallback = null,
            Action<TwinCollection> onDesiredStatePatchReceived = null,
            Func<string, Message, Task> onEventReceivedCallback = null)
            : base(context, transportSettings)
        {
            this.productInfo = context.Get<ProductInfo>();

            TransportType transportType = transportSettings.GetTransportType();
            this.deviceId = connectionString.DeviceId;
            this.moduleId = connectionString.ModuleId;

            if (!transportSettings.AmqpConnectionPoolSettings.Pooling)
            {
                this.IotHubConnection = new IotHubSingleTokenConnection(null, connectionString, transportSettings);
            }
            else
            {
                switch (transportType)
                {
                    case TransportType.Amqp_Tcp_Only:
                        this.IotHubConnection = TcpConnectionCache.GetConnection(connectionString, transportSettings);
                        break;
                    case TransportType.Amqp_WebSocket_Only:
                        this.IotHubConnection = WsConnectionCache.GetConnection(connectionString, transportSettings);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid Transport Type {0}".FormatInvariant(transportType));
                }
            }

            this.IotHubConnection.OnConnectionClose += OnAmqpConnectionClose;

            this.openTimeout = transportSettings.OpenTimeout;
            this.operationTimeout = transportSettings.OperationTimeout;
            this.prefetchCount = transportSettings.PrefetchCount;
            this.faultTolerantEventSendingLink = new Client.FaultTolerantAmqpObject<SendingAmqpLink>(this.CreateEventSendingLinkAsync, OnAmqpLinkClose);
            this.faultTolerantDeviceBoundReceivingLink = new Client.FaultTolerantAmqpObject<ReceivingAmqpLink>(this.CreateDeviceBoundReceivingLinkAsync, OnAmqpLinkClose);
            this.iotHubConnectionString = connectionString;
            this.methodReceivedListener = onMethodCallback;
            this.onDesiredStatePatchListener = onDesiredStatePatchReceived;
            this.eventReceivedListener = onEventReceivedCallback;
        }

        private void OnAmqpConnectionClose(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{sender}", nameof(OnAmqpConnectionClose));
            _transportShouldRetry.TrySetResult(true);
        }

        private void OnAmqpLinkClose(AmqpLink sender)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{sender}", nameof(OnAmqpLinkClose));
            this.IotHubConnection.CloseLink(sender);
            _transportShouldRetry.TrySetResult(true);
        }

        internal IotHubConnection IotHubConnection { get; }

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(OpenAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await this.faultTolerantEventSendingLink.OpenAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(this.moduleId))
                {
                    await this.faultTolerantDeviceBoundReceivingLink.OpenAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                Exception newException = AmqpClientHelper.ToIotHubClientContract(exception);
                if (newException != exception)
                {
                    throw newException;
                }
                else
                {
                    // Maintain the original stack.
                    throw;
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(OpenAsync)}");
            }
        }

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, message, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendEventAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                Outcome outcome;
                using (AmqpMessage amqpMessage = message.ToAmqpMessage())
                {
                    outcome = await this.SendAmqpMessageAsync(amqpMessage, cancellationToken).ConfigureAwait(false);
                }

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, message, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendEventAsync)}");
            }
        }

        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, messages, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendEventAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                // List to hold messages in Amqp friendly format
                var messageList = new List<Data>();

                foreach (Message message in messages)
                {
                    using (AmqpMessage amqpMessage = message.ToAmqpMessage())
                    {
                        var data = new Data()
                        {
                            Value = MessageConverter.ReadStream(amqpMessage.ToStream())
                        };
                        messageList.Add(data);
                    }
                }

                Outcome outcome;
                using (AmqpMessage amqpMessage = AmqpMessage.Create(messageList))
                {
                    amqpMessage.MessageFormat = AmqpConstants.AmqpBatchedMessageFormat;
                    outcome = await this.SendAmqpMessageAsync(amqpMessage, cancellationToken).ConfigureAwait(false);
                }

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, messages, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendEventAsync)}");
            }
        }

        public override async Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Message message = null;
            
            AmqpMessage amqpMessage;
            try
            {
                ReceivingAmqpLink deviceBoundReceivingLink = await this.GetDeviceBoundReceivingLinkAsync(cancellationToken).ConfigureAwait(false);
                amqpMessage = await deviceBoundReceivingLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }

            if (amqpMessage != null)
            {
                message = new Message(amqpMessage)
                {
                    LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
                };
            }
            else
            {
                message = null;
            }

            return message;
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(EnableMethodsAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                if (this.faultTolerantMethodSendingLink == null)
                {
                    this.faultTolerantMethodSendingLink = new Client.FaultTolerantAmqpObject<SendingAmqpLink>(this.CreateMethodSendingLinkAsync, OnAmqpLinkClose);
                }

                if (this.faultTolerantMethodReceivingLink == null)
                {
                    this.faultTolerantMethodReceivingLink = new Client.FaultTolerantAmqpObject<ReceivingAmqpLink>(this.CreateMethodReceivingLinkAsync, OnAmqpLinkClose);
                }

                try
                {
                    if (this.methodReceivedListener != null)
                    {
                        await Task.WhenAll(EnableMethodSendingLinkAsync(cancellationToken), EnableMethodReceivingLinkAsync(cancellationToken)).ConfigureAwait(false);
                        // generate new guid for reconnection
                        methodConnectionCorrelationId = Guid.NewGuid().ToString("N");
                    }
                }
                catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
                {
                    throw AmqpClientHelper.ToIotHubClientContract(exception);
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(EnableMethodsAsync)}");
            }
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(EnableTwinPatchAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                if (this.faultTolerantTwinSendingLink == null)
                {
                    this.faultTolerantTwinSendingLink = new Client.FaultTolerantAmqpObject<SendingAmqpLink>(this.CreateTwinSendingLinkAsync, OnAmqpLinkClose);
                }

                if (this.faultTolerantTwinReceivingLink == null)
                {
                    this.faultTolerantTwinReceivingLink = new Client.FaultTolerantAmqpObject<ReceivingAmqpLink>(this.CreateTwinReceivingLinkAsync, OnAmqpLinkClose);
                }

                try
                {
                    if (this.onDesiredStatePatchListener != null)
                    {
                        await Task.WhenAll(EnableTwinSendingLinkAsync(cancellationToken), EnableTwinReceivingLinkAsync(cancellationToken)).ConfigureAwait(false);
                        // generate new guid for reconnection
                        twinConnectionCorrelationId = Guid.NewGuid().ToString("N");
                    }
                }
                catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
                {
                    throw AmqpClientHelper.ToIotHubClientContract(exception);
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(EnableTwinPatchAsync)}");
            }
        }

        public override async Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(EnableEventReceiveAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                if (this.faultTolerantEventReceivingLink == null)
                {
                    this.faultTolerantEventReceivingLink = new Client.FaultTolerantAmqpObject<ReceivingAmqpLink>(this.CreateEventReceivingLinkAsync, OnAmqpLinkClose);
                }

                try
                {
                    if (this.eventReceivedListener != null)
                    {
                        await this.faultTolerantEventReceivingLink.OpenAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
                {
                    throw AmqpClientHelper.ToIotHubClientContract(exception);
                }

            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(EnableEventReceiveAsync)}");
            }
        }

        private async Task EnableMethodSendingLinkAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await this.faultTolerantMethodSendingLink.OpenAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);
        }

        private async Task EnableMethodReceivingLinkAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await this.faultTolerantMethodReceivingLink.OpenAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);
        }

        private async Task EnableTwinSendingLinkAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await this.faultTolerantTwinSendingLink.OpenAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);
        }

        private async Task EnableTwinReceivingLinkAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await this.faultTolerantTwinReceivingLink.OpenAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(DisableMethodsAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                Task receivingLinkCloseTask;

                if (this.faultTolerantMethodReceivingLink != null)
                {
                    receivingLinkCloseTask = this.faultTolerantMethodReceivingLink.CloseAsync(cancellationToken);
                    this.faultTolerantMethodReceivingLink = null;
                }
                else
                {
                    receivingLinkCloseTask = TaskHelpers.CompletedTask;
                }

                Task sendingLinkCloseTask;
                if (this.faultTolerantMethodSendingLink != null)
                {
                    sendingLinkCloseTask = this.faultTolerantMethodSendingLink.CloseAsync(cancellationToken);
                    this.faultTolerantMethodSendingLink = null;
                }
                else
                {
                    sendingLinkCloseTask = TaskHelpers.CompletedTask;
                }

                await Task.WhenAll(receivingLinkCloseTask, sendingLinkCloseTask).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(DisableMethodsAsync)}");
            }
        }

        public async Task DisableTwinAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(DisableTwinAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                Task receivingLinkCloseTask;

                if (this.faultTolerantTwinReceivingLink != null)
                {
                    receivingLinkCloseTask = this.faultTolerantTwinReceivingLink.CloseAsync(cancellationToken);
                    this.faultTolerantTwinReceivingLink = null;
                }
                else
                {
                    receivingLinkCloseTask = TaskHelpers.CompletedTask;
                }

                Task sendingLinkCloseTask;
                if (this.faultTolerantTwinSendingLink != null)
                {
                    sendingLinkCloseTask = this.faultTolerantTwinSendingLink.CloseAsync(cancellationToken);
                    this.faultTolerantTwinSendingLink = null;
                }
                else
                {
                    sendingLinkCloseTask = TaskHelpers.CompletedTask;
                }

                await Task.WhenAll(receivingLinkCloseTask, sendingLinkCloseTask).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(DisableTwinAsync)}");
            }
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, methodResponse, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendMethodResponseAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                Outcome outcome;
                using (AmqpMessage amqpMessage = methodResponse.ToAmqpMessage())
                {
                    outcome = await this.SendAmqpMethodResponseAsync(amqpMessage, cancellationToken).ConfigureAwait(false);
                }

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, methodResponse, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendMethodResponseAsync)}");
            }
        }

        public override Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(CompleteAsync)}");
                cancellationToken.ThrowIfCancellationRequested();
                return this.DisposeMessageAsync(lockToken, AmqpConstants.AcceptedOutcome, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(CompleteAsync)}");
            }
        }

        public override Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(AbandonAsync)}");
                cancellationToken.ThrowIfCancellationRequested();
                return this.DisposeMessageAsync(lockToken, AmqpConstants.ReleasedOutcome, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(AbandonAsync)}");
            }
        }

        public override Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(RejectAsync)}");
                cancellationToken.ThrowIfCancellationRequested();
                return this.DisposeMessageAsync(lockToken, AmqpConstants.RejectedOutcome, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(RejectAsync)}");
            }
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, "", $"{nameof(AmqpTransportHandler)}.{nameof(CloseAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                _transportShouldRetry.TrySetCanceled();

                Task eventSendingLinkCloseTask = this.faultTolerantEventSendingLink.CloseAsync(cancellationToken);
                Task deviceBoundReceivingLinkCloseTask = this.faultTolerantDeviceBoundReceivingLink.CloseAsync(cancellationToken);

                Task disabledMethodTask = this.DisableMethodsAsync(cancellationToken);
                Task disableTwinTask = this.DisableTwinAsync(cancellationToken);
                await Task.WhenAll(eventSendingLinkCloseTask, deviceBoundReceivingLinkCloseTask, disabledMethodTask, disableTwinTask).ConfigureAwait(false);

                this.IotHubConnection.Release(this.deviceId);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, "", $"{nameof(AmqpTransportHandler)}.{nameof(CloseAsync)}");
            }
        }

        async Task<Outcome> SendAmqpMessageAsync(AmqpMessage amqpMessage, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendAmqpMessageAsync)}");
            Outcome outcome;
            try
            {
                SendingAmqpLink eventSendingLink = await this.GetEventSendingLinkAsync(cancellationToken).ConfigureAwait(false);
                outcome = await eventSendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), AmqpConstants.NullBinary, this.operationTimeout).ConfigureAwait(false);
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendAmqpMessageAsync)}");
            }

            return outcome;
        }

        async Task<Outcome> SendAmqpMethodResponseAsync(AmqpMessage amqpMessage, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendAmqpMethodResponseAsync)}");
            Outcome outcome;
            try
            {
                SendingAmqpLink methodRespSendingLink = await this.GetMethodSendingLinkAsync(cancellationToken).ConfigureAwait(false);
                outcome = await methodRespSendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), AmqpConstants.NullBinary, this.operationTimeout).ConfigureAwait(false);
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendAmqpMethodResponseAsync)}");
            }

            return outcome;
        }

        private async Task<AmqpMessage> RoundTripTwinMessage(AmqpMessage amqpMessage, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(RoundTripTwinMessage)}");
            string correlationId = Guid.NewGuid().ToString();
            AmqpMessage response = null;
            
            try
            {
                Outcome outcome;
                SendingAmqpLink eventSendingLink = await this.GetTwinSendingLinkAsync(cancellationToken).ConfigureAwait(false);

                amqpMessage.Properties.CorrelationId = correlationId;

                var taskCompletionSource = new TaskCompletionSource<AmqpMessage>();
                this.twinResponseCompletions[correlationId] = taskCompletionSource;

                outcome = await eventSendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), AmqpConstants.NullBinary, this.operationTimeout).ConfigureAwait(false);
                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }

                var receivingTask = taskCompletionSource.Task;
                if (await Task.WhenAny(receivingTask, Task.Delay(TimeSpan.FromSeconds(ResponseTimeoutInSeconds))).ConfigureAwait(false) == receivingTask)
                {
                    // Task completed within timeout.
                    // Consider that the task may have faulted or been canceled.
                    // We re-await the task so that any exceptions/cancellation is rethrown.
                    response = await receivingTask.ConfigureAwait(false);
                }
                else
                {
                    // Timeout happen
                    throw new TimeoutException();
                }
            }
            finally
            {
                TaskCompletionSource<AmqpMessage> throwAway;
                this.twinResponseCompletions.TryRemove(correlationId, out throwAway);
                if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(RoundTripTwinMessage)}");
            }

            return response;
        }

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendTwinGetAsync)}");
            try
            {
                await EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);

                AmqpMessage amqpMessage = AmqpMessage.Create();
                amqpMessage.MessageAnnotations.Map["operation"] = "GET";

                var response = await RoundTripTwinMessage(amqpMessage, cancellationToken).ConfigureAwait(false);

                return TwinFromResponse(response);
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendTwinGetAsync)}");
            }
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, reportedProperties, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendTwinPatchAsync)}");
            try
            {
                await EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);

                var body = JsonConvert.SerializeObject(reportedProperties);
                var bodyStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));

                var amqpMessage = AmqpMessage.Create(bodyStream, true);
                amqpMessage.MessageAnnotations.Map["operation"] = "PATCH";
                amqpMessage.MessageAnnotations.Map["resource"] = "/properties/reported";
                amqpMessage.MessageAnnotations.Map["version"] = null;

                var response = await RoundTripTwinMessage(amqpMessage, cancellationToken).ConfigureAwait(false);

                VerifyResponseMessage(response);
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, reportedProperties, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendTwinPatchAsync)}");
            }
        }

        private void VerifyResponseMessage(AmqpMessage response)
        {
            if (response != null)
            {
                int status;
                if (response.MessageAnnotations.Map.TryGetValue(ResponseStatusName, out status))
                {
                    if (status >= 400)
                    {
                        throw new InvalidOperationException("Service rejected the message with status: " + status);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Service response is null.");
            }
        }

        private void HandleTwinMessage(AmqpMessage message, ReceivingAmqpLink link)
        {
            link.DisposeDelivery(message, true, AmqpConstants.AcceptedOutcome);

            string correlationId = message.Properties?.CorrelationId?.ToString();
            if (correlationId != null)
            {
                // If we have a correlation id, it must be a response, complete the task.
                TaskCompletionSource<AmqpMessage> task;
                if (this.twinResponseCompletions.TryRemove(correlationId, out task))
                {
                    task.SetResult(message);
                }
            }
            else
            {
                // No correlation id? Must be a patch.
                if (this.onDesiredStatePatchListener != null)
                {
                    using (StreamReader reader = new StreamReader(message.BodyStream, System.Text.Encoding.UTF8))
                    {
                        string patch = reader.ReadToEnd();
                        var props = JsonConvert.DeserializeObject<TwinCollection>(patch);
                        this.onDesiredStatePatchListener(props);
                    }
                }
            }

        }

        private Twin TwinFromResponse(AmqpMessage message)
        {
            using (StreamReader reader = new StreamReader(message.BodyStream, System.Text.Encoding.UTF8))
            {
                string body = reader.ReadToEnd();
                var props = JsonConvert.DeserializeObject<Microsoft.Azure.Devices.Shared.TwinProperties>(body);
                return new Twin(props);
            }
        }


        async Task DisposeMessageAsync(string lockToken, Outcome outcome, CancellationToken cancellationToken)
        {
            ArraySegment<byte> deliveryTag = IotHubConnection.ConvertToDeliveryTag(lockToken);

            Outcome disposeOutcome;
            try
            {
                // Currently, the same mechanism is used for sending feedback for C2D messages and events received by modules.
                // However, devices only support C2D messages (they cannot receive events), and modules only support receiving events
                // (they cannot receive C2D messages). So we use this to distinguish whether to dispose the message (i.e. send outcome on)
                // the DeviceBoundReceivingLink or the EventsReceivingLink. 
                // If this changes (i.e. modules are able to receive C2D messages, or devices are able to receive telemetry), this logic 
                // will have to be updated.
                ReceivingAmqpLink deviceBoundReceivingLink = !string.IsNullOrWhiteSpace(this.moduleId)
                    ? await this.GetEventReceivingLinkAsync(cancellationToken).ConfigureAwait(false)
                    : await this.GetDeviceBoundReceivingLinkAsync(cancellationToken).ConfigureAwait(false);
                disposeOutcome = await deviceBoundReceivingLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: this.operationTimeout).ConfigureAwait(false);
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }

            if (disposeOutcome.DescriptorCode != Accepted.Code)
            {
                if (disposeOutcome.DescriptorCode == Rejected.Code)
                {
                    var rejected = (Rejected)disposeOutcome;

                    // Special treatment for NotFound amqp rejected error code in case of DisposeMessage 
                    if (rejected.Error != null && rejected.Error.Condition.Equals(AmqpErrorCode.NotFound))
                    {
                        throw new DeviceMessageLockLostException(rejected.Error.Description);
                    }
                }

                throw AmqpErrorMapper.GetExceptionFromOutcome(disposeOutcome);
            }
        }

        Task<SendingAmqpLink> GetEventSendingLinkAsync(CancellationToken cancellationToken)
        {
            SendingAmqpLink eventSendingLink;
            if (!this.faultTolerantEventSendingLink.TryGetOpenedObject(out eventSendingLink))
            {
                throw new IotHubCommunicationException("The EventSending AMQP link is not ready.");
            }

            return Task.FromResult(eventSendingLink);
        }

        async Task<SendingAmqpLink> CreateEventSendingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = this.BuildPath(CommonConstants.DeviceEventPathTemplate, CommonConstants.ModuleEventPathTemplate);

            return await this.IotHubConnection.CreateSendingLinkAsync(path, this.iotHubConnectionString, this.deviceId, IotHubConnection.SendingLinkType.TelemetryEvents, timeout, this.productInfo, cancellationToken).ConfigureAwait(false);
        }

        Task<ReceivingAmqpLink> GetDeviceBoundReceivingLinkAsync(CancellationToken cancellationToken)
        {
            ReceivingAmqpLink deviceBoundReceivingLink;
            if (!this.faultTolerantDeviceBoundReceivingLink.TryGetOpenedObject(out deviceBoundReceivingLink))
            {
                throw new IotHubCommunicationException("The DeviceBoundReceiving AMQP link is not ready.");
            }

            return Task.FromResult(deviceBoundReceivingLink);
        }

        async Task<ReceivingAmqpLink> CreateDeviceBoundReceivingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = this.BuildPath(CommonConstants.DeviceBoundPathTemplate, CommonConstants.ModuleBoundPathTemplate);

            return await this.IotHubConnection.CreateReceivingLinkAsync(path, this.iotHubConnectionString, this.deviceId, IotHubConnection.ReceivingLinkType.C2DMessages, this.prefetchCount, timeout, this.productInfo, cancellationToken).ConfigureAwait(false);
        }

        Task<SendingAmqpLink> GetMethodSendingLinkAsync(CancellationToken cancellationToken)
        {
            SendingAmqpLink methodSendingLink;
            if (!this.faultTolerantMethodSendingLink.TryGetOpenedObject(out methodSendingLink))
            {
                throw new IotHubCommunicationException("The MethodSending AMQP link is not ready.");
            }

            return Task.FromResult(methodSendingLink);
        }

        async Task<SendingAmqpLink> CreateMethodSendingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = this.BuildPath(CommonConstants.DeviceMethodPathTemplate, CommonConstants.ModuleMethodPathTemplate);

            SendingAmqpLink methodSendingLink = await this.IotHubConnection.CreateSendingLinkAsync(path, this.iotHubConnectionString, this.methodConnectionCorrelationId, IotHubConnection.SendingLinkType.Methods, timeout, this.productInfo, cancellationToken).ConfigureAwait(false);

            MyStringCopy(methodSendingLink.Name, out methodSendingLinkName);

            methodSendingLink.SafeAddClosed(OnAmqpConnectionClose);
            return methodSendingLink;
        }

        Task<ReceivingAmqpLink> GetMethodReceivingLinkAsync(CancellationToken cancellationToken)
        {
            ReceivingAmqpLink methodReceivingLink;
            if (!this.faultTolerantMethodReceivingLink.TryGetOpenedObject(out methodReceivingLink))
            {
                throw new IotHubCommunicationException("The MethodReceiving AMQP link is not ready.");
            }

            return Task.FromResult(methodReceivingLink);
        }

        async Task<ReceivingAmqpLink> CreateMethodReceivingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = this.BuildPath(CommonConstants.DeviceMethodPathTemplate, CommonConstants.ModuleMethodPathTemplate);

            ReceivingAmqpLink methodReceivingLink = await IotHubConnection.CreateReceivingLinkAsync(path, iotHubConnectionString, methodConnectionCorrelationId, IotHubConnection.ReceivingLinkType.Methods, prefetchCount, timeout, productInfo, cancellationToken).ConfigureAwait(false);
            methodReceivingLink.RegisterMessageListener(amqpMessage =>
                {
                    MethodRequestInternal methodRequestInternal = MethodConverter.ConstructMethodRequestFromAmqpMessage(amqpMessage, cancellationToken);
                    methodReceivingLink.DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
                    this.methodReceivedListener(methodRequestInternal);
                });

            MyStringCopy(methodReceivingLink.Name, out methodReceivingLinkName);

            methodReceivingLink.SafeAddClosed(OnAmqpConnectionClose);

            return methodReceivingLink;
        }

        Task<SendingAmqpLink> GetTwinSendingLinkAsync(CancellationToken cancellationToken)
        {
            SendingAmqpLink twinSendingLink;
            if (!this.faultTolerantTwinSendingLink.TryGetOpenedObject(out twinSendingLink))
            {
                throw new IotHubCommunicationException("The TwinSending AMQP link is not ready.");
            }

            return Task.FromResult(twinSendingLink);
        }

        async Task<SendingAmqpLink> CreateTwinSendingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = this.BuildPath(CommonConstants.DeviceTwinPathTemplate, CommonConstants.ModuleTwinPathTemplate);

            SendingAmqpLink twinSendingLink = await this.IotHubConnection.CreateSendingLinkAsync(path, this.iotHubConnectionString, this.twinConnectionCorrelationId, IotHubConnection.SendingLinkType.Twin, timeout, this.productInfo, cancellationToken).ConfigureAwait(false);

            MyStringCopy(twinSendingLink.Name, out twinSendingLinkName);
            twinSendingLink.SafeAddClosed(async (o, ea) =>
                await Task.Run(async () =>
                    {
                        foreach (var entry in twinResponseCompletions)
                        {
                            TaskCompletionSource<AmqpMessage> task;
                            if (this.twinResponseCompletions.TryRemove(entry.Key, out task))
                            {
                                task.SetCanceled();
                            }

                            OnAmqpConnectionClose(o, ea);
                        }
                    }
            ).ConfigureAwait(false));

            return twinSendingLink;
        }

        Task<ReceivingAmqpLink> GetTwinReceivingLinkAsync(CancellationToken cancellationToken)
        {
            ReceivingAmqpLink twinReceivingLink;
            if (!this.faultTolerantTwinReceivingLink.TryGetOpenedObject(out twinReceivingLink))
            {
                throw new IotHubCommunicationException("The TwinReceiving AMQP link is not ready.");
            }

            return Task.FromResult(twinReceivingLink);
        }

        async Task<ReceivingAmqpLink> CreateTwinReceivingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = this.BuildPath(CommonConstants.DeviceTwinPathTemplate, CommonConstants.ModuleTwinPathTemplate);

            ReceivingAmqpLink twinReceivingLink = await this.IotHubConnection.CreateReceivingLinkAsync(path, this.iotHubConnectionString, this.twinConnectionCorrelationId, IotHubConnection.ReceivingLinkType.Twin, this.prefetchCount, timeout, this.productInfo, cancellationToken).ConfigureAwait(false);

            MyStringCopy(twinReceivingLink.Name, out twinReceivingLinkName);
            twinReceivingLink.SafeAddClosed(OnAmqpConnectionClose);
            twinReceivingLink.RegisterMessageListener(message => this.HandleTwinMessage(message, twinReceivingLink));

            return twinReceivingLink;
        }

        private Task<ReceivingAmqpLink> GetEventReceivingLinkAsync(CancellationToken cancellationToken)
        {
            ReceivingAmqpLink messageReceivingLink;
            if (!this.faultTolerantEventReceivingLink.TryGetOpenedObject(out messageReceivingLink))
            {
                throw new IotHubCommunicationException("The EventReceiving AMQP link is not ready.");
            }

            return Task.FromResult(messageReceivingLink);
        }

        private async Task<ReceivingAmqpLink> CreateEventReceivingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = this.BuildPath(CommonConstants.DeviceEventPathTemplate, CommonConstants.ModuleEventPathTemplate);

            ReceivingAmqpLink messageReceivingLink = await this.IotHubConnection.CreateReceivingLinkAsync(path, this.iotHubConnectionString, this.deviceId, IotHubConnection.ReceivingLinkType.Events, this.prefetchCount, timeout, this.productInfo, cancellationToken).ConfigureAwait(false);
            messageReceivingLink.RegisterMessageListener(amqpMessage => this.ProcessReceivedEventMessage(amqpMessage));

            MyStringCopy(messageReceivingLink.Name, out eventReceivingLinkName);
            messageReceivingLink.SafeAddClosed(OnAmqpConnectionClose);

            return messageReceivingLink;
        }

        private async void ProcessReceivedEventMessage(AmqpMessage amqpMessage)
        {
            Message message = new Message(amqpMessage)
            {
                LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
            };
            await this.eventReceivedListener(message.InputName, message).ConfigureAwait(false);
        }

        private void MyStringCopy(String source, out String destination)
        {
            char[] chars = new Char[source.Length];
            source.CopyTo(0, chars, 0, source.Length);
            destination = new String(chars);
        }

        private string BuildPath(string deviceTemplate, string moduleTemplate)
        {
            string path;
            if (string.IsNullOrEmpty(this.moduleId))
            {
                path = string.Format(CultureInfo.InvariantCulture, deviceTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));
            }
            else
            {
                path = string.Format(CultureInfo.InvariantCulture, moduleTemplate, System.Net.WebUtility.UrlEncode(this.deviceId), System.Net.WebUtility.UrlEncode(this.moduleId));
            }

            return path;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    faultTolerantEventSendingLink?.Dispose();
                    faultTolerantDeviceBoundReceivingLink?.Dispose();
                    faultTolerantMethodSendingLink?.Dispose();
                    faultTolerantMethodReceivingLink?.Dispose();
                    faultTolerantTwinSendingLink?.Dispose();
                    faultTolerantTwinReceivingLink?.Dispose();
                    faultTolerantEventReceivingLink?.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
