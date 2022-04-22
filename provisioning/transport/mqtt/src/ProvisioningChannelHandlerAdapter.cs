// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNetty.Buffers;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Transport.Channels;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    //
    // Note on ConfigureAwait: dotNetty is using a custom TaskScheduler that binds Tasks to the corresponding
    // EventLoop. To limit I/O to the EventLoopGroup and keep Netty semantics, we are going to ensure that the
    // task continuations are executed by this scheduler using ConfigureAwait(true).
    //
    internal class ProvisioningChannelHandlerAdapter : ChannelHandlerAdapter
    {
        private const string ExceptionPrefix = "MQTT Protocol Exception:";
        private const QualityOfService Qos = QualityOfService.AtLeastOnce;
        private const string UsernameFormat = "{0}/registrations/{1}/api-version={2}&ClientVersion={3}";
        private const string SubscribeFilter = "$dps/registrations/res/#";
        private const string RegisterTopic = "$dps/registrations/PUT/iotdps-register/?$rid={0}";
        private const string GetOperationsTopic = "$dps/registrations/GET/iotdps-get-operationstatus/?$rid={0}&operationId={1}";
        private static readonly Regex s_registrationStatusTopicRegex = new Regex("^\\$dps/registrations/res/(.*?)/\\?\\$rid=(.*?)$", RegexOptions.Compiled);
        private static readonly TimeSpan s_defaultOperationPoolingInterval = TimeSpan.FromSeconds(2);

        private const string Registration = "registration";
        private const string EmptyJson = "{}";

        private readonly ProvisioningTransportRegisterMessage _message;
        private readonly TaskCompletionSource<RegistrationOperationStatus> _taskCompletionSource;
        private readonly CancellationToken _cancellationToken;
        private int _state;
        private int _packetId;

        internal enum State
        {
            Start,
            Failed,
            WaitForConnack,
            WaitForSuback,
            WaitForPubAck,
            WaitForStatus,
            Done,
        }

        public ProvisioningChannelHandlerAdapter(
            ProvisioningTransportRegisterMessage message,
            TaskCompletionSource<RegistrationOperationStatus> taskCompletionSource,
            CancellationToken cancellationToken)
        {
            _message = message;
            _taskCompletionSource = taskCompletionSource;
            _cancellationToken = cancellationToken;

            ForceState(State.Start);
        }

        #region DotNetty.ChannelHandlerAdapter overrides

        public override async void ChannelActive(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, context.Name, nameof(ChannelActive));
            }

            await VerifyCancellationAsync(context).ConfigureAwait(true);

            try
            {
                ChangeState(State.Start, State.WaitForConnack);
                await ConnectAsync(context).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                if (ex is AggregateException)
                {
                    ex = ex.InnerException;
                }

                await FailWithExceptionAsync(context, ex).ConfigureAwait(true);
            }

            base.ChannelActive(context);

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, context.Name, nameof(ChannelActive));
            }
        }

        public override async void ChannelInactive(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, context.Name, nameof(ChannelInactive));
            }

            base.ChannelInactive(context);

            await FailWithExceptionAsync(
                context,
                new ProvisioningTransportException($"{ExceptionPrefix} Channel closed.")).ConfigureAwait(true);

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, context.Name, nameof(ChannelInactive));
            }
        }

        public override async void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, context.Name, nameof(ChannelRead));
            }

            Debug.Assert(message is Packet);
            await VerifyCancellationAsync(context).ConfigureAwait(true);

            await ProcessMessageAsync(context, (Packet)message).ConfigureAwait(true);

            base.ChannelRead(context, message);
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, context.Name, nameof(ChannelRead));
            }
        }

        public override async void ChannelReadComplete(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, context.Name, nameof(ChannelReadComplete));
            }

            await VerifyCancellationAsync(context).ConfigureAwait(true);

            base.ChannelReadComplete(context);

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, context.Name, nameof(ChannelReadComplete));
            }
        }

        public override async void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, context.Name, nameof(ExceptionCaught));
            }

            base.ExceptionCaught(context, exception);

            await FailWithExceptionAsync(context, exception).ConfigureAwait(true);
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, context.Name, nameof(ExceptionCaught));
            }
        }

        #endregion DotNetty.ChannelHandlerAdapter overrides

        private Task ConnectAsync(IChannelHandlerContext context)
        {
            string registrationId = _message.Security.GetRegistrationID();
            string userAgent = _message.ProductInfo;

            bool hasPassword = false;
            string password = null;
            if (_message.Security is SecurityProviderSymmetricKey)
            {
                hasPassword = true;
                string key = ((SecurityProviderSymmetricKey)_message.Security).GetPrimaryKey();
                password = ProvisioningSasBuilder.BuildSasSignature(Registration, key, string.Concat(_message.IdScope, '/', "registrations", '/', registrationId), TimeSpan.FromDays(1));
            }

            var message = new ConnectPacket()
            {
                CleanSession = true,
                ClientId = registrationId,
                HasWill = false,
                HasUsername = true,
                Username = string.Format(
                    CultureInfo.InvariantCulture,
                    UsernameFormat,
                    _message.IdScope,
                    registrationId,
                    ClientApiVersionHelper.ApiVersion,
                    Uri.EscapeDataString(userAgent)),
                HasPassword = hasPassword,
                Password = hasPassword ? password : null
            };

            return context.WriteAndFlushAsync(message);
        }

        private async Task ProcessMessageAsync(IChannelHandlerContext context, Packet message)
        {
            var currentState = (State)Volatile.Read(ref _state);

            switch (currentState)
            {
                case State.Start:
                    Debug.Fail($"{nameof(ProvisioningChannelHandlerAdapter)}: Invalid state: {nameof(State.Start)}");
                    break;

                case State.Done:
                    Debug.Fail($"{nameof(ProvisioningChannelHandlerAdapter)}: Invalid state: {nameof(State.Done)}");
                    break;

                case State.Failed:
                    Debug.Fail($"{nameof(ProvisioningChannelHandlerAdapter)}: Invalid state: {nameof(State.Failed)}");
                    break;

                case State.WaitForConnack:
                    await VerifyExpectedPacketTypeAsync(context, PacketType.CONNACK, message).ConfigureAwait(true);
                    await ProcessConnAckAsync(context, (ConnAckPacket)message).ConfigureAwait(true);
                    break;

                case State.WaitForSuback:
                    await VerifyExpectedPacketTypeAsync(context, PacketType.SUBACK, message).ConfigureAwait(true);
                    await ProcessSubAckAsync(context, (SubAckPacket)message).ConfigureAwait(true);
                    break;

                case State.WaitForPubAck:
                    ChangeState(State.WaitForPubAck, State.WaitForStatus);
                    await VerifyExpectedPacketTypeAsync(context, PacketType.PUBACK, message).ConfigureAwait(true);
                    break;

                case State.WaitForStatus:
                    await VerifyExpectedPacketTypeAsync(context, PacketType.PUBLISH, message).ConfigureAwait(true);
                    await ProcessRegistrationStatusAsync(context, (PublishPacket)message).ConfigureAwait(true);
                    break;

                default:
                    await FailWithExceptionAsync(
                        context,
                        new ProvisioningTransportException(
                            $"{ExceptionPrefix} Invalid state: {(State)_state}")).ConfigureAwait(true);
                    break;
            }
        }

        private async Task ProcessConnAckAsync(IChannelHandlerContext context, ConnAckPacket packet)
        {
            if (packet.SessionPresent)
            {
                await FailWithExceptionAsync(
                    context,
                    new ProvisioningTransportException(
                        $"{ExceptionPrefix} Unexpected CONNACK with SessionPresent.")).ConfigureAwait(true);
            }

            switch (packet.ReturnCode)
            {
                case ConnectReturnCode.Accepted:
                    try
                    {
                        ChangeState(State.WaitForConnack, State.WaitForSuback);
                        await SubscribeAsync(context).ConfigureAwait(true);
                    }
                    catch (Exception ex)
                    {
                        if (ex is AggregateException)
                        {
                            ex = ex.InnerException;
                        }

                        await FailWithExceptionAsync(context, ex).ConfigureAwait(true);
                    }

                    break;

                case ConnectReturnCode.RefusedUnacceptableProtocolVersion:
                case ConnectReturnCode.RefusedIdentifierRejected:
                case ConnectReturnCode.RefusedBadUsernameOrPassword:
                case ConnectReturnCode.RefusedNotAuthorized:
                    await FailWithExceptionAsync(
                        context,
                        new ProvisioningTransportException(
                            $"{ExceptionPrefix} CONNACK failed with {packet.ReturnCode}")).ConfigureAwait(true);
                    break;

                case ConnectReturnCode.RefusedServerUnavailable:
                    await FailWithExceptionAsync(
                        context,
                        new ProvisioningTransportException(
                            $"{ExceptionPrefix} CONNACK failed with {packet.ReturnCode}. Try again later.",
                            null,
                            true)).ConfigureAwait(true);
                    break;

                default:
                    await FailWithExceptionAsync(
                        context,
                        new ProvisioningTransportException(
                            $"{ExceptionPrefix} CONNACK failed unknown return code: {packet.ReturnCode}")).ConfigureAwait(true);
                    break;
            }
        }

        private Task SubscribeAsync(IChannelHandlerContext context)
        {
            var message = new SubscribePacket(GetNextPacketId(), new SubscriptionRequest(SubscribeFilter, Qos));
            return context.WriteAndFlushAsync(message);
        }

        private async Task ProcessSubAckAsync(IChannelHandlerContext context, SubAckPacket packet)
        {
            if (packet.PacketId == GetCurrentPacketId())
            {
                try
                {
                    ChangeState(State.WaitForSuback, State.WaitForPubAck);
                    await PublishRegisterAsync(context).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    if (ex is AggregateException)
                    {
                        ex = ex.InnerException;
                    }

                    await FailWithExceptionAsync(context, ex).ConfigureAwait(true);
                }
            }
        }

        private async Task PublishRegisterAsync(IChannelHandlerContext context)
        {
            IByteBuffer packagePayload = Unpooled.Empty;

            var deviceRegistration = new DeviceRegistration();

            if (!string.IsNullOrWhiteSpace(_message.Payload))
            {
                deviceRegistration.Payload = new JRaw(_message.Payload);
            }

            if (!string.IsNullOrWhiteSpace(_message.ClientCertificateSigningRequest))
            {
                deviceRegistration.ClientCertificateSigningRequest = _message.ClientCertificateSigningRequest;
            }

            string deviceRegistrationJsonString = JsonConvert.SerializeObject(deviceRegistration);

            if (deviceRegistrationJsonString != EmptyJson)
            {
                using var customContentStream = new MemoryStream(Encoding.UTF8.GetBytes(deviceRegistrationJsonString));
                long streamLength = customContentStream.Length;
                int length = (int)streamLength;
                packagePayload = context.Channel.Allocator.Buffer(length, length);
                await packagePayload.WriteBytesAsync(customContentStream, length).ConfigureAwait(false);
            }

            int packetId = GetNextPacketId();
            var message = new PublishPacket(Qos, false, false)
            {
                TopicName = string.Format(CultureInfo.InvariantCulture, RegisterTopic, packetId),
                PacketId = packetId,
                Payload = packagePayload
            };

            await context.WriteAndFlushAsync(message).ConfigureAwait(false);
        }

        private async Task VerifyPublishPacketTopicAsync(IChannelHandlerContext context, string topicName, string jsonData)
        {
            Match match = s_registrationStatusTopicRegex.Match(topicName);
            if (match.Groups.Count >= 2)
            {
                if (Enum.TryParse(match.Groups[1].Value, out HttpStatusCode statusCode))
                {
                    if (statusCode >= HttpStatusCode.BadRequest)
                    {
                        ProvisioningErrorDetailsMqtt errorDetails = JsonConvert.DeserializeObject<ProvisioningErrorDetailsMqtt>(jsonData);

                        bool isTransient = statusCode >= HttpStatusCode.InternalServerError || (int)statusCode == 429;

                        if (isTransient)
                        {
                            errorDetails.RetryAfter = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(topicName, s_defaultOperationPoolingInterval);
                        }

                        await FailWithExceptionAsync(
                             context,
                             new ProvisioningTransportException(
                                 jsonData,
                                 null,
                                 isTransient,
                                 errorDetails)).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task ProcessRegistrationStatusAsync(IChannelHandlerContext context, PublishPacket packet)
        {
            try // TODO : extract generic method for exception handling.
            {
                await PubAckAsync(context, packet.PacketId).ConfigureAwait(true);

                string jsonData = Encoding.UTF8.GetString(
                    packet.Payload.GetIoBuffer().Array,
                    packet.Payload.GetIoBuffer().Offset,
                    packet.Payload.GetIoBuffer().Count);

                await VerifyPublishPacketTopicAsync(context, packet.TopicName, jsonData).ConfigureAwait(true);

                RegistrationOperationStatus operation = JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonData);
                string operationId = operation.OperationId;
                operation.RetryAfter = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(packet.TopicName, s_defaultOperationPoolingInterval);

                if (string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusAssigning) == 0 ||
                    string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusUnassigned) == 0)
                {
                    await Task.Delay(operation.RetryAfter ?? RetryJitter.GenerateDelayWithJitterForRetry(s_defaultOperationPoolingInterval)).ConfigureAwait(true);
                    ChangeState(State.WaitForStatus, State.WaitForPubAck);
                    await PublishGetOperationAsync(context, operationId).ConfigureAwait(true);
                }
                else
                {
                    ChangeState(State.WaitForStatus, State.Done);
                    _taskCompletionSource.TrySetResult(operation);

                    await DoneAsync(context).ConfigureAwait(true);
                }
            }
            catch (ProvisioningTransportException te)
            {
                await FailWithExceptionAsync(context, te).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                var wrapperEx = new ProvisioningTransportException(
                    $"{ExceptionPrefix} Error while processing RegistrationStatus.",
                    ex,
                    false);

                await FailWithExceptionAsync(context, wrapperEx).ConfigureAwait(true);
            }
        }

        private static Task PubAckAsync(IChannelHandlerContext context, int packetId)
        {
            var message = new PubAckPacket
            {
                PacketId = packetId,
            };

            return context.WriteAndFlushAsync(message);
        }

        private Task PublishGetOperationAsync(IChannelHandlerContext context, string operationId)
        {
            int packetId = GetNextPacketId();

            var message = new PublishPacket(Qos, false, false)
            {
                TopicName = string.Format(CultureInfo.InvariantCulture, GetOperationsTopic, packetId, operationId),
                PacketId = packetId,
                Payload = Unpooled.Empty,
            };

            return context.WriteAndFlushAsync(message);
        }

        private async Task VerifyExpectedPacketTypeAsync(IChannelHandlerContext context, PacketType expectedPacketType, Packet message)
        {
            if (message.PacketType != expectedPacketType)
            {
                await FailWithExceptionAsync(
                    context,
                    new ProvisioningTransportException(
                        $"{ExceptionPrefix} Received unexpected packet type {message.PacketType} in state {(State)_state}")).ConfigureAwait(true);
            }
        }

        private async Task FailWithExceptionAsync(IChannelHandlerContext context, Exception ex)
        {
            if (Volatile.Read(ref _state) != (int)State.Failed)
            {
                if (Logging.IsEnabled)
                {
                    Logging.Error(this, $"Failing with Exception: {ex}", nameof(FailWithExceptionAsync));
                }

                ForceState(State.Failed);
                _taskCompletionSource.TrySetException(ex);

                await context.CloseAsync().ConfigureAwait(true);
            }
            else
            {
                if (Logging.IsEnabled)
                {
                    Logging.Error(this, $"Ignoring Exception: {ex}", nameof(FailWithExceptionAsync));
                }
            }
        }

        private async Task VerifyCancellationAsync(IChannelHandlerContext context)
        {
            if (_cancellationToken.IsCancellationRequested &&
                Volatile.Read(ref _state) != (int)State.Failed)
            {
                if (Logging.IsEnabled)
                {
                    Logging.Error(this, "CancellationRequested", nameof(VerifyCancellationAsync));
                }

                ForceState(State.Failed);
                _taskCompletionSource.TrySetCanceled(_cancellationToken);

                await context.CloseAsync().ConfigureAwait(true);
            }
        }

        private void ChangeState(State expectedCurrentState, State newState)
        {
            if (Logging.IsEnabled)
            {
                Logging.Info(this, $"{expectedCurrentState} -> {newState}", nameof(ChangeState));
            }

            int currentState = Interlocked.CompareExchange(ref _state, (int)newState, (int)expectedCurrentState);

            if (currentState != (int)expectedCurrentState)
            {
                string newStateString = Enum.GetName(typeof(State), newState);
                string currentStateString = Enum.GetName(typeof(State), currentState);
                string expectedStateString = Enum.GetName(typeof(State), expectedCurrentState);

                var exception = new ProvisioningTransportException(
                    $"{ExceptionPrefix} Unexpected state transition to {newStateString} from {currentStateString}. " +
                    $"Expecting {expectedStateString}");

                ForceState(State.Failed);
                _taskCompletionSource.TrySetException(exception);
            }
        }

        private void ForceState(State newState)
        {
            if (Logging.IsEnabled)
            {
                Logging.Info(this, $"{(State)_state} -> {newState}", nameof(ForceState));
            }

            Volatile.Write(ref _state, (int)newState);
        }

        private async Task DoneAsync(IChannelHandlerContext context)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, context.Name, nameof(DoneAsync));
            }

            try
            {
                await context.Channel.WriteAndFlushAsync(DisconnectPacket.Instance).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                if (Logging.IsEnabled)
                {
                    Logging.Info(this, $"Exception trying to send disconnect packet: {e}", nameof(DoneAsync));
                }

                await FailWithExceptionAsync(context, e).ConfigureAwait(true);
            }

            // This delay is required to work-around a .NET Framework CloseAsync bug.
            if (Logging.IsEnabled)
            {
                Logging.Info(this, "Applying close channel delay.", nameof(DoneAsync));
            }

            await Task.Delay(TimeSpan.FromMilliseconds(400)).ConfigureAwait(true);

            if (Logging.IsEnabled)
            {
                Logging.Info(this, "Closing channel.", nameof(DoneAsync));
            }

            try
            {
                await context.Channel.CloseAsync().ConfigureAwait(true);
            }
            catch (Exception e)
            {
                if (Logging.IsEnabled)
                {
                    Logging.Info(this, $"Exception trying to close channel: {e}", nameof(DoneAsync));
                }

                await FailWithExceptionAsync(context, e).ConfigureAwait(true);
            }

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, context.Name, nameof(DoneAsync));
            }
        }

        private ushort GetNextPacketId()
        {
            unchecked
            {
                ushort newIdShort;
                int newId = Interlocked.Increment(ref _packetId);

                newIdShort = (ushort)newId;
                return newIdShort == 0 ? GetNextPacketId() : newIdShort;
            }
        }

        private ushort GetCurrentPacketId()
        {
            unchecked
            {
                return (ushort)Volatile.Read(ref _packetId);
            }
        }
    }
}
