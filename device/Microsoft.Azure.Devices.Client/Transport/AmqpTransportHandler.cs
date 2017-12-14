﻿// Copyright (c) Microsoft. All rights reserved.
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
    using System.Linq.Expressions;
    using Newtonsoft.Json;

    sealed class AmqpTransportHandler : TransportHandler
    {
        static readonly IotHubConnectionCache TcpConnectionCache = new IotHubConnectionCache();
        static readonly IotHubConnectionCache WsConnectionCache = new IotHubConnectionCache();
        readonly string deviceId;
        readonly Client.FaultTolerantAmqpObject<SendingAmqpLink> faultTolerantEventSendingLink;
        readonly Client.FaultTolerantAmqpObject<ReceivingAmqpLink> faultTolerantDeviceBoundReceivingLink;
        volatile Client.FaultTolerantAmqpObject<SendingAmqpLink> faultTolerantMethodSendingLink;
        volatile Client.FaultTolerantAmqpObject<ReceivingAmqpLink> faultTolerantMethodReceivingLink;
        volatile Client.FaultTolerantAmqpObject<SendingAmqpLink> faultTolerantTwinSendingLink;
        volatile Client.FaultTolerantAmqpObject<ReceivingAmqpLink> faultTolerantTwinReceivingLink;
        readonly IotHubConnectionString iotHubConnectionString;
        readonly TimeSpan openTimeout;
        readonly TimeSpan operationTimeout;
        readonly uint prefetchCount;
        readonly SemaphoreSlim recoverySemaphore = new SemaphoreSlim(1, 1);

        Func<MethodRequestInternal, Task> messageListener;

        Action<TwinCollection> onDesiredStatePatchListener;
        Action<object, ConnectionEventArgs> linkOpenedListener;
        Func<object, ConnectionEventArgs, Task> linkClosedListener;
        Func<object, ConnectionEventArgs, Task> SafeAddClosedMethodReceivingLinkHandler;
        Func<object, ConnectionEventArgs, Task> SafeAddClosedMethodSendingLinkHandler;
        Func<object, ConnectionEventArgs, Task> SafeAddClosedTwinReceivingLinkHandler;
        Func<object, ConnectionEventArgs, Task> SafeAddClosedTwinSendingLinkHandler;
        internal delegate void OnConnectionClosedDelegate(object sender, EventArgs e);

        string methodConnectionCorrelationId = Guid.NewGuid().ToString("N");
        string twinConnectionCorrelationId = Guid.NewGuid().ToString("N");

        private string methodSendingLinkName;
        private string methodReceivingLinkName;
        private string twinSendingLinkName;
        private string twinReceivingLinkName;

        const int ResponseTimeoutInSeconds = 10;

        ConcurrentDictionary<string, TaskCompletionSource<AmqpMessage>> twinResponseCompletions = new ConcurrentDictionary<string, TaskCompletionSource<AmqpMessage>>();

        ProductInfo productInfo;

        internal AmqpTransportHandler(
            IPipelineContext context, IotHubConnectionString connectionString, 
            AmqpTransportSettings transportSettings,
            Action<object, ConnectionEventArgs> onLinkOpenedCallback,
            Func<object, ConnectionEventArgs, Task> onLinkClosedCallback,
            Func<MethodRequestInternal, Task> onMethodCallback = null,
            Action<TwinCollection> onDesiredStatePatchReceived = null)
            :base(context, transportSettings)
        {
            this.linkOpenedListener = onLinkOpenedCallback;
            this.linkClosedListener = onLinkClosedCallback;

            this.productInfo = context.Get<ProductInfo>();

            TransportType transportType = transportSettings.GetTransportType();
            this.deviceId = connectionString.DeviceId;
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

            this.openTimeout = transportSettings.OpenTimeout;
            this.operationTimeout = transportSettings.OperationTimeout;
            this.prefetchCount = transportSettings.PrefetchCount;
            this.faultTolerantEventSendingLink = new Client.FaultTolerantAmqpObject<SendingAmqpLink>(this.CreateEventSendingLinkAsync, this.IotHubConnection.CloseLink);
            this.faultTolerantDeviceBoundReceivingLink = new Client.FaultTolerantAmqpObject<ReceivingAmqpLink>(this.CreateDeviceBoundReceivingLinkAsync, this.IotHubConnection.CloseLink);
            this.iotHubConnectionString = connectionString;
            this.messageListener = onMethodCallback;
            this.onDesiredStatePatchListener = onDesiredStatePatchReceived;
        }

        internal IotHubConnection IotHubConnection { get; }

        public override async Task OpenAsync(bool explicitOpen, CancellationToken cancellationToken)
        {
            if (!explicitOpen)
            {
                return;
            }

            await this.HandleTimeoutCancellation(async () =>
             {
                 try
                 {
                     await Task.WhenAll(
                         this.faultTolerantEventSendingLink.OpenAsync(this.openTimeout, cancellationToken),
                         this.faultTolerantDeviceBoundReceivingLink.OpenAsync(this.openTimeout, cancellationToken)).ConfigureAwait(false);
                     this.linkOpenedListener(
                         this.faultTolerantEventSendingLink, 
                         new ConnectionEventArgs { ConnectionType = ConnectionType.AmqpTelemetry, ConnectionStatus = ConnectionStatus.Connected, ConnectionStatusChangeReason = ConnectionStatusChangeReason.Connection_Ok });
                     this.linkOpenedListener(
                         this.faultTolerantDeviceBoundReceivingLink, 
                         new ConnectionEventArgs { ConnectionType = ConnectionType.AmqpMessaging, ConnectionStatus = ConnectionStatus.Connected, ConnectionStatusChangeReason = ConnectionStatusChangeReason.Connection_Ok });
                 }
                 catch (Exception exception)
                 {
                     if (exception.IsFatal())
                     {
                         throw;
                     }

                     throw AmqpClientHelper.ToIotHubClientContract(exception);
                 }
             }, cancellationToken).ConfigureAwait(false);
        }

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            await this.HandleTimeoutCancellation(async () =>
            {
                Outcome outcome;
                using (AmqpMessage amqpMessage = message.ToAmqpMessage())
                {
                    outcome = await this.SendAmqpMessageAsync(amqpMessage, cancellationToken).ConfigureAwait(false);
                }

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            await this.HandleTimeoutCancellation(async () =>
            {
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
            }, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Message message = null;

            await this.HandleTimeoutCancellation(async () =>
            {
                AmqpMessage amqpMessage;
                try
                {
                    ReceivingAmqpLink deviceBoundReceivingLink = await this.GetDeviceBoundReceivingLinkAsync(cancellationToken).ConfigureAwait(false);
                    amqpMessage = await deviceBoundReceivingLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (exception.IsFatal())
                    {
                        throw;
                    }

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
            }, cancellationToken).ConfigureAwait(false);

            return message;
        }

        public override async Task RecoverConnections(object link, ConnectionType connectionType, CancellationToken cancellationToken)
        {
            bool needMethodRecovery = false;
            bool needTwinRecovery = false;

            await recoverySemaphore.WaitAsync().ConfigureAwait(false);

            // disconnected link belongs to the current sets
            if (((connectionType == ConnectionType.AmqpMethodSending) &&
                 ((link as SendingAmqpLink).Name == methodSendingLinkName)) ||
                ((connectionType == ConnectionType.AmqpMethodReceiving) &&
                 ((link as ReceivingAmqpLink).Name == methodReceivingLinkName)))
            {
                methodSendingLinkName = null;
                methodReceivingLinkName = null;
                needMethodRecovery = true;
            }

            if (((connectionType == ConnectionType.AmqpTwinSending) &&
                 ((link as SendingAmqpLink).Name == twinSendingLinkName)) ||
                ((connectionType == ConnectionType.AmqpTwinReceiving) &&
                 ((link as ReceivingAmqpLink).Name == twinReceivingLinkName)))
            {
                twinSendingLinkName = null;
                twinReceivingLinkName = null;
                needTwinRecovery = true;
            }

            recoverySemaphore.Release(1);

            if (needMethodRecovery)
            {
                this.faultTolerantMethodSendingLink = null;
                this.faultTolerantMethodReceivingLink = null;
                await this.EnableMethodsAsync(cancellationToken).ConfigureAwait(false);
            }

            if (needTwinRecovery)
            {
                this.faultTolerantTwinSendingLink = null;
                this.faultTolerantTwinReceivingLink = null;
                await this.EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            if (this.faultTolerantMethodSendingLink == null)
            {
                this.faultTolerantMethodSendingLink = new Client.FaultTolerantAmqpObject<SendingAmqpLink>(this.CreateMethodSendingLinkAsync, this.IotHubConnection.CloseLink);
            }

            if (this.faultTolerantMethodReceivingLink == null)
            {
                this.faultTolerantMethodReceivingLink = new Client.FaultTolerantAmqpObject<ReceivingAmqpLink>(this.CreateMethodReceivingLinkAsync, this.IotHubConnection.CloseLink);
            }

            await this.HandleTimeoutCancellation(async () =>
            {
                try
                {
                    if (this.messageListener != null)
                    {
                        await Task.WhenAll(EnableMethodSendingLinkAsync(cancellationToken), EnableMethodReceivingLinkAsync(cancellationToken)).ConfigureAwait(false);
                        this.linkOpenedListener(
                            this.faultTolerantMethodSendingLink, 
                            new ConnectionEventArgs { ConnectionType = ConnectionType.AmqpMethodSending, ConnectionStatus = ConnectionStatus.Connected, ConnectionStatusChangeReason = ConnectionStatusChangeReason.Connection_Ok });
                        this.linkOpenedListener(
                            this.faultTolerantMethodReceivingLink, 
                            new ConnectionEventArgs { ConnectionType = ConnectionType.AmqpMethodReceiving, ConnectionStatus = ConnectionStatus.Connected, ConnectionStatusChangeReason = ConnectionStatusChangeReason.Connection_Ok });
                        // generate new guid for reconnection
                        methodConnectionCorrelationId = Guid.NewGuid().ToString("N");
                    }
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    throw AmqpClientHelper.ToIotHubClientContract(ex);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            if (this.faultTolerantTwinSendingLink == null)
            {
                this.faultTolerantTwinSendingLink = new Client.FaultTolerantAmqpObject<SendingAmqpLink>(this.CreateTwinSendingLinkAsync, this.IotHubConnection.CloseLink);
            }

            if (this.faultTolerantTwinReceivingLink == null)
            {
                this.faultTolerantTwinReceivingLink = new Client.FaultTolerantAmqpObject<ReceivingAmqpLink>(this.CreateTwinReceivingLinkAsync, this.IotHubConnection.CloseLink);
            }

            await this.HandleTimeoutCancellation(async () =>
            {
                try
                {
                    if (this.messageListener != null)
                    {
                        await Task.WhenAll(EnableTwinSendingLinkAsync(cancellationToken), EnableTwinReceivingLinkAsync(cancellationToken)).ConfigureAwait(false);
                        this.linkOpenedListener(
                            this.faultTolerantTwinSendingLink, 
                            new ConnectionEventArgs { ConnectionType = ConnectionType.AmqpTwinSending, ConnectionStatus = ConnectionStatus.Connected, ConnectionStatusChangeReason = ConnectionStatusChangeReason.Connection_Ok });
                        this.linkOpenedListener(
                            this.faultTolerantTwinReceivingLink, 
                            new ConnectionEventArgs { ConnectionType = ConnectionType.AmqpTwinReceiving, ConnectionStatus = ConnectionStatus.Connected, ConnectionStatusChangeReason = ConnectionStatusChangeReason.Connection_Ok });
                        // generate new guid for reconnection
                        twinConnectionCorrelationId = Guid.NewGuid().ToString("N");
                    }
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    throw AmqpClientHelper.ToIotHubClientContract(ex);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private async Task EnableMethodSendingLinkAsync(CancellationToken cancellationToken)
        {
            await this.GetMethodSendingLinkAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task EnableMethodReceivingLinkAsync(CancellationToken cancellationToken)
        {
            await this.GetMethodReceivingLinkAsync(cancellationToken).ConfigureAwait(false);
        }
        
        private async Task EnableTwinSendingLinkAsync(CancellationToken cancellationToken)
        {
            await this.GetTwinSendingLinkAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task EnableTwinReceivingLinkAsync(CancellationToken cancellationToken)
        {
            await this.GetTwinReceivingLinkAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            Task receivingLinkCloseTask;
            
            this.SafeAddClosedMethodSendingLinkHandler = (o, ea) => { return TaskHelpers.CompletedTask; };
            this.SafeAddClosedMethodReceivingLinkHandler = (o, ea) => { return TaskHelpers.CompletedTask; };
            
            if (this.faultTolerantMethodReceivingLink != null)
            {
                receivingLinkCloseTask = this.faultTolerantMethodReceivingLink.CloseAsync();
                this.faultTolerantMethodReceivingLink = null;
            }
            else
            {
                receivingLinkCloseTask = TaskHelpers.CompletedTask;
            }

            Task sendingLinkCloseTask;
            if (this.faultTolerantMethodSendingLink != null)
            {
                sendingLinkCloseTask = this.faultTolerantMethodSendingLink.CloseAsync();
                this.faultTolerantMethodSendingLink = null;
            }
            else
            {
                sendingLinkCloseTask = TaskHelpers.CompletedTask;
            }

            await Task.WhenAll(receivingLinkCloseTask, sendingLinkCloseTask).ConfigureAwait(false);
            await this.linkClosedListener(
                this.faultTolerantMethodSendingLink, 
                new ConnectionEventArgs { ConnectionType = ConnectionType.AmqpMethodSending, ConnectionStatus = ConnectionStatus.Disabled, ConnectionStatusChangeReason = ConnectionStatusChangeReason.Client_Close }).ConfigureAwait(false);
            await this.linkClosedListener(
                this.faultTolerantMethodReceivingLink, 
                new ConnectionEventArgs { ConnectionType = ConnectionType.AmqpMethodReceiving, ConnectionStatus = ConnectionStatus.Disabled, ConnectionStatusChangeReason = ConnectionStatusChangeReason.Client_Close }).ConfigureAwait(false);
        }

        public async Task DisableTwinAsync(CancellationToken cancellationToken)
        {
            Task receivingLinkCloseTask;

            this.SafeAddClosedTwinSendingLinkHandler = (o, ea) => { return TaskHelpers.CompletedTask; };
            this.SafeAddClosedTwinReceivingLinkHandler = (o, ea) => { return TaskHelpers.CompletedTask; };
            
            if (this.faultTolerantTwinReceivingLink != null)
            {
                receivingLinkCloseTask = this.faultTolerantTwinReceivingLink.CloseAsync();
                this.faultTolerantTwinReceivingLink = null;
            }
            else
            {
                receivingLinkCloseTask = TaskHelpers.CompletedTask;
            }

            Task sendingLinkCloseTask;
            if (this.faultTolerantTwinSendingLink != null)
            {
                sendingLinkCloseTask = this.faultTolerantTwinSendingLink.CloseAsync();
                this.faultTolerantTwinSendingLink = null;
            }
            else
            {
                sendingLinkCloseTask = TaskHelpers.CompletedTask;
            }

            await Task.WhenAll(receivingLinkCloseTask, sendingLinkCloseTask).ConfigureAwait(false);
            await this.linkClosedListener(
                this.faultTolerantTwinSendingLink, 
                new ConnectionEventArgs { ConnectionType = ConnectionType.AmqpTwinSending, ConnectionStatus = ConnectionStatus.Disabled, ConnectionStatusChangeReason = ConnectionStatusChangeReason.Client_Close }).ConfigureAwait(false);
            await this.linkClosedListener(
                this.faultTolerantTwinReceivingLink, 
                new ConnectionEventArgs { ConnectionType = ConnectionType.AmqpTwinReceiving, ConnectionStatus = ConnectionStatus.Disabled, ConnectionStatusChangeReason = ConnectionStatusChangeReason.Client_Close }).ConfigureAwait(false);
        }
        
        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            await this.HandleTimeoutCancellation(async () =>
            {
                Outcome outcome;
                using (AmqpMessage amqpMessage = methodResponse.ToAmqpMessage())
                {
                    outcome = await this.SendAmqpMethodResponseAsync(amqpMessage, cancellationToken).ConfigureAwait(false);
                }

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public override Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            return this.HandleTimeoutCancellation(() => this.DisposeMessageAsync(lockToken, AmqpConstants.AcceptedOutcome, cancellationToken), cancellationToken);
        }

        public override Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            return this.HandleTimeoutCancellation(() => this.DisposeMessageAsync(lockToken, AmqpConstants.ReleasedOutcome, cancellationToken), cancellationToken);
        }

        public override Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            return this.HandleTimeoutCancellation(() => this.DisposeMessageAsync(lockToken, AmqpConstants.RejectedOutcome, cancellationToken), cancellationToken);
        }

        protected override async void Dispose(bool disposing)
        {
            try
            {
                await this.CloseAsync().ConfigureAwait(false);
            }
            catch
            {
                // TODO: add traces here
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override async Task CloseAsync()
        {
            GC.SuppressFinalize(this);
            Task eventSendingLinkCloseTask = this.faultTolerantEventSendingLink.CloseAsync();
            Task deviceBoundReceivingLinkCloseTask = this.faultTolerantDeviceBoundReceivingLink.CloseAsync();

            Task disabledMethodTask = this.DisableMethodsAsync(CancellationToken.None);
            Task disableTwinTask = this.DisableTwinAsync(CancellationToken.None);
            await Task.WhenAll(eventSendingLinkCloseTask, deviceBoundReceivingLinkCloseTask, disabledMethodTask, disableTwinTask).ConfigureAwait(false);

            await this.linkClosedListener(
                this.faultTolerantEventSendingLink, 
                new ConnectionEventArgs { ConnectionType = ConnectionType.AmqpTelemetry, ConnectionStatus = ConnectionStatus.Disabled, ConnectionStatusChangeReason = ConnectionStatusChangeReason.Client_Close }).ConfigureAwait(false);
            await this.linkClosedListener(
                this.faultTolerantDeviceBoundReceivingLink, 
                new ConnectionEventArgs { ConnectionType = ConnectionType.AmqpMessaging, ConnectionStatus = ConnectionStatus.Disabled, ConnectionStatusChangeReason = ConnectionStatusChangeReason.Client_Close }).ConfigureAwait(false);

            this.IotHubConnection.Release(this.deviceId);
        }

        async Task<Outcome> SendAmqpMessageAsync(AmqpMessage amqpMessage, CancellationToken cancellationToken)
        {
            Outcome outcome;
            try
            {
                SendingAmqpLink eventSendingLink = await this.GetEventSendingLinkAsync(cancellationToken).ConfigureAwait(false);
                outcome = await eventSendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), AmqpConstants.NullBinary, this.operationTimeout).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                {
                    throw;
                }

                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }

            return outcome;
        }

        async Task<Outcome> SendAmqpMethodResponseAsync(AmqpMessage amqpMessage, CancellationToken cancellationToken)
        {
            Outcome outcome;
            try
            {
                SendingAmqpLink methodRespSendingLink = await this.GetMethodSendingLinkAsync(cancellationToken).ConfigureAwait(false);
                outcome = await methodRespSendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), AmqpConstants.NullBinary, this.operationTimeout).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                {
                    throw;
                }

                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }

            return outcome;
        }

        private async Task<AmqpMessage> RoundTripTwinMessage(AmqpMessage amqpMessage, CancellationToken cancellationToken)
        {
            string correlationId = Guid.NewGuid().ToString();
            AmqpMessage response = null;

            
            try
            {
                Outcome outcome;
                SendingAmqpLink eventSendingLink = await this.GetTwinSendingLinkAsync(cancellationToken).ConfigureAwait(false);

                amqpMessage.Properties.CorrelationId = correlationId;
                
                this.twinResponseCompletions[correlationId] = new TaskCompletionSource<AmqpMessage>();

                outcome = await eventSendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), AmqpConstants.NullBinary, this.operationTimeout).ConfigureAwait(false);
                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }

                var receivingTask = this.twinResponseCompletions[correlationId].Task;
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
            }

            return response;
        }
        
        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            try
            {
                await EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                
                AmqpMessage amqpMessage = AmqpMessage.Create();
                amqpMessage.MessageAnnotations.Map["operation"] = "GET";

                var response = await RoundTripTwinMessage(amqpMessage, cancellationToken).ConfigureAwait(false);

                return TwinFromResponse(response);
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                {
                    throw;
                }

                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            try
            {
                await EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);

                var body = JsonConvert.SerializeObject(reportedProperties);
                var bodyStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));

                var amqpMessage = AmqpMessage.Create(bodyStream, true);
                amqpMessage.MessageAnnotations.Map["operation"] = "PATCH";
                amqpMessage.MessageAnnotations.Map["resource"] = "/properties/reported";
                amqpMessage.MessageAnnotations.Map["version"] = null;

                await RoundTripTwinMessage(amqpMessage, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                {
                    throw;
                }

                throw AmqpClientHelper.ToIotHubClientContract(exception);
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
                var twin = new Twin();
                twin.Properties = props;
                return twin;
            }
        }


        async Task DisposeMessageAsync(string lockToken, Outcome outcome, CancellationToken cancellationToken)
        {
            ArraySegment<byte> deliveryTag = IotHubConnection.ConvertToDeliveryTag(lockToken);

            Outcome disposeOutcome;
            try
            {
                ReceivingAmqpLink deviceBoundReceivingLink = await this.GetDeviceBoundReceivingLinkAsync(cancellationToken).ConfigureAwait(false);
                disposeOutcome = await deviceBoundReceivingLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: this.operationTimeout).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                {
                    throw;
                }

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

        async Task<SendingAmqpLink> GetEventSendingLinkAsync(CancellationToken cancellationToken)
        {
            SendingAmqpLink eventSendingLink;
            if (!this.faultTolerantEventSendingLink.TryGetOpenedObject(out eventSendingLink))
            {
                eventSendingLink = await this.faultTolerantEventSendingLink.GetOrCreateAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);
            }
            return eventSendingLink;
        }

        async Task<SendingAmqpLink> CreateEventSendingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = string.Format(CultureInfo.InvariantCulture, CommonConstants.DeviceEventPathTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));

            return await this.IotHubConnection.CreateSendingLinkAsync(path, this.iotHubConnectionString, this.deviceId, IotHubConnection.SendingLinkType.TelemetryEvents, timeout, this.productInfo, cancellationToken).ConfigureAwait(false);
        }

        async Task<ReceivingAmqpLink> GetDeviceBoundReceivingLinkAsync(CancellationToken cancellationToken)
        {
            ReceivingAmqpLink deviceBoundReceivingLink;
            if (!this.faultTolerantDeviceBoundReceivingLink.TryGetOpenedObject(out deviceBoundReceivingLink))
            {
                deviceBoundReceivingLink = await this.faultTolerantDeviceBoundReceivingLink.GetOrCreateAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);
            }

            return deviceBoundReceivingLink;
        }

        async Task<ReceivingAmqpLink> CreateDeviceBoundReceivingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = string.Format(CultureInfo.InvariantCulture, CommonConstants.DeviceBoundPathTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));

            return await this.IotHubConnection.CreateReceivingLinkAsync(path, this.iotHubConnectionString, this.deviceId, IotHubConnection.ReceivingLinkType.C2DMessages, this.prefetchCount, timeout, this.productInfo, cancellationToken).ConfigureAwait(false);
        }

        async Task<SendingAmqpLink> GetMethodSendingLinkAsync(CancellationToken cancellationToken)
        {
            SendingAmqpLink methodSendingLink;
            if (!this.faultTolerantMethodSendingLink.TryGetOpenedObject(out methodSendingLink))
            {
                methodSendingLink = await this.faultTolerantMethodSendingLink.GetOrCreateAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);
            }
            return methodSendingLink;
        }

        async Task<SendingAmqpLink> CreateMethodSendingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = string.Format(CultureInfo.InvariantCulture, CommonConstants.DeviceMethodPathTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));

            SendingAmqpLink methodSendingLink = await this.IotHubConnection.CreateSendingLinkAsync(path, this.iotHubConnectionString, this.methodConnectionCorrelationId, IotHubConnection.SendingLinkType.Methods, timeout, this.productInfo, cancellationToken).ConfigureAwait(false);

            MyStringCopy(methodSendingLink.Name, out methodSendingLinkName);
            this.SafeAddClosedMethodSendingLinkHandler = this.linkClosedListener;
            methodSendingLink.SafeAddClosed(async (o, ea) =>
                await Task.Run(async () =>
                    {
                        await this.SafeAddClosedMethodSendingLinkHandler(
                            o,
                            new ConnectionEventArgs
                            {
                                ConnectionType = ConnectionType.AmqpMethodSending,
                                ConnectionStatus = ConnectionStatus.Disconnected_Retrying,
                                ConnectionStatusChangeReason = ConnectionStatusChangeReason.No_Network
                            }).ConfigureAwait(false);
                    }
                ).ConfigureAwait(false));
            return methodSendingLink;
        }

        async Task<ReceivingAmqpLink> GetMethodReceivingLinkAsync(CancellationToken cancellationToken)
        {
            ReceivingAmqpLink methodReceivingLink;
            if (!this.faultTolerantMethodReceivingLink.TryGetOpenedObject(out methodReceivingLink))
            {
                methodReceivingLink = await this.faultTolerantMethodReceivingLink.GetOrCreateAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);
            }

            return methodReceivingLink;
        }

        async Task<ReceivingAmqpLink> CreateMethodReceivingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = string.Format(CultureInfo.InvariantCulture, CommonConstants.DeviceMethodPathTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));

            ReceivingAmqpLink methodReceivingLink = await this.IotHubConnection.CreateReceivingLinkAsync(path, this.iotHubConnectionString, this.methodConnectionCorrelationId, IotHubConnection.ReceivingLinkType.Methods, this.prefetchCount, timeout, this.productInfo, cancellationToken);
            methodReceivingLink.RegisterMessageListener(amqpMessage => 
                {
                    MethodRequestInternal methodRequestInternal = MethodConverter.ConstructMethodRequestFromAmqpMessage(amqpMessage);
                    methodReceivingLink.DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
                    this.messageListener(methodRequestInternal);
                });

            MyStringCopy(methodReceivingLink.Name, out methodReceivingLinkName);
            this.SafeAddClosedMethodReceivingLinkHandler = this.linkClosedListener;
            methodReceivingLink.SafeAddClosed(async (o, ea) =>
                await Task.Run(async () =>
                    {
                        await this.SafeAddClosedMethodReceivingLinkHandler(
                            o,
                            new ConnectionEventArgs
                            {
                                ConnectionType = ConnectionType.AmqpMethodReceiving,
                                ConnectionStatus = ConnectionStatus.Disconnected_Retrying,
                                ConnectionStatusChangeReason = ConnectionStatusChangeReason.No_Network
                            }).ConfigureAwait(false);
                    }
                ).ConfigureAwait(false));

            return methodReceivingLink;
        }

        async Task<SendingAmqpLink> GetTwinSendingLinkAsync(CancellationToken cancellationToken)
        {
            SendingAmqpLink twinSendingLink;
            if (!this.faultTolerantTwinSendingLink.TryGetOpenedObject(out twinSendingLink))
            {
                twinSendingLink = await this.faultTolerantTwinSendingLink.GetOrCreateAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);
            }
            return twinSendingLink;
        }

        async Task<SendingAmqpLink> CreateTwinSendingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = string.Format(CultureInfo.InvariantCulture, CommonConstants.DeviceTwinPathTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));

            SendingAmqpLink twinSendingLink = await this.IotHubConnection.CreateSendingLinkAsync(path, this.iotHubConnectionString, this.twinConnectionCorrelationId, IotHubConnection.SendingLinkType.Twin, timeout, this.productInfo, cancellationToken).ConfigureAwait(false);

            MyStringCopy(twinSendingLink.Name, out twinSendingLinkName);
            this.SafeAddClosedTwinSendingLinkHandler = this.linkClosedListener;
            twinSendingLink.SafeAddClosed(async (o, ea) => 
                await Task.Run(async () =>
                    {
                        await this.SafeAddClosedTwinSendingLinkHandler(
                            o,
                            new ConnectionEventArgs
                            {
                                ConnectionType = ConnectionType.AmqpTwinSending,
                                ConnectionStatus = ConnectionStatus.Disconnected_Retrying,
                                ConnectionStatusChangeReason = ConnectionStatusChangeReason.No_Network
                            }).ConfigureAwait(false);
                        foreach (var entry in twinResponseCompletions)
                        {
                            TaskCompletionSource<AmqpMessage> task;
                            if (this.twinResponseCompletions.TryRemove(entry.Key, out task))
                            {
                                task.SetCanceled();
                            }
                        }
                    }
            ).ConfigureAwait(false));

            return twinSendingLink;
        }

        async Task<ReceivingAmqpLink> GetTwinReceivingLinkAsync(CancellationToken cancellationToken)
        {
            ReceivingAmqpLink twinReceivingLink;
            if (!this.faultTolerantTwinReceivingLink.TryGetOpenedObject(out twinReceivingLink))
            {
                twinReceivingLink = await this.faultTolerantTwinReceivingLink.GetOrCreateAsync(this.openTimeout, cancellationToken).ConfigureAwait(false);
            }

            return twinReceivingLink;
        }

        async Task<ReceivingAmqpLink> CreateTwinReceivingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = string.Format(CultureInfo.InvariantCulture, CommonConstants.DeviceTwinPathTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));

            ReceivingAmqpLink twinReceivingLink = await this.IotHubConnection.CreateReceivingLinkAsync(path, this.iotHubConnectionString, this.twinConnectionCorrelationId, IotHubConnection.ReceivingLinkType.Twin, this.prefetchCount, timeout, this.productInfo, cancellationToken).ConfigureAwait(false);

            MyStringCopy(twinReceivingLink.Name, out twinReceivingLinkName);
            this.SafeAddClosedTwinReceivingLinkHandler = this.linkClosedListener;
            twinReceivingLink.SafeAddClosed(async (o, ea) =>
                await Task.Run(async () =>
                    {
                        await this.SafeAddClosedTwinReceivingLinkHandler(
                            o,
                            new ConnectionEventArgs
                            {
                                ConnectionType = ConnectionType.AmqpTwinReceiving,
                                ConnectionStatus = ConnectionStatus.Disconnected_Retrying,
                                ConnectionStatusChangeReason = ConnectionStatusChangeReason.No_Network
                            }).ConfigureAwait(false);
                    }
            ).ConfigureAwait(false));

            twinReceivingLink.RegisterMessageListener(message => this.HandleTwinMessage(message, twinReceivingLink));

            return twinReceivingLink;
        }

        private void MyStringCopy(String source, out String destination)
        {
            char[] chars = new Char[source.Length];
            source.CopyTo(0, chars, 0, source.Length);
            destination = new String(chars);
        }
    }
}
