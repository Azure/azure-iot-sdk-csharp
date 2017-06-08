// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Amqp.Framing;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Shared;
    using System.IO;
    using System.Collections.Concurrent;
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

        Func<MethodRequestInternal, Task> messageListener;
        Action<TwinCollection> onDesiredStatePatchListener;
        Action<Message> twinResponseEvent;
        
        Action<object, EventArgs> linkClosedListener;
        Action<object, EventArgs> SafeAddClosedMethodReceivingLinkHandler;
        Action<object, EventArgs> SafeAddClosedMethodSendingLinkHandler;
        Action<object, EventArgs> SafeAddClosedTwinReceivingLinkHandler;
        Action<object, EventArgs> SafeAddClosedTwinSendingLinkHandler;
        internal delegate void OnConnectionClosedDelegate(object sender, EventArgs e);

        int closed;
        ConcurrentDictionary<string, TaskCompletionSource<AmqpMessage>> twinResponseCompletions = new ConcurrentDictionary<string, TaskCompletionSource<AmqpMessage>>();

        internal AmqpTransportHandler(
            IPipelineContext context, IotHubConnectionString connectionString, 
            AmqpTransportSettings transportSettings,
            Action<object, EventArgs> onLinkClosedCallback,
            Func<MethodRequestInternal, Task> onMethodCallback = null,
            Action<TwinCollection> onDesiredStatePatchReceived = null)
            :base(context, transportSettings)
        {
            this.linkClosedListener = onLinkClosedCallback;

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
                         this.faultTolerantDeviceBoundReceivingLink.OpenAsync(this.openTimeout, cancellationToken));
                 }
                 catch (Exception exception)
                 {
                     if (exception.IsFatal())
                     {
                         throw;
                     }

                     throw AmqpClientHelper.ToIotHubClientContract(exception);
                 }
             }, cancellationToken);
        }

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            await this.HandleTimeoutCancellation(async () =>
            {
                Outcome outcome;
                using (AmqpMessage amqpMessage = message.ToAmqpMessage())
                {
                    outcome = await this.SendAmqpMessageAsync(amqpMessage, cancellationToken);
                }

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }, cancellationToken);
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
                    outcome = await this.SendAmqpMessageAsync(amqpMessage, cancellationToken);
                }

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }, cancellationToken);
        }

        public override async Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Message message = null;

            await this.HandleTimeoutCancellation(async () =>
            {
                AmqpMessage amqpMessage;
                try
                {
                    ReceivingAmqpLink deviceBoundReceivingLink = await this.GetDeviceBoundReceivingLinkAsync(cancellationToken);
                    amqpMessage = await deviceBoundReceivingLink.ReceiveMessageAsync(timeout);
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
            }, cancellationToken);

            return message;
        }

        public override Task RecoverConnections(object link, CancellationToken cancellationToken)
        {
#if WIP_C2D_METHODS_AMQP
            Func<Task> enableMethodLinkAsyncFunc = null;

            var amqpLink = link as AmqpLink;
            if (amqpLink == null)
            {
                return Common.TaskConstants.Completed;
            }

            if (amqpLink.IsReceiver)
            {
                this.faultTolerantMethodReceivingLink = new Client.FaultTolerantAmqpObject<ReceivingAmqpLink>(this.CreateMethodReceivingLinkAsync, this.IotHubConnection.CloseLink);
                enableMethodLinkAsyncFunc = async () => await EnableMethodReceivingLinkAsync(cancellationToken);
            }
            else
            {
                this.faultTolerantMethodSendingLink = new Client.FaultTolerantAmqpObject<SendingAmqpLink>(this.CreateMethodSendingLinkAsync, this.IotHubConnection.CloseLink);
                enableMethodLinkAsyncFunc = async () => await EnableMethodSendingLinkAsync(cancellationToken);
            }

            return this.HandleTimeoutCancellation(async () =>
            {
                try
                {
                    if (this.messageListener != null)
                    {
                        await enableMethodLinkAsyncFunc();
                    }
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    throw AmqpClientHelper.ToIotHubClientContract(ex);
                }
            }, cancellationToken);
#else
            throw new NotImplementedException();
#endif
        }

        public override Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
