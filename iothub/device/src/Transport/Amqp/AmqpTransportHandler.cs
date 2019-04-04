// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpTransportHandler : TransportHandler
    {
        #region Members-Constructor
        private const string ResponseStatusName = "status";
        private const int ResponseTimeoutInSeconds = 300;
        private readonly TimeSpan _operationTimeout;
        private readonly AmqpUnit _amqpUnit;
        private readonly Action<TwinCollection> _desiredPropertyListener;
        private ConcurrentDictionary<string, TaskCompletionSource<AmqpMessage>> _twinResponseCompletions = new ConcurrentDictionary<string, TaskCompletionSource<AmqpMessage>>();

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
            Func<MethodRequestInternal, Task> methodHandler = null,
            Action<TwinCollection> desiredPropertyListener = null,
            Func<string, Message, Task> eventListener = null)
            : base(context, transportSettings)
        {
            _operationTimeout = transportSettings.OperationTimeout;
            _desiredPropertyListener = desiredPropertyListener;
            DeviceIdentity deviceIdentity = new DeviceIdentity(connectionString, transportSettings, context.Get<ProductInfo>());
            _amqpUnit = AmqpUnitManager.GetInstance().CreateAmqpUnit(
                deviceIdentity,
                methodHandler,
                TwinMessageListener, 
                eventListener
            );

            _amqpUnit.OnUnitDisconnected += (o, args) =>
            {
                bool gracefulDisconnect = (bool)o;
                if (gracefulDisconnect)
                {
                    OnTransportClosedGracefully();
                }
                else
                {
                    OnTransportDisconnected();
                }
            };

            if (Logging.IsEnabled) Logging.Associate(this, _amqpUnit, $"{nameof(_amqpUnit)}");
        }

        #endregion

        public override bool IsUsable => _amqpUnit.IsUsable();

        #region Open-Close
        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(OpenAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.OpenAsync(_operationTimeout).ConfigureAwait(false);
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
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(OpenAsync)}");
            }
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(CloseAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _amqpUnit.CloseAsync(_operationTimeout).ConfigureAwait(false);
                OnTransportClosedGracefully();
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(CloseAsync)}");
            }
        }
        #endregion

        #region Telemetry
        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, cancellationToken, $"{nameof(SendEventAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                Outcome outcome = await _amqpUnit.SendEventAsync(message.ToAmqpMessage(), _operationTimeout).ConfigureAwait(false);

                if (outcome != null)
                {
                    if (outcome.DescriptorCode != Accepted.Code)
                    {
                        throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                    }
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, message, cancellationToken, $"{nameof(SendEventAsync)}");
            }
        }

        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, messages, cancellationToken, $"{nameof(SendEventAsync)}");

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
                    outcome = await _amqpUnit.SendEventAsync(amqpMessage, _operationTimeout).ConfigureAwait(false);
                }

                if (outcome != null)
                {
                    if (outcome.DescriptorCode != Accepted.Code)
                    {
                        throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                    }
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, messages, cancellationToken, $"{nameof(SendEventAsync)}");
            }
        }

        public override async Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, cancellationToken, $"{nameof(ReceiveAsync)}");
            Message message = null;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    message = await _amqpUnit.ReceiveMessageAsync(timeout).ConfigureAwait(false);
                    if (message != null)
                    {
                        break;
                    }
                }
                catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
                {
                    throw AmqpClientHelper.ToIotHubClientContract(exception);
                }
            }
            if (Logging.IsEnabled) Logging.Exit(this, timeout, cancellationToken, $"{nameof(ReceiveAsync)}");
            return message;
        }
        #endregion

        #region Methods
        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(EnableMethodsAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.EnableMethodsAsync(_operationTimeout).ConfigureAwait(false);

            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(EnableMethodsAsync)}");
            }
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(DisableMethodsAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.DisableMethodsAsync(_operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(DisableMethodsAsync)}");
            }
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, methodResponse, cancellationToken, $"{nameof(SendMethodResponseAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                Outcome outcome;
                using (AmqpMessage amqpMessage = methodResponse.ToAmqpMessage())
                {
                    outcome = await _amqpUnit.SendMethodResponseAsync(amqpMessage, _operationTimeout).ConfigureAwait(false);
                }
                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, methodResponse, cancellationToken, $"{nameof(SendMethodResponseAsync)}");
            }
        }
        #endregion

        #region Twin
        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(EnableTwinPatchAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.EnsureTwinLinksAreOpenedAsync(_operationTimeout).ConfigureAwait(false);

            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                throw AmqpClientHelper.ToIotHubClientContract(exception);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(EnableTwinPatchAsync)}");
            }
        }

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(SendTwinGetAsync)}");
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
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(SendTwinGetAsync)}");
            }
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, reportedProperties, cancellationToken, $"{nameof(SendTwinPatchAsync)}");
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
                if (Logging.IsEnabled) Logging.Exit(this, reportedProperties, cancellationToken, $"{nameof(SendTwinPatchAsync)}");
            }
        }

        private async Task<AmqpMessage> RoundTripTwinMessage(AmqpMessage amqpMessage, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, cancellationToken, $"{nameof(RoundTripTwinMessage)}");
            string correlationId = Guid.NewGuid().ToString();
            AmqpMessage response = null;

            try
            {
                Outcome outcome;
                //SendingAmqpLink eventSendingLink = await this.GetTwinSendingLinkAsync(cancellationToken).ConfigureAwait(false);

                amqpMessage.Properties.CorrelationId = correlationId;

                var taskCompletionSource = new TaskCompletionSource<AmqpMessage>();
                _twinResponseCompletions[correlationId] = taskCompletionSource;

                outcome = await _amqpUnit.SendTwinMessageAsync(amqpMessage, _operationTimeout).ConfigureAwait(false);

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
                _twinResponseCompletions.TryRemove(correlationId, out _);
                if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, cancellationToken, $"{nameof(RoundTripTwinMessage)}");
            }

            return response;
        }
        #endregion

        #region Events
        public override async Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(EnableEventReceiveAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.EnableEventReceiveAsync(_operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(EnableEventReceiveAsync)}");
            }
        }
        
        #endregion

        #region Accept-Dispose
        public override Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, $"{nameof(CompleteAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return DisposeMessageAsync(lockToken, AmqpConstants.AcceptedOutcome, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, $"{nameof(CompleteAsync)}");
            }
        }

        public override Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, $"{nameof(AbandonAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return DisposeMessageAsync(lockToken, AmqpConstants.ReleasedOutcome, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, $"{nameof(AbandonAsync)}");
            }
        }

        public override Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, $"{nameof(RejectAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return DisposeMessageAsync(lockToken, AmqpConstants.RejectedOutcome, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, $"{nameof(RejectAsync)}");
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
                disposeOutcome = await _amqpUnit.DisposeMessageAsync(lockToken, outcome, _operationTimeout).ConfigureAwait(false);
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
                if (response.MessageAnnotations.Map.TryGetValue(ResponseStatusName, out int status))
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

        private void TwinMessageListener(AmqpMessage message)
        {
            string correlationId = message.Properties?.CorrelationId?.ToString();
            if (correlationId != null)
            {
                // If we have a correlation id, it must be a response, complete the task.
                TaskCompletionSource<AmqpMessage> task;
                if (_twinResponseCompletions.TryRemove(correlationId, out task))
                {
                    task.SetResult(message);
                }
            }
            else
            {
                // No correlation id? Must be a patch.
                if (_desiredPropertyListener != null)
                {
                    using (StreamReader reader = new StreamReader(message.BodyStream, System.Text.Encoding.UTF8))
                    {
                        string patch = reader.ReadToEnd();
                        var props = JsonConvert.DeserializeObject<TwinCollection>(patch);
                        _desiredPropertyListener(props);
                    }
                }
            }

        }

        private Twin TwinFromResponse(AmqpMessage message)
        {
            using (StreamReader reader = new StreamReader(message.BodyStream, System.Text.Encoding.UTF8))
            {
                string body = reader.ReadToEnd();
                var props = JsonConvert.DeserializeObject<TwinProperties>(body);
                return new Twin(props);
            }
        }

        #endregion

        #region IDispose
        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            base.Dispose(disposing);
            if (disposing)
            {
                _amqpUnit?.Dispose();
            }

        }
        #endregion

    }
}
