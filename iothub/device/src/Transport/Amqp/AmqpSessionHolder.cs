using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpSessionHolder : IAmqpSessionHolder, ILinkFactory
    {
        private const string MessageOutgoingLinkSuffix = "_TelemetrySenderLink";
        private const string MessageIncomingLinkSuffix = "_TelemetryReceiverLink";
        private const string EventIncomingLinkSuffix = "_EventsReceiverLink";
        private const string MethodOutgoingLinkSuffix = "_MethodsSenderLink";
        private const string MethodIncomingLinkSuffix = "_MethodsReceiverLink";
        private const string MethodCorrelationIdPrefix = "methods:";
        private const string TwinOutgoingLinkSuffix = "_TwinSenderLink";
        private const string TwinIncomingLinkSuffix = "_TwinReceiverLink";
        private const string TwinCorrelationIdPrefix = "twin:";
        private readonly DeviceIdentity DeviceIdentity;
        private readonly Action OnSessionClose;
        private readonly Func<MethodRequestInternal, Task> MethodHandler;
        private readonly Action<AmqpMessage> TwinMessageListener;
        private readonly Func<string, Message, Task> EventListener;
        private readonly Func<DeviceIdentity, ILinkFactory, TimeSpan, Task<AmqpSession>> AmqpSessionSupplier;
        private readonly Func<DeviceIdentity, TimeSpan, Task<AmqpAuthenticationRefresher>> AmqpAuthenticationRefresherSupplier;
        private readonly Action<DeviceIdentity> RemoveDevice;
        private readonly SemaphoreSlim Lock;
        private ReceivingAmqpLink MessageReceivingLink;
        private ReceivingAmqpLink MethodReceivingLink;
        private ReceivingAmqpLink TwinReceivingLink;
        private ReceivingAmqpLink EventReceivingLink;
        private SendingAmqpLink MessageSendingLink;
        private SendingAmqpLink MethodSendingLink;
        private SendingAmqpLink TwinSendingLink;
        private SendingAmqpLink EventSendingLink;
        private AmqpSession AmqpSession;
        private AmqpAuthenticationRefresher AmqpAuthenticationRefresher;
        private bool Closed;

        public AmqpSessionHolder(
            DeviceIdentity deviceIdentity,
            Action onSessionClose,
            Func<DeviceIdentity, ILinkFactory, TimeSpan, Task<AmqpSession>> amqpSessionSupplier,
            Func<DeviceIdentity, TimeSpan, Task<AmqpAuthenticationRefresher>> amqpAuthenticationRefresherSupplier,
            Action<DeviceIdentity> removeDevice, 
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<AmqpMessage> twinMessageListener, 
            Func<string, Message, Task> eventListener)
        {
            Lock = new SemaphoreSlim(1, 1);
            DeviceIdentity = deviceIdentity;
            OnSessionClose = onSessionClose;
            MethodHandler = methodHandler;
            TwinMessageListener = twinMessageListener;
            EventListener = eventListener;
            AmqpSessionSupplier = amqpSessionSupplier;
            AmqpAuthenticationRefresherSupplier = amqpAuthenticationRefresherSupplier;
            RemoveDevice = removeDevice;
            Closed = false;
            if (Logging.IsEnabled) Logging.Associate(this, DeviceIdentity, $"{nameof(DeviceIdentity)}");
        }
        
        #region Usability
        public bool IsUsable()
        {
            return !Closed;
        }
        #endregion

        #region Open-Close
        public async Task OpenAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(OpenAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            AmqpSession = await AmqpSessionSupplier(DeviceIdentity, this, timeoutHelper.RemainingTime()).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Associate(this, AmqpSession, $"{nameof(AmqpSession)}");
            AmqpSession.Closed += OnSessionDisconnected;
            try
            {
                if (DeviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
                {
                    AmqpAuthenticationRefresher = await AmqpAuthenticationRefresherSupplier(DeviceIdentity, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    if (Logging.IsEnabled) Logging.Associate(this, AmqpAuthenticationRefresher, $"{nameof(AmqpAuthenticationRefresher)}");
                }
                await OpenMessageLinksAsync(timeoutHelper).ConfigureAwait(false);
            }
            catch(Exception)
            {
                Closed = true;
                await AmqpSession.CloseAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                throw;
            }
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(OpenAsync)}");
        }

        public async Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(CloseAsync)}");         
            RemoveDevice(DeviceIdentity);
            try
            {
                await AmqpSession.CloseAsync(timeout).ConfigureAwait(false);
            }
            catch(Exception)
            {
                if (Logging.IsEnabled) Logging.Info(this, "Discard any exception.", $"{nameof(CloseAsync)}");
            }
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(CloseAsync)}");
        }
        #endregion

        #region Message
        private async Task OpenMessageLinksAsync(TimeoutHelper timeoutHelper)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeoutHelper, $"{nameof(OpenMessageLinksAsync)}");
            Task<SendingAmqpLink> sendingLinkCreator = OpenSendingAmqpLinkAsync(
                null,
                null,
                CommonConstants.DeviceEventPathTemplate,
                CommonConstants.ModuleEventPathTemplate,
                MessageOutgoingLinkSuffix,
                null,
                timeoutHelper
            );
            Task<ReceivingAmqpLink> receiveLinkCreator = OpenReceivingAmqpLinkAsync(
                null,
                (byte)ReceiverSettleMode.Second,
                CommonConstants.DeviceBoundPathTemplate,
                CommonConstants.ModuleBoundPathTemplate,
                MessageIncomingLinkSuffix,
                null,
                timeoutHelper
            );
            await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);

            MessageReceivingLink = receiveLinkCreator.Result;
            MessageSendingLink = sendingLinkCreator.Result;
            MessageReceivingLink.Closed += OnLinkDisconnected;
            MessageSendingLink.Closed += OnLinkDisconnected;
            if (Logging.IsEnabled) Logging.Exit(this, MessageReceivingLink, $"{nameof(MessageReceivingLink)}");
            if (Logging.IsEnabled) Logging.Exit(this, MessageSendingLink, $"{nameof(MessageSendingLink)}");
            if (Logging.IsEnabled) Logging.Exit(this, timeoutHelper, $"{nameof(OpenMessageLinksAsync)}");
        }

        public async Task<Outcome> SendMessageAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, timeout, $"{nameof(SendMessageAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            Outcome outcome = await SendAmqpMessageAsync(MessageSendingLink, message, timeoutHelper).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, message, timeout, $"{nameof(SendMessageAsync)}");
            return outcome;
        }
        public async Task<Message> ReceiveMessageAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(ReceiveMessageAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            Message message = null;
            AmqpMessage amqpMessage = await MessageReceivingLink.ReceiveMessageAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            if (amqpMessage != null)
            {
                message = new Message(amqpMessage)
                {
                    LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
                };
            }
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(ReceiveMessageAsync)}");
            return message;
        }

        public async Task<Outcome> DisposeMessageAsync(string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(DisposeMessageAsync)}");
            ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);
            Outcome disposeOutcome = await MessageReceivingLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this,  timeout, $"{nameof(DisposeMessageAsync)}");
            return disposeOutcome;
        }
        #endregion

        #region Event
        public async Task EnableEventReceiveAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableEventReceiveAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            if (EventReceivingLink == null)
            {
                try
                {
                    EventReceivingLink = await OpenReceivingAmqpLinkAsync(
                        null,
                        (byte) ReceiverSettleMode.First,
                        CommonConstants.DeviceEventPathTemplate,
                        CommonConstants.ModuleEventPathTemplate,
                        EventIncomingLinkSuffix,
                        null,
                        timeoutHelper
                    ).ConfigureAwait(false);
                    EventReceivingLink.RegisterMessageListener(OnEventsReceived);
                    EventReceivingLink.Closed += OnLinkDisconnected;
                    if (Logging.IsEnabled) Logging.Exit(this, EventReceivingLink, $"{nameof(EventReceivingLink)}");
                    if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnableEventReceiveAsync)}");
                }
                finally
                {
                    Lock.Release();
                }
            }
        }

        internal void OnEventsReceived(AmqpMessage message)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Method
        public async Task EnableMethodsAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableMethodsAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            ReceivingAmqpLink methodReceivingLink = null;
            SendingAmqpLink methodSendingLink = null;
            try
            {
                
                if (MethodReceivingLink == null)
                {
                    string correlationIdSuffix = Guid.NewGuid().ToString();
                    Task<ReceivingAmqpLink> receiveLinkCreator = OpenReceivingAmqpLinkAsync(
                        (byte) SenderSettleMode.Settled,
                        (byte) ReceiverSettleMode.First,
                        CommonConstants.DeviceMethodPathTemplate,
                        CommonConstants.ModuleMethodPathTemplate,
                        MethodIncomingLinkSuffix,
                        MethodCorrelationIdPrefix + correlationIdSuffix,
                        timeoutHelper
                    );
                    Task<SendingAmqpLink> sendingLinkCreator = OpenSendingAmqpLinkAsync(
                        (byte) SenderSettleMode.Settled,
                        (byte) ReceiverSettleMode.First,
                        CommonConstants.DeviceMethodPathTemplate,
                        CommonConstants.ModuleMethodPathTemplate,
                        MethodOutgoingLinkSuffix,
                        MethodCorrelationIdPrefix + correlationIdSuffix,
                        timeoutHelper
                    );
                    await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);

                    methodReceivingLink = receiveLinkCreator.Result;
                    methodSendingLink = sendingLinkCreator.Result;

                    MethodSendingLink = methodSendingLink;
                    MethodReceivingLink = methodReceivingLink;

                    MethodReceivingLink.RegisterMessageListener(OnMethodReceived);
                    MethodSendingLink.Closed += OnLinkDisconnected;
                    MethodReceivingLink.Closed += OnLinkDisconnected;

                    if (Logging.IsEnabled) Logging.Exit(this, MethodReceivingLink, $"{nameof(MethodReceivingLink)}");
                    if (Logging.IsEnabled) Logging.Exit(this, MethodSendingLink, $"{nameof(MethodSendingLink)}");
                }
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnableMethodsAsync)}");
            }
            catch(Exception)
            {
                methodReceivingLink?.Close();
                methodSendingLink?.Close();
                throw;
            }
            finally
            {
                Lock.Release();
            }
        }
        private void OnMethodReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, $"{nameof(OnMethodReceived)}");
            MethodRequestInternal methodRequestInternal = MethodConverter.ConstructMethodRequestFromAmqpMessage(amqpMessage, new CancellationToken(false));
            MethodReceivingLink?.DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
            MethodHandler?.Invoke(methodRequestInternal);
            if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, $"{nameof(OnMethodReceived)}");
        }

        public async Task DisableMethodsAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(DisableMethodsAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            ICollection<Task> tasks = new List<Task>();
            if (MethodReceivingLink != null)
            {
                tasks.Add(MethodReceivingLink.CloseAsync(timeoutHelper.RemainingTime()));
                MethodReceivingLink = null;

            }
            if (MethodSendingLink != null)
            {
                tasks.Add(MethodSendingLink.CloseAsync(timeoutHelper.RemainingTime()));
                MethodSendingLink = null;
            }
            Lock.Release();
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(DisableMethodsAsync)}");
        }

        public async Task<Outcome> SendMethodResponseAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, $"{nameof(SendMethodResponseAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            SendingAmqpLink methodSendingLink = MethodSendingLink;
            Lock.Release();
            if (methodSendingLink == null)
            {
                throw new IotHubCommunicationException();
            }
            else
            {
                Outcome outcome = await SendAmqpMessageAsync(methodSendingLink, message, timeoutHelper).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, message, $"{nameof(SendMethodResponseAsync)}");
                return outcome;
            }
        }
        #endregion

        #region Twin
        public async Task EnableTwinPatchAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableTwinPatchAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            string correlationIdSuffix = Guid.NewGuid().ToString();
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            ReceivingAmqpLink twinReceivingLink = null;
            SendingAmqpLink twinSendingLink = null;
            try
            {
                if (TwinReceivingLink == null)
                {
                    Task<ReceivingAmqpLink> receiveLinkCreator = OpenReceivingAmqpLinkAsync(
                        (byte) SenderSettleMode.Settled,
                        (byte) ReceiverSettleMode.First,
                        CommonConstants.DeviceTwinPathTemplate,
                        CommonConstants.ModuleTwinPathTemplate,
                        TwinIncomingLinkSuffix,
                        TwinCorrelationIdPrefix + correlationIdSuffix,
                        timeoutHelper
                    );
                    Task<SendingAmqpLink> sendingLinkCreator = OpenSendingAmqpLinkAsync(
                        (byte) SenderSettleMode.Settled,
                        (byte) ReceiverSettleMode.First,
                        CommonConstants.DeviceTwinPathTemplate,
                        CommonConstants.ModuleTwinPathTemplate,
                        TwinOutgoingLinkSuffix,
                        TwinCorrelationIdPrefix + correlationIdSuffix,
                        timeoutHelper
                    );
                    await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);
                    twinReceivingLink = receiveLinkCreator.Result;
                    twinSendingLink = sendingLinkCreator.Result;
                    TwinReceivingLink = twinReceivingLink;
                    TwinSendingLink = twinSendingLink;
                    TwinReceivingLink.RegisterMessageListener(OnDesiredPropertyReceived);
                    TwinReceivingLink.Closed += OnLinkDisconnected;
                    TwinSendingLink.Closed += OnLinkDisconnected;
                    if (Logging.IsEnabled) Logging.Exit(this, TwinReceivingLink, $"{nameof(TwinReceivingLink)}");
                    if (Logging.IsEnabled) Logging.Exit(this, TwinSendingLink, $"{nameof(TwinSendingLink)}");
                }
            }
            catch (Exception)
            {
                twinReceivingLink?.Close();
                twinSendingLink?.Close();
                throw;
            }
            finally
            {
                Lock.Release();
            }
            
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableTwinPatchAsync)}");
        }

        private void OnDesiredPropertyReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, $"{nameof(OnDesiredPropertyReceived)}");
            TwinReceivingLink?.DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
            TwinMessageListener?.Invoke(amqpMessage);
            if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, $"{nameof(OnDesiredPropertyReceived)}");
        }

        public async Task DisableTwinAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(DisableTwinAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            ICollection<Task> tasks = new List<Task>();
            if (TwinReceivingLink != null)
            {
                tasks.Add(TwinReceivingLink.CloseAsync(timeoutHelper.RemainingTime()));
                TwinReceivingLink = null;
                
            }
            if (TwinReceivingLink != null)
            {
                tasks.Add(TwinSendingLink.CloseAsync(timeoutHelper.RemainingTime()));
                TwinSendingLink = null;
            }
            Lock.Release();
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(DisableTwinAsync)}");
        }

        public async Task<Outcome> SendTwinMessageAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(SendTwinMessageAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            SendingAmqpLink twinSendingLink = TwinSendingLink;
            Lock.Release();
            Outcome outcome = await SendAmqpMessageAsync(twinSendingLink, message, timeoutHelper).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(SendTwinMessageAsync)}");
            return outcome;
        }
        #endregion

        #region Connectivity Event
        private void OnSessionDisconnected(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, o, $"{nameof(OnSessionDisconnected)}");
            Lock.Wait();
            bool wasOpen = !Closed && ReferenceEquals(o, AmqpSession);
            Closed = true;
            Lock.Release();
            if (wasOpen)
            {
                AmqpAuthenticationRefresher?.StopLoop();
                RemoveDevice(DeviceIdentity);
                OnSessionClose();
            }
            if (Logging.IsEnabled) Logging.Exit(this, o, $"{nameof(OnSessionDisconnected)}");
        }

        private void OnLinkDisconnected(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, o, $"{nameof(OnLinkDisconnected)}");
            Lock.Wait();
            bool wasOpen = !Closed && (ReferenceEquals(o, MessageReceivingLink)
                || ReferenceEquals(o, MethodReceivingLink)
                || ReferenceEquals(o, TwinReceivingLink)
                || ReferenceEquals(o, EventReceivingLink)
                || ReferenceEquals(o, MessageSendingLink)
                || ReferenceEquals(o, MethodSendingLink)
                || ReferenceEquals(o, TwinSendingLink)
            );
            Closed = true;
            Lock.Release();
            if (wasOpen)
            {
                AmqpSession?.Abort();
                AmqpAuthenticationRefresher?.StopLoop();
                RemoveDevice(DeviceIdentity);
                OnSessionClose();
            }
            if (Logging.IsEnabled) Logging.Exit(this, o, $"{nameof(OnLinkDisconnected)}");
        }


        public void OnConnectionClosed()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(OnConnectionClosed)}");
            
            Lock.Wait();
            bool wasOpen = !Closed;
            Closed = true;
            Lock.Wait();
            if (wasOpen)
            {
                AmqpAuthenticationRefresher?.StopLoop();
                RemoveDevice(DeviceIdentity);
                OnSessionClose();
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(OnConnectionClosed)}");
        }
        #endregion

        #region Helpers
        private async Task<ReceivingAmqpLink> OpenReceivingAmqpLinkAsync(
            byte? senderSettleMode,
            byte? receiverSettleMode,
            string deviceTemplate, 
            string moduleTemplate, 
            string linkSuffix,
            string CorrelationId,
            TimeoutHelper timeoutHelper
        )
        {
            AmqpSession amqpSession = AmqpSession;
            if (amqpSession == null)
            {
                throw new IotHubCommunicationException();
            }
            else
            {
                uint prefetchCount = DeviceIdentity.AmqpTransportSettings.PrefetchCount;

                AmqpLinkSettings amqpLinkSettings = new AmqpLinkSettings
                {
                    LinkName = CommonResources.GetNewStringGuid(linkSuffix),
                    Role = true,
                    TotalLinkCredit = prefetchCount,
                    AutoSendFlow = prefetchCount > 0,
                    Source = new Source() { Address = BuildLinkAddress(deviceTemplate, moduleTemplate) }
                };
                amqpLinkSettings.SndSettleMode = senderSettleMode;
                amqpLinkSettings.RcvSettleMode = receiverSettleMode;
                amqpLinkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeoutHelper.RemainingTime().TotalMilliseconds);
                amqpLinkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, DeviceIdentity.ProductInfo.ToString());
                amqpLinkSettings.AddProperty(IotHubAmqpProperty.ApiVersion, ClientApiVersionHelper.ApiVersionString);
                if (CorrelationId != null)
                {
                    amqpLinkSettings.AddProperty(IotHubAmqpProperty.ChannelCorrelationId, CorrelationId);
                }

                ReceivingAmqpLink receivingLink = new ReceivingAmqpLink(amqpLinkSettings);
                receivingLink.AttachTo(amqpSession);
                await receivingLink.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Info(this, receivingLink, $"{nameof(OpenReceivingAmqpLinkAsync)}");
                return receivingLink;
            }
        }

        private async Task<SendingAmqpLink> OpenSendingAmqpLinkAsync(
            byte? senderSettleMode,
            byte? receiverSettleMode,
            string deviceTemplate,
            string moduleTemplate,
            string linkSuffix,
            string CorrelationId,
            TimeoutHelper timeoutHelper
        )
        {
            AmqpSession amqpSession = AmqpSession;
            if (amqpSession == null)
            {
                throw new IotHubCommunicationException();
            }
            else
            {
                AmqpLinkSettings amqpLinkSettings = new AmqpLinkSettings
                {
                    LinkName = CommonResources.GetNewStringGuid(linkSuffix),
                    Role = false,
                    InitialDeliveryCount = 0,
                    Target = new Target() { Address = BuildLinkAddress(deviceTemplate, moduleTemplate) }
                };
                amqpLinkSettings.SndSettleMode = senderSettleMode;
                amqpLinkSettings.RcvSettleMode = receiverSettleMode;
                amqpLinkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeoutHelper.RemainingTime().TotalMilliseconds);
                amqpLinkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, DeviceIdentity.ProductInfo.ToString());
                amqpLinkSettings.AddProperty(IotHubAmqpProperty.ApiVersion, ClientApiVersionHelper.ApiVersionString);
                if (CorrelationId != null)
                {
                    amqpLinkSettings.AddProperty(IotHubAmqpProperty.ChannelCorrelationId, CorrelationId);
                }

                SendingAmqpLink sendingLink = new SendingAmqpLink(amqpLinkSettings);
                sendingLink.AttachTo(amqpSession);
                await sendingLink.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Info(this, sendingLink, $"{nameof(OpenSendingAmqpLinkAsync)}");
                return sendingLink;
            }
        }

        private string BuildLinkAddress(string deviceTemplate, string moduleTemplate)
        {
            string path;
            if (string.IsNullOrEmpty(DeviceIdentity.IotHubConnectionString.ModuleId))
            {
                path = string.Format(
                    CultureInfo.InvariantCulture,
                    deviceTemplate,
                    WebUtility.UrlEncode(DeviceIdentity.IotHubConnectionString.DeviceId)
                );
            }
            else
            {
                path = string.Format(
                    CultureInfo.InvariantCulture,
                    moduleTemplate,
                    WebUtility.UrlEncode(DeviceIdentity.IotHubConnectionString.DeviceId), WebUtility.UrlEncode(DeviceIdentity.IotHubConnectionString.ModuleId)
                );
            }
            return DeviceIdentity.IotHubConnectionString.BuildLinkAddress(path).AbsoluteUri;
        }

        private static async Task<Outcome> SendAmqpMessageAsync(SendingAmqpLink sendingAmqpLink, AmqpMessage message, TimeoutHelper timeoutHelper)
        {
            if (sendingAmqpLink != null)
            {
                return await sendingAmqpLink.SendMessageAsync(
                    message,
                    new ArraySegment<byte>(Guid.NewGuid().ToByteArray()),
                    AmqpConstants.NullBinary,
                    timeoutHelper.RemainingTime()
                ).ConfigureAwait(false);
            }
            else
            {
                throw new IotHubCommunicationException();
            }
        }

        private async Task<Message> ReceiveMessageAsync(ReceivingAmqpLink receivingAmqpLink, TimeoutHelper timeoutHelper)
        {
            if (Logging.IsEnabled) Logging.Info(this, receivingAmqpLink, $"{nameof(ReceiveMessageAsync)}");
            AmqpMessage amqpMessage = null;
            if (receivingAmqpLink != null)
            {
                amqpMessage = await receivingAmqpLink.ReceiveMessageAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }
            if (amqpMessage != null)
            {
                return new Message(amqpMessage)
                {
                    LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
                };
            }
            else
            {
                return null;
            }
        }
        protected static ArraySegment<byte> ConvertToDeliveryTag(string lockToken)
        {
            if (lockToken == null)
            {
                throw new ArgumentNullException("lockToken");
            }

            Guid lockTokenGuid;
            if (!Guid.TryParse(lockToken, out lockTokenGuid))
            {
                throw new ArgumentException("Should be a valid Guid", "lockToken");
            }

            var deliveryTag = new ArraySegment<byte>(lockTokenGuid.ToByteArray());
            return deliveryTag;
        }

        #endregion

        #region ILinkFactory
        public IAsyncResult BeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
        {
            if (Logging.IsEnabled) Logging.Info(this, link, $"{nameof(BeginOpenLink)}");
            return TaskHelpers.ToAsyncResult(OpenLinkAsync(link, timeout), callback, state);
        }
        public static Task<bool> OpenLinkAsync(AmqpLink link, TimeSpan timeout)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (timeout.TotalMilliseconds > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return Task.FromResult(true);
        }

        public AmqpLink CreateLink(AmqpSession session, AmqpLinkSettings settings)
        {
            if (Logging.IsEnabled) Logging.Info(this, session, $"{nameof(CreateLink)}");
            if (settings.IsReceiver())
            {
                return new ReceivingAmqpLink(session, settings);
            }
            else
            {
                return new SendingAmqpLink(session, settings);
            }
        }

        public void EndOpenLink(IAsyncResult result)
        {
            if (Logging.IsEnabled) Logging.Info(this, result, $"{nameof(EndOpenLink)}");
            if (result as Task == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Logging.IsEnabled) Logging.Info(this, disposing, $"{nameof(Dispose)}");
                try
                {
                    MessageReceivingLink?.Abort();
                    MethodReceivingLink?.Abort();
                    TwinReceivingLink?.Abort();
                    EventReceivingLink?.Abort();
                    MessageSendingLink?.Abort();
                    MethodSendingLink?.Abort();
                    TwinSendingLink?.Abort();
                    EventSendingLink?.Abort();
                    AmqpSession?.Abort();
                    AmqpAuthenticationRefresher?.StopLoop();
                    MessageReceivingLink = null;
                    MethodReceivingLink = null;
                    TwinReceivingLink = null;
                    EventReceivingLink = null;
                    MessageSendingLink = null;
                    MethodSendingLink = null;
                    TwinSendingLink = null;
                    EventSendingLink = null;
                    AmqpSession = null;
                    AmqpAuthenticationRefresher = null;
                }
                catch (Exception)
                {
                    if (Logging.IsEnabled) Logging.Info(this, disposing, "Discard any exception during disposing.");
                }
                Lock.Dispose();
            }
        }
        #endregion
    }
}