#if WIP_C2D_METHODS_AMQP
            if (this.faultTolerantMethodSendingLink == null)
            {
                this.faultTolerantMethodSendingLink = new Client.FaultTolerantAmqpObject<SendingAmqpLink>(this.CreateMethodSendingLinkAsync, this.IotHubConnection.CloseLink);
            }

            if (this.faultTolerantMethodReceivingLink == null)
            {
                this.faultTolerantMethodReceivingLink = new Client.FaultTolerantAmqpObject<ReceivingAmqpLink>(this.CreateMethodReceivingLinkAsync, this.IotHubConnection.CloseLink);
            }

            return this.HandleTimeoutCancellation(async () =>
            {
                try
                {
                    if (this.messageListener != null)
                    {
                        await Task.WhenAll(EnableMethodSendingLinkAsync(cancellationToken), EnableMethodReceivingLinkAsync(cancellationToken));
                    }
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    throw AmqpClientHelper.ToIotHubClientContract(ex);
                }
            }, cancellationToken);
#else
            throw new NotImplementedException();
#endif
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
#if WIP_C2D_METHODS_AMQP
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
                        await Task.WhenAll(EnableTwinSendingLinkAsync(cancellationToken), EnableTwinReceivingLinkAsync(cancellationToken));
                    }
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    throw AmqpClientHelper.ToIotHubClientContract(ex);
                }
            }, cancellationToken);
#else
            throw new NotImplementedException();
#endif
        }


#if WIP_C2D_METHODS_AMQP
        private async Task EnableMethodSendingLinkAsync(CancellationToken cancellationToken)
        {
            SendingAmqpLink methodSendingLink = await this.GetMethodSendingLinkAsync(cancellationToken);
            this.SafeAddClosedMethodSendingLinkHandler = this.linkClosedListener;
            methodSendingLink.SafeAddClosed((o, ea) => this.SafeAddClosedMethodSendingLinkHandler(o, ea));
        }

        private async Task EnableMethodReceivingLinkAsync(CancellationToken cancellationToken)
        {
            ReceivingAmqpLink methodReceivingLink = await this.GetMethodReceivingLinkAsync(cancellationToken);
            this.SafeAddClosedMethodReceivingLinkHandler = this.linkClosedListener;
            methodReceivingLink.SafeAddClosed((o, ea) => this.SafeAddClosedMethodReceivingLinkHandler(o, ea));
        }
        
        private async Task EnableTwinSendingLinkAsync(CancellationToken cancellationToken)
        {
            SendingAmqpLink twinSendingLink = await this.GetTwinSendingLinkAsync(cancellationToken);
            this.SafeAddClosedTwinSendingLinkHandler = this.linkClosedListener;
            twinSendingLink.SafeAddClosed((o, ea) => this.SafeAddClosedTwinSendingLinkHandler(o, ea));
        }

        private async Task EnableTwinReceivingLinkAsync(CancellationToken cancellationToken)
        {
            ReceivingAmqpLink twinReceivingLink = await this.GetTwinReceivingLinkAsync(cancellationToken);
            this.SafeAddClosedTwinReceivingLinkHandler = this.linkClosedListener;
            twinReceivingLink.SafeAddClosed((o, ea) => this.SafeAddClosedTwinReceivingLinkHandler(o, ea));
        }
#endif

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
#if WIP_C2D_METHODS_AMQP
            Task receivingLinkCloseTask;

            this.SafeAddClosedMethodSendingLinkHandler = (o, ea) => {};
            this.SafeAddClosedMethodReceivingLinkHandler = (o, ea) => {};

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

            await Task.WhenAll(receivingLinkCloseTask, sendingLinkCloseTask);
#else
            throw new NotImplementedException();
