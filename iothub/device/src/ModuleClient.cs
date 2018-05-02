// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
#if NETSTANDARD1_3
    using System.Net.Http;
#endif
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.Devices.Client.Edge;

    /// <summary>
    ///    Status of handling a message. 
    ///    None - Means Device SDK won't send an ackowledge of receipt.
    ///    Completed - Means Device SDK will Complete the event. Removing from the queue.
    ///    Abandoned - Event will be Abandoned. 
    /// </summary>
    public enum MessageResponse { None, Completed, Abandoned };

    /// <summary>
    /// Delegate that gets called when a message is received on a particular input.
    /// </summary>
    /// <param name="message">The message received</param>
    /// <param name="userContext">The context object passed in</param>
    /// <returns>MessageResponse</returns>
    public delegate Task<MessageResponse> MessageHandler(Message message, object userContext);

    /// <summary>
    /// Contains methods that a device can use to send messages to and receive from the service.
    /// </summary>
    public sealed class ModuleClient : BaseClient
    {
        volatile Dictionary<string, Tuple<MessageHandler, object>> receiveEventEndpoints;

        readonly SemaphoreSlim receiveSemaphore = new SemaphoreSlim(1, 1);

        volatile Tuple<MessageHandler, object> defaultEventCallback;

        private ProductInfo productInfo = new ProductInfo();

        /// <summary>
        /// Stores custom product information that will be appended to the user agent string that is sent to IoT Hub.
        /// </summary>
        public string ProductInfo
        {
            // We store DeviceClient.ProductInfo as a string property of an object (rather than directly as a string)
            // so that updates will propagate down to the transport layer throughout the lifetime of the DeviceClient
            // object instance.
            get => productInfo.Extra;
            set => productInfo.Extra = value;
        }

        ModuleClient(IotHubConnectionString iotHubConnectionString, ITransportSettings[] transportSettings, IDeviceClientPipelineBuilder pipelineBuilder)
            : base(iotHubConnectionString, transportSettings)
        {
            if (string.IsNullOrWhiteSpace(iotHubConnectionString.ModuleId))
            {
                throw new ArgumentException("ModuleId not present - ModuleClient can be used only with modules");
            }

            var pipelineContext = new PipelineContext();
            pipelineContext.Set(transportSettings);
            pipelineContext.Set(iotHubConnectionString);
            pipelineContext.Set<OnMethodCalledDelegate>(OnMethodCalled);
            pipelineContext.Set<Action<TwinCollection>>(OnReportedStatePatchReceived);
            pipelineContext.Set<OnConnectionClosedDelegate>(OnConnectionClosed);
            pipelineContext.Set<OnConnectionOpenedDelegate>(OnConnectionOpened);
            pipelineContext.Set<OnReceiveEventMessageCalledDelegate>(OnReceiveEventMessageCalled);
            pipelineContext.Set(this.productInfo);

            IDelegatingHandler innerHandler = pipelineBuilder.Build(pipelineContext);
            this.InnerHandler = innerHandler;
        }

        /// <summary>
        /// Create an Amqp ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, IAuthenticationMethod authenticationMethod)
        {
            return Create(hostname, authenticationMethod, TransportType.Amqp);
        }

        /// <summary>
        /// Create an Amqp ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of Gateway</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod)
        {
            return Create(hostname, gatewayHostname, authenticationMethod, TransportType.Amqp);
        }

        /// <summary>
        /// Create a ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportType">The transportType used (Http1 or Amqp)</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, IAuthenticationMethod authenticationMethod, TransportType transportType)
        {
            return Create(hostname, null, authenticationMethod, transportType);
        }

        /// <summary>
        /// Create a ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of Gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportType">The transportType used (Http1 or Amqp)</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod, TransportType transportType)
        {
            if (hostname == null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            if (authenticationMethod == null)
            {
                throw new ArgumentNullException(nameof(authenticationMethod));
            }

            IotHubConnectionStringBuilder connectionStringBuilder = IotHubConnectionStringBuilder.Create(hostname, gatewayHostname, authenticationMethod);

#if !NETMF
            if (authenticationMethod is DeviceAuthenticationWithX509Certificate)
            {
                throw new ArgumentException("Certificate authentication is not supported for modules");
            }
#endif

            return CreateFromConnectionString(connectionStringBuilder.ToString(), authenticationMethod, transportType, null);
        }

        /// <summary>
        /// Create a ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings)
        {
            return Create(hostname, null, authenticationMethod, transportSettings);
        }

        /// <summary>
        /// Create a ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of Gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings)
        {
            if (hostname == null)
            {
                throw new ArgumentNullException("hostname");
            }

            if (authenticationMethod == null)
            {
                throw new ArgumentNullException("authenticationMethod");
            }

            var connectionStringBuilder = IotHubConnectionStringBuilder.Create(hostname, gatewayHostname, authenticationMethod);
#if !NETMF
            if (authenticationMethod is DeviceAuthenticationWithX509Certificate)
            {
                throw new ArgumentException("Certificate authentication is not supported for modules");
            }
#endif
            return CreateFromConnectionString(connectionStringBuilder.ToString(), authenticationMethod, transportSettings, null);
        }

        /// <summary>
        /// Create a ModuleClient using Amqp transport from the specified connection string
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient CreateFromConnectionString(string connectionString)
        {
            return CreateFromConnectionString(connectionString, TransportType.Amqp);
        }

        /// <summary>
        /// Create ModuleClient from the specified connection string using the specified transport type
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <param name="transportType">Specifies whether Amqp or Http transport is used</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient CreateFromConnectionString(string connectionString, TransportType transportType)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            return CreateFromConnectionString(connectionString, null, transportType, null);
        }

        /// <summary>
        /// Create ModuleClient from the specified connection string using a prioritized list of transports
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (with DeviceId)</param>
        /// <param name="transportSettings">Prioritized list of transports and their settings</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient CreateFromConnectionString(string connectionString,
            ITransportSettings[] transportSettings)
        {
            return CreateFromConnectionString(connectionString, null, transportSettings, null);
        }

        internal static ModuleClient CreateFromConnectionString(
            string connectionString,
            IAuthenticationMethod authenticationMethod,
            TransportType transportType,
            IDeviceClientPipelineBuilder pipelineBuilder)
        {
            ITransportSettings[] transportSettings = GetTransportSettings(transportType);
            return CreateFromConnectionString(
                connectionString,
                authenticationMethod,
                transportSettings,
                pipelineBuilder);
        }

        internal static ModuleClient CreateFromConnectionString(
            string connectionString,
            IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings,
            IDeviceClientPipelineBuilder pipelineBuilder)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings));
            }

            if (transportSettings.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(connectionString), "Must specify at least one TransportSettings instance");
            }

            var builder = IotHubConnectionStringBuilder.CreateWithIAuthenticationOverride(
                connectionString,
                authenticationMethod);

            IotHubConnectionString iotHubConnectionString = builder.ToIotHubConnectionString();

            ValidateTransportSettings(transportSettings);

            pipelineBuilder = pipelineBuilder ?? BuildPipeline();

            // Defer concrete ModuleClient creation to OpenAsync
            return new ModuleClient(iotHubConnectionString, transportSettings, pipelineBuilder);
        }

        /// <summary>
        /// Creates a ModuleClient instance in an IoT Edge deployment
        /// based on environment variables.
        /// </summary>
        /// <returns>ModuleClient instance</returns>
        public static ModuleClient Create()
        {
            return Create(TransportType.Amqp);
        }


        /// <summary>
        /// Creates a ModuleClient instance in an IoT Edge deployment
        /// based on environment variables.
        /// </summary>
        /// <param name="transportType">Specifies whether Amqp or Http transport is used</param>
        /// <returns>ModuleClient instance</returns>
        public static ModuleClient Create(TransportType transportType)
        {
            return Create(GetTransportSettings(transportType));
        }

        /// <summary>
        /// Creates a ModuleClient instance in an IoT Edge deployment
        /// based on environment variables.
        /// </summary>
        /// <param name="transportSettings">Prioritized list of transports and their settings</param>
        /// <returns>ModuleClient instance</returns>
        public static ModuleClient Create(ITransportSettings[] transportSettings)
        {
            return new EdgeModuleClientFactory(transportSettings).Create();
        }

        #region Module Specific API
        /// <summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="message">The message to send</param>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(string outputName, Message message)
        {
            // Codes_SRS_DEVICECLIENT_10_012: [If `outputName` is `null` or empty, an `ArgumentNullException` shall be thrown.]
            if (string.IsNullOrWhiteSpace(outputName))
            {
                throw new ArgumentException(nameof(outputName));
            }

            // Codes_SRS_DEVICECLIENT_10_013: [If `message` is `null` or empty, an `ArgumentNullException` shall be thrown.]
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            // Codes_SRS_DEVICECLIENT_10_015: [The `output` property of a given `message` shall be assigned the value `outputName` before submitting each request to the transport layer.]
            message.SystemProperties.Add(MessageSystemPropertyNames.OutputName, outputName);

            // Codes_SRS_DEVICECLIENT_10_011: [The `SendEventAsync` operation shall retry sending `message` until the `BaseClient::RetryStrategy` tiemspan expires or unrecoverable error(authentication or quota exceed) occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.SendEventAsync(message, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="messages">A list of one or more messages to send</param>
        /// </summary>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages)
        {
            // Codes_SRS_DEVICECLIENT_10_012: [If `outputName` is `null` or empty, an `ArgumentNullException` shall be thrown.]
            if (string.IsNullOrWhiteSpace(outputName))
            {
                throw new ArgumentException(nameof(outputName));
            }

            List<Message> messagesList = messages?.ToList();
            // Codes_SRS_DEVICECLIENT_10_013: [If `message` is `null` or empty, an `ArgumentNullException` shall be thrown]
            if (messagesList == null || messagesList.Count == 0)
            {
                throw new ArgumentNullException(nameof(messages));
            }

#if PCL
            foreach(var m in messagesList)
            {
                m.SystemProperties.Add(MessageSystemPropertyNames.OutputName, outputName);
            }
#else
            // Codes_SRS_DEVICECLIENT_10_015: [The `module-output` property of a given `message` shall be assigned the value `outputName` before submitting each request to the transport layer.]
            messagesList.ForEach(m => m.SystemProperties.Add(MessageSystemPropertyNames.OutputName, outputName));
#endif

            // Codes_SRS_DEVICECLIENT_10_014: [The `SendEventBachAsync` operation shall retry sending `messages` until the `BaseClient::RetryStrategy` tiemspan expires or unrecoverable error(authentication or quota exceed) occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.SendEventAsync(messagesList, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Registers a new delgate for the particular input. If a delegate is already associated with
        /// the input, it will be replaced with the new delegate.
        /// <param name="inputName">The name of the input to associate with the delegate.</param>
        /// <param name="messageHandler">The delegate to be used when a message is sent to the particular inputName.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>
        /// <returns>The task containing the event</returns>
        public async Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler, object userContext)
        {
            try
            {
                await this.receiveSemaphore.WaitAsync();

                if (messageHandler != null)
                {
                    // codes_SRS_DEVICECLIENT_33_003: [ It shall EnableEventReceiveAsync when called for the first time. ]
                    await this.EnableEventReceiveAsync();
                    // codes_SRS_DEVICECLIENT_33_005: [ It shall lazy-initialize the receiveEventEndpoints property. ]
                    if (this.receiveEventEndpoints == null)
                    {
                        this.receiveEventEndpoints = new Dictionary<string, Tuple<MessageHandler, object>>();
                    }
                    this.receiveEventEndpoints[inputName] = new Tuple<MessageHandler, object>(messageHandler, userContext);
                }
                else
                {
                    if (this.receiveEventEndpoints != null)
                    {
                        this.receiveEventEndpoints.Remove(inputName);
                        if (this.receiveEventEndpoints.Count == 0)
                        {
                            this.receiveEventEndpoints = null;
                        }
                    }
                    // codes_SRS_DEVICECLIENT_33_004: [ It shall call DisableEventReceiveAsync when the last delegate has been removed. ]
                    await this.DisableEventReceiveAsync();
                }
            }
            finally
            {
                this.receiveSemaphore.Release();
            }
        }

        /// <summary>
        /// Registers a new default delegate which applies to all endpoints. If a delegate is already associated with
        /// the input, it will be called, else the default delegate will be called. If a default delegate was set previously,
        /// it will be overwritten.
        /// <param name="messageHandler">The delegate to be called when a message is sent to any input.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>
        /// <returns>The task containing the event</returns>
        public async Task SetMessageHandlerAsync(MessageHandler messageHandler, object userContext)
        {
            try
            {
                await this.receiveSemaphore.WaitAsync();
                if (messageHandler != null)
                {
                    // codes_SRS_DEVICECLIENT_33_003: [ It shall EnableEventReceiveAsync when called for the first time. ]
                    await this.EnableEventReceiveAsync();
                    this.defaultEventCallback = new Tuple<MessageHandler, object>(messageHandler, userContext);
                }
                else
                {
                    this.defaultEventCallback = null;
                    // codes_SRS_DEVICECLIENT_33_004: [ It shall DisableEventReceiveAsync when the last delegate has been removed. ]
                    await this.DisableEventReceiveAsync();
                }
            }
            finally
            {
                this.receiveSemaphore.Release();
            }
        }

        private async Task EnableEventReceiveAsync()
        {
            if (this.receiveEventEndpoints == null && this.defaultEventCallback == null)
            {
                await ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.EnableEventReceiveAsync(operationTimeoutCancellationToken));
            }
        }

        private async Task DisableEventReceiveAsync()
        {
            if (this.receiveEventEndpoints == null && this.defaultEventCallback == null)
            {
                await ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.DisableEventReceiveAsync(operationTimeoutCancellationToken));
            }
        }

        /// <summary>
        /// The delegate for handling event messages received
        /// <param name="input">The input on which a message is received</param>
        /// <param name="message">The message received</param>
        /// </summary>
        internal async Task OnReceiveEventMessageCalled(string input, Message message)
        {
            // codes_SRS_DEVICECLIENT_33_001: [ If the given eventMessageInternal argument is null, fail silently ]
            if (message != null)
            {
                Tuple<MessageHandler, object> callback = null;
                await this.receiveSemaphore.WaitAsync();
                try
                {
                    // codes_SRS_DEVICECLIENT_33_006: [ The OnReceiveEventMessageCalled shall get the default delegate if a delegate has not been assigned. ]
                    if (this.receiveEventEndpoints == null ||
                        string.IsNullOrWhiteSpace(input) ||
                        !this.receiveEventEndpoints.TryGetValue(input, out callback))
                    {
                        callback = this.defaultEventCallback;
                    }
                }
                finally
                {
                    this.receiveSemaphore.Release();
                }

                // codes_SRS_DEVICECLIENT_33_002: [ The OnReceiveEventMessageCalled shall invoke the specified delegate. ]
                MessageResponse response = await (callback?.Item1?.Invoke(message, callback.Item2) ?? Task.FromResult(MessageResponse.None));

                switch (response)
                {
                    case MessageResponse.Completed:
                        await this.CompleteAsync(message);
                        break;
                    case MessageResponse.Abandoned:
                        await this.AbandonAsync(message);
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion Module Specific API
    }
}
