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
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Shared;
    using System.Collections.Concurrent;
    using Newtonsoft.Json;
    using System.Diagnostics;
    using Microsoft.Azure.Devices.Client.Exceptions;

    sealed class AmqpTransportHandler : TransportHandler
    {
        #region Members-Constructor
        const string InputNameKey = "x-opt-input-name";
        public const string ResponseStatusName = "status";

        readonly IotHubConnectionString iotHubConnectionString;
        readonly TimeSpan openTimeout;
        readonly TimeSpan operationTimeout;
        readonly uint prefetchCount;

        Func<MethodRequestInternal, Task> methodReceivedListener;
        Func<string, Message, Task> eventReceivedListener;
        Action<TwinCollection> onDesiredStatePatchListener;

        string methodConnectionCorrelationId = CommonResources.GetNewStringGuid("");
        string twinConnectionCorrelationId = CommonResources.GetNewStringGuid("");

        const int ResponseTimeoutInSeconds = 300;

        ConcurrentDictionary<string, TaskCompletionSource<AmqpMessage>> twinResponseCompletions = new ConcurrentDictionary<string, TaskCompletionSource<AmqpMessage>>();

        ProductInfo productInfo;

        private DeviceClientEndpointIdentity deviceClientEndpointIdentity;
        private AmqpClientConnection amqpClientConnection;
        private AmqpClientConnectionManager amqpClientConnectionManager;

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

            this.openTimeout = transportSettings.OpenTimeout;
            this.operationTimeout = transportSettings.OperationTimeout;
            this.prefetchCount = transportSettings.PrefetchCount;
            this.iotHubConnectionString = connectionString;
            this.methodReceivedListener = onMethodCallback;
            this.onDesiredStatePatchListener = onDesiredStatePatchReceived;
            this.eventReceivedListener = onEventReceivedCallback;

            // Get AmqpClientConnectionCache instance
            this.amqpClientConnectionManager = AmqpClientConnectionManager.Instance;

            // Get connection from AmqpClientConnectionCache
            DeviceClientEndpointIdentityFactory deviceClientEndpointIdentityFactory = new DeviceClientEndpointIdentityFactory();
            this.deviceClientEndpointIdentity = deviceClientEndpointIdentityFactory.Create(iotHubConnectionString, transportSettings, productInfo);

            this.amqpClientConnection = amqpClientConnectionManager.GetClientConnection(deviceClientEndpointIdentity);

            this.amqpClientConnection.OnAmqpClientConnectionClosed += OnAmqpConnectionClose;
        }
        #endregion

        #region Open-Close
        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(OpenAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await amqpClientConnection.OpenAsync(this.openTimeout).ConfigureAwait(false);
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
        
        private void OnAmqpConnectionClose(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{sender}", nameof(OnAmqpConnectionClose));

            amqpClientConnectionManager.RemoveClientConnection(deviceClientEndpointIdentity);
            _transportShouldRetry.TrySetResult(true);

            if (Logging.IsEnabled) Logging.Exit(this, $"{sender}", nameof(OnAmqpConnectionClose));
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, "", $"{nameof(AmqpTransportHandler)}.{nameof(CloseAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _transportShouldRetry.TrySetCanceled();

                await amqpClientConnection.CloseAsync(this.openTimeout).ConfigureAwait(false);

                amqpClientConnectionManager.RemoveClientConnection(deviceClientEndpointIdentity);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, "", $"{nameof(AmqpTransportHandler)}.{nameof(CloseAsync)}");
            }
        }
        #endregion

        #region Telemetry
        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendEventAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                Outcome outcome = await amqpClientConnection.SendTelemetrMessageAsync(message.ToAmqpMessage(), this.operationTimeout).ConfigureAwait(false);

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
            if (Logging.IsEnabled) Logging.Enter(this, messages, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendEventAsync)}");

            try
            {
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
                    outcome = await amqpClientConnection.SendTelemetrMessageAsync(amqpMessage, this.operationTimeout).ConfigureAwait(false);
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
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(ReceiveAsync)}");

            Message message;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                message = await amqpClientConnection.ReceiveAsync(operationTimeout).ConfigureAwait(false);

            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(ReceiveAsync)}");
            }

            return message;
        }
        #endregion

        #region Methods
        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(EnableMethodsAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (String.IsNullOrEmpty(methodConnectionCorrelationId))
                {
                    methodConnectionCorrelationId = CommonResources.GetNewStringGuid("");
                }

                await amqpClientConnection.EnableMethodsAsync(methodConnectionCorrelationId, methodReceivedListener, this.openTimeout).ConfigureAwait(false);

                if (methodReceivedListener != null)
                {
                    methodConnectionCorrelationId = CommonResources.GetNewStringGuid("");
                }
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(EnableMethodsAsync)}");
            }
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(DisableMethodsAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                await amqpClientConnection.DisableMethodsAsync(this.openTimeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(DisableMethodsAsync)}");
            }
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, methodResponse, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(SendMethodResponseAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                Outcome outcome;
                using (AmqpMessage amqpMessage = methodResponse.ToAmqpMessage())
                {
                    outcome = await amqpClientConnection.SendMethodResponseAsync(amqpMessage, this.operationTimeout).ConfigureAwait(false);
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
        #endregion

        #region Twin
        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(EnableTwinPatchAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (String.IsNullOrEmpty(twinConnectionCorrelationId))
                {
                    twinConnectionCorrelationId = CommonResources.GetNewStringGuid("");
                }

                await amqpClientConnection.EnableTwinPatchAsync(twinConnectionCorrelationId, onTwinPatchReceived, this.openTimeout).ConfigureAwait(false);

                if (onDesiredStatePatchListener != null)
                {
                    twinConnectionCorrelationId = CommonResources.GetNewStringGuid("");
                }
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(EnableTwinPatchAsync)}");
            }
        }

        private void onTwinPatchReceived(AmqpMessage message)
        {
            amqpClientConnection.DisposeTwinPatchDelivery(message);

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

        public async Task DisableTwinAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(DisableTwinAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                await amqpClientConnection.DisableTwinAsync(this.openTimeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(DisableTwinAsync)}");
            }
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

        private async Task<AmqpMessage> RoundTripTwinMessage(AmqpMessage amqpMessage, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(RoundTripTwinMessage)}");
            string correlationId = Guid.NewGuid().ToString();
            AmqpMessage response = null;

            try
            {
                Outcome outcome;
                //SendingAmqpLink eventSendingLink = await this.GetTwinSendingLinkAsync(cancellationToken).ConfigureAwait(false);

                amqpMessage.Properties.CorrelationId = correlationId;

                var taskCompletionSource = new TaskCompletionSource<AmqpMessage>();
                this.twinResponseCompletions[correlationId] = taskCompletionSource;

                outcome = await amqpClientConnection.SendTwinMessageAsync(amqpMessage, this.operationTimeout).ConfigureAwait(false);

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
        #endregion

        #region Events
        public override async Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(EnableEventReceiveAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await amqpClientConnection.EnableEventsReceiveAsync(onEventsReceived, this.openTimeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(EnableEventReceiveAsync)}");
            }
        }
        internal void onEventsReceived(AmqpMessage message)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Accept-Dispose
        public override Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(CompleteAsync)}");

            try
            {
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
            if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(AbandonAsync)}");

            try
            {
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
            if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(RejectAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return this.DisposeMessageAsync(lockToken, AmqpConstants.RejectedOutcome, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, $"{nameof(AmqpTransportHandler)}.{nameof(RejectAsync)}");
            }
        }

        private async Task DisposeMessageAsync(string lockToken, Outcome outcome, CancellationToken cancellationToken)
        {
            Outcome disposeOutcome;
            try
            {
                // Currently, the same mechanism is used for sending feedback for C2D messages and events received by modules.
                // However, devices only support C2D messages (they cannot receive events), and modules only support receiving events
                // (they cannot receive C2D messages). So we use this to distinguish whether to dispose the message (i.e. send outcome on)
                // the DeviceBoundReceivingLink or the EventsReceivingLink. 
                // If this changes (i.e. modules are able to receive C2D messages, or devices are able to receive telemetry), this logic 
                // will have to be updated.
                disposeOutcome = await amqpClientConnection.DisposeMessageAsync(lockToken, outcome, this.operationTimeout).ConfigureAwait(false);
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
                        Error error = new Error
                        {
                            Condition = IotHubAmqpErrorCode.MessageLockLostError
                        };
                        throw AmqpErrorMapper.ToIotHubClientContract(error);
                    }
                }

                throw AmqpErrorMapper.GetExceptionFromOutcome(disposeOutcome);
            }
        }
        #endregion

        #region Helpers
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

        private async void ProcessReceivedEventMessage(AmqpMessage amqpMessage)
        {
            Message message = new Message(amqpMessage)
            {
                LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
            };
            await this.eventReceivedListener(message.InputName, message).ConfigureAwait(false);
        }
        #endregion
    }
}
