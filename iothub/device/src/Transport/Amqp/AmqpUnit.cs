using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpUnit : IAmqpUnit
    {
        public event EventHandler OnUnitDisconnected;
        private readonly DeviceIdentity DeviceIdentity;
        private readonly Func<MethodRequestInternal, Task> MethodHandler;
        private readonly Action<AmqpMessage> TwinMessageListener;
        private readonly Func<string, Message, Task> EventListener;
        private readonly Func<DeviceIdentity, ILinkFactory, AmqpSessionSettings, TimeSpan, Task<AmqpSession>> AmqpSessionCreator;
        private readonly Func<DeviceIdentity, TimeSpan, Task<IAmqpAuthenticationRefresher>> AmqpAuthenticationRefresherCreator;
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
        private IAmqpAuthenticationRefresher AmqpAuthenticationRefresher;
        private bool Closed;
        private AmqpSessionSettings AmqpSessionSettings;

        public AmqpUnit(
            DeviceIdentity deviceIdentity,
            Func<DeviceIdentity, ILinkFactory, AmqpSessionSettings, TimeSpan, Task<AmqpSession>> amqpSessionCreator,
            Func<DeviceIdentity, TimeSpan, Task<IAmqpAuthenticationRefresher>> amqpAuthenticationRefresherCreator,
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<AmqpMessage> twinMessageListener, 
            Func<string, Message, Task> eventListener)
        {
            Lock = new SemaphoreSlim(1, 1);
            DeviceIdentity = deviceIdentity;
            MethodHandler = methodHandler;
            TwinMessageListener = twinMessageListener;
            EventListener = eventListener;
            AmqpSessionCreator = amqpSessionCreator;
            AmqpAuthenticationRefresherCreator = amqpAuthenticationRefresherCreator;
            Closed = false;
            AmqpSessionSettings = new AmqpSessionSettings()
             {
                 Properties = new Fields()
             };
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
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
           try
            {
                if (!Closed && AmqpSession == null)
                { 
                    AmqpSession = await AmqpSessionCreator.Invoke(DeviceIdentity, this, AmqpSessionSettings, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    if (Logging.IsEnabled) Logging.Associate(this, AmqpSession, $"{nameof(AmqpSession)}");
                    await AmqpSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    if (DeviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
                    {
                        AmqpAuthenticationRefresher = await AmqpAuthenticationRefresherCreator.Invoke(DeviceIdentity, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        if (Logging.IsEnabled) Logging.Associate(this, AmqpAuthenticationRefresher, $"{nameof(AmqpAuthenticationRefresher)}");
                    }
                    await OpenMessageLinksAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    AmqpSession.Closed += OnSessionDisconnected;
                    if (Logging.IsEnabled) Logging.Info(this, $"Connected: {DeviceIdentity.GetHashCode()}<->DeviceId: {DeviceIdentity.IotHubConnectionString.DeviceId}<->AmqpSession[{AmqpSession.GetHashCode()}]");
                }
            }
            catch(Exception)
            {
                Closed = true;
                if (AmqpSession != null)
                {
                    await AmqpSession.CloseAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                }
                throw;
            }
            finally
            {
                Lock.Release();
            }
            
            if (Logging.IsEnabled) Logging.Exit(this, timeoutHelper.RemainingTime(), $"{nameof(OpenAsync)}");
        }

        public async Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(CloseAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            bool wasOpen = !Closed;
            if (wasOpen)
            {
                Closed = true;
            }
            Lock.Release();
            if (wasOpen && AmqpSession != null)
            {
                try
                {
                    await AmqpSession.CloseAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    if (Logging.IsEnabled) Logging.Info(this, "Discard any exception.", $"{nameof(CloseAsync)}");
                }
                OnUnitDisconnected?.Invoke(this, EventArgs.Empty);
            }
            if (Logging.IsEnabled) Logging.Exit(this, timeoutHelper.RemainingTime(), $"{nameof(CloseAsync)}");
        }
        #endregion

        #region Message
        private async Task OpenMessageLinksAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(OpenMessageLinksAsync)}");
            Task<SendingAmqpLink> sendingLinkCreator = AmqpLinkHelper.OpenTelemetrySenderLinkAsync(
                DeviceIdentity,
                AmqpSession,
                timeout
            );
            Task<ReceivingAmqpLink> receiveLinkCreator = AmqpLinkHelper.OpenTelemetryReceiverLinkAsync(
                DeviceIdentity,
                AmqpSession, 
                timeout
            );
            await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);

            MessageReceivingLink = receiveLinkCreator.Result;
            MessageSendingLink = sendingLinkCreator.Result;
            MessageReceivingLink.Closed += OnLinkDisconnected;
            MessageSendingLink.Closed += OnLinkDisconnected;
            if (Logging.IsEnabled) Logging.Associate(this, MessageReceivingLink, $"{nameof(MessageReceivingLink)}");
            if (Logging.IsEnabled) Logging.Associate(this, MessageSendingLink, $"{nameof(MessageSendingLink)}");
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(OpenMessageLinksAsync)}");
        }

        public async Task<Outcome> SendMessageAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, timeout, $"{nameof(SendMessageAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            SendingAmqpLink messageSendingLink = MessageSendingLink;
            Lock.Release();
            if (messageSendingLink == null)
            {
                throw new IotHubCommunicationException();
            }
            else
            {
                Outcome outcome = await AmqpLinkHelper.SendAmqpMessageAsync(messageSendingLink, message, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, message, timeoutHelper.RemainingTime(), $"{nameof(SendMessageAsync)}");
                return outcome;
            }
        }

        public async Task<Message> ReceiveMessageAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(ReceiveMessageAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            try
            {
                if (MessageSendingLink == null)
                {
                    throw new IotHubCommunicationException();
                }
                else
                {
                    AmqpMessage amqpMessage = await AmqpLinkHelper.ReceiveAmqpMessageAsync(MessageReceivingLink, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    Message message = null;
                    if (amqpMessage != null)
                    {
                        message = new Message(amqpMessage)
                        {
                            LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
                        };
                    }
                    if (Logging.IsEnabled) Logging.Exit(this, timeoutHelper.RemainingTime(), $"{nameof(ReceiveMessageAsync)}");
                    return message;
                }
            }
            finally
            {
                Lock.Release();
            }
        }
        #endregion

        #region Event
        public async Task EnableEventReceiveAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnableEventReceiveAsync)}");
            if (EventReceivingLink == null)
            {
                EventReceivingLink = await AmqpLinkHelper.OpenEventsReceiverLinkAsync(
                    DeviceIdentity,
                    AmqpSession,
                    timeout
                ).ConfigureAwait(false);
                EventReceivingLink.RegisterMessageListener(OnEventsReceived);
                EventReceivingLink.Closed += OnLinkDisconnected;
                if (Logging.IsEnabled) Logging.Associate(this, EventReceivingLink, $"{nameof(EventReceivingLink)}");
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnableEventReceiveAsync)}");
                
            }
        }

        public async Task<Outcome> SendEventAsync(AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, message, timeout, $"{nameof(SendEventAsync)}");
            Outcome outcome = await SendMessageAsync(message, timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, message, timeout, $"{nameof(SendEventAsync)}");
            return outcome;
        }

        internal void OnEventsReceived(AmqpMessage amqpMessage)
        {
            Message message = new Message(amqpMessage)
            {
                LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
            };
            EventListener?.Invoke(message.InputName, message);
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
                    Task<ReceivingAmqpLink> receiveLinkCreator = AmqpLinkHelper.OpenMethodsReceiverLinkAsync(
                        DeviceIdentity,
                        AmqpSession,
                        correlationIdSuffix,
                        timeoutHelper.RemainingTime()
                    );
                    Task<SendingAmqpLink> sendingLinkCreator = AmqpLinkHelper.OpenMethodsSenderLinkAsync(
                        DeviceIdentity,
                        AmqpSession,
                        correlationIdSuffix,
                        timeoutHelper.RemainingTime()
                    );
                    await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);

                    methodReceivingLink = receiveLinkCreator.Result;
                    methodSendingLink = sendingLinkCreator.Result;

                    MethodSendingLink = methodSendingLink;
                    MethodReceivingLink = methodReceivingLink;

                    MethodReceivingLink.RegisterMessageListener(OnMethodReceived);
                    MethodSendingLink.Closed += OnLinkDisconnected;
                    MethodReceivingLink.Closed += OnLinkDisconnected;

                    if (Logging.IsEnabled) Logging.Associate(this, MethodReceivingLink, $"{nameof(MethodReceivingLink)}");
                    if (Logging.IsEnabled) Logging.Associate(this, MethodSendingLink, $"{nameof(MethodSendingLink)}");
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
            if (Logging.IsEnabled) Logging.Exit(this, timeoutHelper.RemainingTime(), $"{nameof(DisableMethodsAsync)}");
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
                Outcome outcome = await AmqpLinkHelper.SendAmqpMessageAsync(methodSendingLink, message, timeoutHelper.RemainingTime()).ConfigureAwait(false);
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
                    Task<ReceivingAmqpLink> receiveLinkCreator = AmqpLinkHelper.OpenTwinReceiverLinkAsync(
                        DeviceIdentity,
                        AmqpSession,
                        correlationIdSuffix,
                        timeoutHelper.RemainingTime()
                    );
                    Task<SendingAmqpLink> sendingLinkCreator = AmqpLinkHelper.OpenTwinSenderLinkAsync(
                        DeviceIdentity,
                        AmqpSession,
                        correlationIdSuffix,
                        timeoutHelper.RemainingTime()
                    );
                    await Task.WhenAll(receiveLinkCreator, sendingLinkCreator).ConfigureAwait(false);
                    twinReceivingLink = receiveLinkCreator.Result;
                    twinSendingLink = sendingLinkCreator.Result;
                    TwinReceivingLink = twinReceivingLink;
                    TwinSendingLink = twinSendingLink;
                    TwinReceivingLink.RegisterMessageListener(OnDesiredPropertyReceived);
                    TwinReceivingLink.Closed += OnLinkDisconnected;
                    TwinSendingLink.Closed += OnLinkDisconnected;
                    if (Logging.IsEnabled) Logging.Associate(this, TwinReceivingLink, $"{nameof(TwinReceivingLink)}");
                    if (Logging.IsEnabled) Logging.Associate(this, TwinSendingLink, $"{nameof(TwinSendingLink)}");
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
            if (twinSendingLink == null)
            {
                throw new IotHubCommunicationException();
            }
            else
            {
                Outcome outcome = await AmqpLinkHelper.SendAmqpMessageAsync(twinSendingLink, message, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(SendTwinMessageAsync)}");
                return outcome;
            }
        }
        #endregion

        #region Connectivity Event
        public void OnConnectionDisconnected()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(OnConnectionDisconnected)}");
            Lock.Wait();
            bool wasOpen = !Closed;
            Closed = true;
            Lock.Release();
            if (wasOpen)
            {
                OnUnitDisconnected?.Invoke(this, EventArgs.Empty);
                Cleanup();
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(OnConnectionDisconnected)}");
        }

        private void OnSessionDisconnected(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, o, $"{nameof(OnSessionDisconnected)}");
            Lock.Wait();
            bool wasOpen = !Closed && ReferenceEquals(o, AmqpSession);
            Closed = true;
            Lock.Release();
            if (wasOpen)
            {
                OnUnitDisconnected?.Invoke(this, EventArgs.Empty);
                Cleanup();
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
                OnUnitDisconnected?.Invoke(this, EventArgs.Empty);
                Cleanup();
            }
            if (Logging.IsEnabled) Logging.Exit(this, o, $"{nameof(OnLinkDisconnected)}");
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

        private void Cleanup()
        {
            try
            {
                AmqpLinkHelper.CloseAmqpObject(MessageReceivingLink);
                AmqpLinkHelper.CloseAmqpObject(MessageReceivingLink);
                AmqpLinkHelper.CloseAmqpObject(MethodReceivingLink);
                AmqpLinkHelper.CloseAmqpObject(TwinReceivingLink);
                AmqpLinkHelper.CloseAmqpObject(EventReceivingLink);
                AmqpLinkHelper.CloseAmqpObject(MessageSendingLink);
                AmqpLinkHelper.CloseAmqpObject(MethodSendingLink);
                AmqpLinkHelper.CloseAmqpObject(TwinSendingLink);
                AmqpLinkHelper.CloseAmqpObject(EventSendingLink);
                AmqpLinkHelper.CloseAmqpObject(AmqpSession);
                AmqpAuthenticationRefresher?.Dispose();
            }
            catch (Exception)
            {
                if (Logging.IsEnabled) Logging.Info(this, "Discard any exception during cleanup.");
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Logging.IsEnabled) Logging.Enter(this, disposing, $"{nameof(Dispose)}");
                Closed = true;
                Cleanup();
                if (Logging.IsEnabled) Logging.Exit(this, disposing, $"{nameof(Dispose)}");
                Lock.Dispose();
            }
        }
        
        public async Task<Outcome> DisposeMessageAsync(string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, lockToken, $"{nameof(DisposeMessageAsync)}");
            Outcome disposeOutcome = await AmqpLinkHelper.DisposeMessageAsync(MessageReceivingLink, lockToken, outcome, timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, lockToken, $"{nameof(DisposeMessageAsync)}");
            return disposeOutcome;
        }
        #endregion
    }
}