#endif
        }

        public async Task DisableTwinAsync(CancellationToken cancellationToken)
        {
#if WIP_C2D_METHODS_AMQP
            Task receivingLinkCloseTask;

            this.SafeAddClosedTwinSendingLinkHandler = (o, ea) => {};
            this.SafeAddClosedTwinReceivingLinkHandler = (o, ea) => {};

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

            await Task.WhenAll(receivingLinkCloseTask, sendingLinkCloseTask);
#else
            throw new NotImplementedException();
#endif
        }
        
        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            await this.HandleTimeoutCancellation(async () =>
            {
                Outcome outcome;
                using (AmqpMessage amqpMessage = methodResponse.ToAmqpMessage())
                {
                    outcome = await this.SendAmqpMethodResponseAsync(amqpMessage, cancellationToken);
                }

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }, cancellationToken);
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
                await this.CloseAsync();
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
            if (Interlocked.CompareExchange(ref this.closed, 1, 0) == 0)
            {
                GC.SuppressFinalize(this);
                Task eventSendingLinkCloseTask = this.faultTolerantEventSendingLink.CloseAsync();
                Task deviceBoundReceivingLinkCloseTask = this.faultTolerantDeviceBoundReceivingLink.CloseAsync();
#if WIP_C2D_METHODS_AMQP
                Task disabledMethodTask = this.DisableMethodsAsync(CancellationToken.None);
                Task disableTwinTask = this.DisableTwinAsync(CancellationToken.None);
                await Task.WhenAll(eventSendingLinkCloseTask, deviceBoundReceivingLinkCloseTask, disabledMethodTask, disableTwinTask);
#else
                await Task.WhenAll(eventSendingLinkCloseTask, deviceBoundReceivingLinkCloseTask);
#endif
                this.IotHubConnection.Release(this.deviceId);
            }
        }

        async Task<Outcome> SendAmqpMessageAsync(AmqpMessage amqpMessage, CancellationToken cancellationToken)
        {
            Outcome outcome;
            try
            {
                SendingAmqpLink eventSendingLink = await this.GetEventSendingLinkAsync(cancellationToken);
                outcome = await eventSendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), AmqpConstants.NullBinary, this.operationTimeout);
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
                SendingAmqpLink methodRespSendingLink = await this.GetMethodSendingLinkAsync(cancellationToken);
                outcome = await methodRespSendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), AmqpConstants.NullBinary, this.operationTimeout);
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
        
        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            Outcome outcome;
            string correlationId = Guid.NewGuid().ToString();
            
            try
            {
                await EnableTwinPatchAsync(cancellationToken);
                
                SendingAmqpLink eventSendingLink = await this.GetTwinSendingLinkAsync(cancellationToken);

                AmqpMessage amqpMessage = AmqpMessage.Create();
                amqpMessage.Properties.CorrelationId = correlationId;
                amqpMessage.MessageAnnotations.Map["operation"] = "GET";

                this.twinResponseCompletions[correlationId] = new TaskCompletionSource<AmqpMessage>();
                
                outcome = await eventSendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), AmqpConstants.NullBinary, this.operationTimeout);
                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }

                var response = await this.twinResponseCompletions[correlationId].Task;

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
            finally
            {
                TaskCompletionSource<AmqpMessage> throwAway;
                this.twinResponseCompletions.TryRemove(correlationId, out throwAway);
            }
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {

            Outcome outcome;
            string correlationId = Guid.NewGuid().ToString();

            try
            {
                await EnableTwinPatchAsync(cancellationToken);

                var body = JsonConvert.SerializeObject(reportedProperties);
                var bodyStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));

                // TODO: disposal?
                var amqpMessage = AmqpMessage.Create(bodyStream, false);
                amqpMessage.Properties.CorrelationId = correlationId;
                amqpMessage.MessageAnnotations.Map["operation"] = "PATCH";
                amqpMessage.MessageAnnotations.Map["resource"] = "/properties/reported";
                amqpMessage.MessageAnnotations.Map["version"] = null;

                SendingAmqpLink eventSendingLink = await this.GetTwinSendingLinkAsync(cancellationToken);

                this.twinResponseCompletions[correlationId] = new TaskCompletionSource<AmqpMessage>();

                outcome = await eventSendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), AmqpConstants.NullBinary, this.operationTimeout);
                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }

                await this.twinResponseCompletions[correlationId].Task;
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                {
                    throw;
                }

                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                TaskCompletionSource<AmqpMessage> throwAway;
                this.twinResponseCompletions.TryRemove(correlationId, out throwAway);
            }
        }

        private void HandleTwinMessage(AmqpMessage message, ReceivingAmqpLink link)
        {
            link.DisposeDelivery(message, true, AmqpConstants.AcceptedOutcome);

            string correlationId = message.Properties.CorrelationId?.ToString();
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
                ReceivingAmqpLink deviceBoundReceivingLink = await this.GetDeviceBoundReceivingLinkAsync(cancellationToken);
                disposeOutcome = await deviceBoundReceivingLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: this.operationTimeout);
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
                eventSendingLink = await this.faultTolerantEventSendingLink.GetOrCreateAsync(this.openTimeout, cancellationToken);
            }
            return eventSendingLink;
        }

        async Task<SendingAmqpLink> CreateEventSendingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = string.Format(CultureInfo.InvariantCulture, CommonConstants.DeviceEventPathTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));

            return await this.IotHubConnection.CreateSendingLinkAsync(path, this.iotHubConnectionString, this.deviceId, IotHubConnection.SendingLinkType.Telemetry, timeout, cancellationToken);
        }

        async Task<ReceivingAmqpLink> GetDeviceBoundReceivingLinkAsync(CancellationToken cancellationToken)
        {
            ReceivingAmqpLink deviceBoundReceivingLink;
            if (!this.faultTolerantDeviceBoundReceivingLink.TryGetOpenedObject(out deviceBoundReceivingLink))
            {
                deviceBoundReceivingLink = await this.faultTolerantDeviceBoundReceivingLink.GetOrCreateAsync(this.openTimeout, cancellationToken);
            }

            return deviceBoundReceivingLink;
        }

        async Task<ReceivingAmqpLink> CreateDeviceBoundReceivingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = string.Format(CultureInfo.InvariantCulture, CommonConstants.DeviceBoundPathTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));

            return await this.IotHubConnection.CreateReceivingLinkAsync(path, this.iotHubConnectionString, this.deviceId, IotHubConnection.ReceivingLinkType.Messaging, this.prefetchCount, timeout, cancellationToken);
        }

        async Task<SendingAmqpLink> GetMethodSendingLinkAsync(CancellationToken cancellationToken)
        {
            SendingAmqpLink methodSendingLink;
            if (!this.faultTolerantMethodSendingLink.TryGetOpenedObject(out methodSendingLink))
            {
                methodSendingLink = await this.faultTolerantMethodSendingLink.GetOrCreateAsync(this.openTimeout, cancellationToken);
            }
            return methodSendingLink;
        }

        async Task<SendingAmqpLink> CreateMethodSendingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = string.Format(CultureInfo.InvariantCulture, CommonConstants.DeviceMethodPathTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));
            return await this.IotHubConnection.CreateSendingLinkAsync(path, this.iotHubConnectionString, this.deviceId, IotHubConnection.SendingLinkType.Methods, timeout, cancellationToken);
        }

        async Task<ReceivingAmqpLink> GetMethodReceivingLinkAsync(CancellationToken cancellationToken)
        {
            ReceivingAmqpLink methodReceivingLink;
            if (!this.faultTolerantMethodReceivingLink.TryGetOpenedObject(out methodReceivingLink))
            {
                methodReceivingLink = await this.faultTolerantMethodReceivingLink.GetOrCreateAsync(this.openTimeout, cancellationToken);
            }

            return methodReceivingLink;
        }

        async Task<ReceivingAmqpLink> CreateMethodReceivingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = string.Format(CultureInfo.InvariantCulture, CommonConstants.DeviceMethodPathTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));

            var link = await this.IotHubConnection.CreateReceivingLinkAsync(path, this.iotHubConnectionString, this.deviceId, IotHubConnection.ReceivingLinkType.Methods, this.prefetchCount, timeout, cancellationToken);

            link.RegisterMessageListener(amqpMessage => 
                {
                    MethodRequestInternal methodRequestInternal = MethodConverter.ConstructMethodRequestFromAmqpMessage(amqpMessage);
                    link.DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
                    this.messageListener(methodRequestInternal);
                });

            return link;
        }

        async Task<SendingAmqpLink> GetTwinSendingLinkAsync(CancellationToken cancellationToken)
        {
            SendingAmqpLink twinSendingLink;
            if (!this.faultTolerantTwinSendingLink.TryGetOpenedObject(out twinSendingLink))
            {
                twinSendingLink = await this.faultTolerantTwinSendingLink.GetOrCreateAsync(this.openTimeout, cancellationToken);
            }
            return twinSendingLink;
        }

        async Task<SendingAmqpLink> CreateTwinSendingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = string.Format(CultureInfo.InvariantCulture, CommonConstants.DeviceMethodPathTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));

            return await this.IotHubConnection.CreateSendingLinkAsync(path, this.iotHubConnectionString, this.deviceId, IotHubConnection.SendingLinkType.Twin, timeout, cancellationToken);
        }

        async Task<ReceivingAmqpLink> GetTwinReceivingLinkAsync(CancellationToken cancellationToken)
        {
            ReceivingAmqpLink twinReceivingLink;
            if (!this.faultTolerantTwinReceivingLink.TryGetOpenedObject(out twinReceivingLink))
            {
                twinReceivingLink = await this.faultTolerantTwinReceivingLink.GetOrCreateAsync(this.openTimeout, cancellationToken);
            }

            return twinReceivingLink;
        }

        async Task<ReceivingAmqpLink> CreateTwinReceivingLinkAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            string path = string.Format(CultureInfo.InvariantCulture, CommonConstants.DeviceMethodPathTemplate, System.Net.WebUtility.UrlEncode(this.deviceId));

            var link = await this.IotHubConnection.CreateReceivingLinkAsync(path, this.iotHubConnectionString, this.deviceId, IotHubConnection.ReceivingLinkType.Twin, this.prefetchCount, timeout, cancellationToken);

            link.RegisterMessageListener(message => this.HandleTwinMessage(message, link));

            return link;
        }
    
    }

}
