// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
#if !NET451
    using System.Net.Http;
#else
    using System.Net;
#endif
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Client.Transport;
    using Microsoft.Azure.Devices.Shared;
    using System.Security.Cryptography.X509Certificates;
    using System.IO;
    using Microsoft.Azure.Devices.Client.Exceptions;

    /// <summary>
    /// Delegate for desired property update callbacks.  This will be called
    /// every time we receive a PATCH from the service.
    /// </summary>
    /// <param name="desiredProperties">Properties that were contained in the update that was received from the service</param>
    /// <param name="userContext">Context object passed in when the callback was registered</param>
    public delegate Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext);

    /// <summary>
    ///      Delegate for method call. This will be called every time we receive a method call that was registered.
    /// </summary>
    /// <param name="methodRequest">Class with details about method.</param>
    /// <param name="userContext">Context object passed in when the callback was registered.</param>
    /// <returns>MethodResponse</returns>
    public delegate Task<MethodResponse> MethodCallback(MethodRequest methodRequest, object userContext);

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
    /// Delegate for connection status changed.
    /// </summary>
    /// <param name="status">The updated connection status</param>
    /// <param name="reason">The reason for the connection status change</param>
    public delegate void ConnectionStatusChangesHandler(ConnectionStatus status, ConnectionStatusChangeReason reason);

    /*
     * Class Diagram and Chain of Responsibility in Device Client 
     * 
                                         +--------------------+
                                         | <<interface>>      |
                                         | IDelegatingHandler |
                                         |  * Open            |
                                         |  * Close           |
                                         |  * SendEvent       |
                                         |  * SendEvents      |
                                         |  * Receive         |
                                         |  * Complete        |
                                         |  * Abandon         |
                                         |  * Reject          |
                                         +-------+------------+
                                                 |
                                                 |implements
                                                 |
                                                 |
                                         +-------+-------+
                                         |  <<abstract>> |     
                                         |  Default      |
     +----------------+                  |  Delegating   <------inherits-----------------+
     | ClientFactory  |                  |  Handler      |                               |
     +-------|--------+      +--inherits->               <--inherits----+                |
             |               |           +-------^-------+              |                |
             |               |                   |inherits              |                |
             |               |                   |                      |                |
     +-------v--------+  +---+---------+      +--+----------+       +---+--------+       +--------------+
     |                |  |             |      |             |       | Protocol   |       | <<abstract>> |
     | InternalClient |  | Retry       | use  |  Error      |  use  | Routing    |  use  | Transport    |
     |                |--> Delegating  +------>  Delegating +-------> Delegating +-------> Delegating   |
     +----------------+  | Handler     |      |  Handler    |       | Handler    |       | Handler      |
             |           |             |      |             |       |            |       |              |
             |           | overrides:  |      |  overrides  |       | overrides: |       | overrides:   |
       ------+------     |  Open       |      |   Open      |       |  Open      |       |  Receive     |
       |           |     |  SendEvent  |      |   SendEvent |       |            |       |              |
    +-------+ +--------+ |  SendEvents |      |   SendEvents|       +------------+       +--^--^---^----+
    |Device | | Module | |  Receive    |      |   Receive   |                               |  |   |
    |Client | | Client | |  Reject     |      |   Reject    |                               |  |   |
    +-------+ +--------+ |  Abandon    |      |   Abandon   |                               |  |   |
                         |  Complete   |      |   Complete  |                               |  |   |
                         |             |      |             |                               |  |   |
                         +-------------+      +-------------+     +-------------+-+inherits-+  |   +---inherits-+-------------+
                                                                  |             |              |                |             |
                                                                  | AMQP        |              inherits         | HTTP        |
                                                                  | Transport   |              |                | Transport   |
                                                                  | Handler     |          +---+---------+      | Handler     |
                                                                  |             |          |             |      |             |
                                                                  | overrides:  |          | MQTT        |      | overrides:  |
                                                                  |  everything |          | Transport   |      |  everything |
                                                                  |             |          | Handler     |      |             |
                                                                  +-------------+          |             |      +-------------+
                                                                                           | overrides:  |
                                                                                           |  everything |
                                                                                           |             |
                                                                                           +-------------+
    */

    /// <summary>
    /// Contains methods that a device can use to send messages to and receive from the service.
    /// </summary>
    internal class InternalClient : IDisposable
    {
        private const double _defaultDeviceStreamingTimeoutSecs = 60;
        private uint _operationTimeoutInMilliseconds = DeviceClient.DefaultOperationTimeoutInMilliseconds;
        private int _diagnosticSamplingPercentage = 0;
        private ITransportSettings[] transportSettings;
        private SemaphoreSlim methodsDictionarySemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim receiveSemaphore = new SemaphoreSlim(1, 1);
        private SemaphoreSlim deviceStreamsSemaphore = new SemaphoreSlim(1, 1);
        private volatile Dictionary<string, Tuple<MessageHandler, object>> receiveEventEndpoints;
        private volatile Tuple<MessageHandler, object> defaultEventCallback;
        private ProductInfo productInfo = new ProductInfo();

        /// <summary>
        /// Stores Methods supported by the client device and their associated delegate.
        /// </summary>
        private volatile Dictionary<string, Tuple<MethodCallback, object>> deviceMethods;

        private volatile Tuple<MethodCallback, object> deviceDefaultMethodCallback;

        private volatile ConnectionStatusChangesHandler connectionStatusChangesHandler;
        private ConnectionStatus lastConnectionStatus = ConnectionStatus.Disconnected;
        private ConnectionStatusChangeReason lastConnectionStatusChangeReason = ConnectionStatusChangeReason.Client_Close;


        internal delegate Task OnMethodCalledDelegate(MethodRequestInternal methodRequestInternal);

        internal delegate Task OnReceiveEventMessageCalledDelegate(string input, Message message);

        /// <summary>
        /// Callback to call whenever the twin's desired state is updated by the service
        /// </summary>
        internal DesiredPropertyUpdateCallback desiredPropertyUpdateCallback;

        /// <summary>
        /// Has twin functionality been enabled with the service?
        /// </summary>
        Boolean patchSubscribedWithService = false;

        /// <summary>
        /// userContext passed when registering the twin patch callback
        /// </summary>
        Object twinPatchCallbackContext = null;

        private int _currentMessageCount = 0;

        public InternalClient(IotHubConnectionString iotHubConnectionString, ITransportSettings[] transportSettings, IDeviceClientPipelineBuilder pipelineBuilder)
        {
            if (Logging.IsEnabled) Logging.Enter(this, transportSettings, pipelineBuilder, nameof(InternalClient) + "_ctor");

#if NET451
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
#endif

            this.IotHubConnectionString = iotHubConnectionString;

            var pipelineContext = new PipelineContext();
            pipelineContext.Set(transportSettings);
            pipelineContext.Set(iotHubConnectionString);
            pipelineContext.Set<OnMethodCalledDelegate>(OnMethodCalled);
            pipelineContext.Set<Action<TwinCollection>>(OnReportedStatePatchReceived);
            pipelineContext.Set<ConnectionStatusChangesHandler>(OnConnectionStatusChanged);
            pipelineContext.Set<OnReceiveEventMessageCalledDelegate>(OnReceiveEventMessageCalled);
            pipelineContext.Set(this.productInfo);

            IDelegatingHandler innerHandler = pipelineBuilder.Build(pipelineContext);

            if (Logging.IsEnabled) Logging.Associate(this, innerHandler, nameof(InternalClient));

            this.InnerHandler = innerHandler;

            if (Logging.IsEnabled) Logging.Associate(this, transportSettings, nameof(InternalClient));
            this.transportSettings = transportSettings;

            if (Logging.IsEnabled) Logging.Exit(this, transportSettings, pipelineBuilder, nameof(InternalClient) + "_ctor");
        }

        /// <summary> 
        /// Diagnostic sampling percentage value, [0-100];  
        /// 0 means no message will carry on diag info 
        /// </summary>
        public int DiagnosticSamplingPercentage
        {
            get { return _diagnosticSamplingPercentage; }
            set
            {
                if (value > 100 || value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(DiagnosticSamplingPercentage), DiagnosticSamplingPercentage,
                        "The range of diagnostic sampling percentage should between [0,100].");
                }

                if (IsE2EDiagnosticSupportedProtocol())
                {
                    _diagnosticSamplingPercentage = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the timeout used in the operation retries.
        /// </summary>
        // Codes_SRS_DEVICECLIENT_28_002: [This property shall be defaulted to 240000 (4 minutes).]
        public uint OperationTimeoutInMilliseconds
        {
            get
            {
                return _operationTimeoutInMilliseconds;
            }

            set
            {
                _operationTimeoutInMilliseconds = value;
                var retryDelegatingHandler = GetDelegateHandler<RetryDelegatingHandler>();
                if (retryDelegatingHandler != null)
                {
                    retryDelegatingHandler.RetryTimeoutInMilliseconds = value;
                }
            }
        }

        internal X509Certificate2 Certificate { get; set; }

        internal IDelegatingHandler InnerHandler { get; set; }

        internal Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            return InnerHandler.SendMethodResponseAsync(methodResponse, cancellationToken);
        }

        internal IotHubConnectionString IotHubConnectionString { get; private set; } = null;

        /// <summary>
        /// Stores custom product information that will be appended to the user agent string that is sent to IoT Hub.
        /// </summary>
        public string ProductInfo
        {
            // We store InternalClient.ProductInfo as a string property of an object (rather than directly as a string)
            // so that updates will propagate down to the transport layer throughout the lifetime of the InternalClient
            // object instance.
            get => productInfo.Extra;
            set => productInfo.Extra = value;
        }

        /// <summary>
        /// Stores the retry strategy used in the operation retries.
        /// </summary>
        // Codes_SRS_DEVICECLIENT_28_001: [This property shall be defaulted to the exponential retry strategy with backoff 
        // parameters for calculating delay in between retries.]
        [Obsolete("This method has been deprecated.  Please use Microsoft.Azure.Devices.Client.SetRetryPolicy(IRetryPolicy retryPolicy) instead.")]
        public RetryPolicyType RetryPolicy { get; set; }

        /// <summary>
        /// Sets the retry policy used in the operation retries.
        /// </summary>
        /// <param name="retryPolicy">The retry policy. The default is new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));</param>
        // Codes_SRS_DEVICECLIENT_28_001: [This property shall be defaulted to the exponential retry strategy with backoff 
        // parameters for calculating delay in between retries.]
        public void SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            var retryDelegatingHandler = GetDelegateHandler<RetryDelegatingHandler>();
            if (retryDelegatingHandler == null)
            {
                throw new NotSupportedException();
            }

            retryDelegatingHandler.SetRetryPolicy(retryPolicy);
        }

        private T GetDelegateHandler<T>() where T : DefaultDelegatingHandler
        {
            var handler = this.InnerHandler as DefaultDelegatingHandler;
            bool isFound = false;

            while (!isFound || handler == null)
            {
                if (handler is T)
                {
                    isFound = true;
                }
                else
                {
                    handler = handler.InnerHandler as DefaultDelegatingHandler;
                }
            }

            if (!isFound)
            {
                return default(T);
            }

            return (T)handler;
        }

        /// <summary>
        /// Explicitly open the InternalClient instance.
        /// </summary>
        public async Task OpenAsync()
        {
            try
            {
                // Codes_SRS_DEVICECLIENT_28_007: [ The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable(authentication, quota exceed) error occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await OpenAsync(cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Explicitly open the InternalClient instance.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        public Task OpenAsync(CancellationToken cancellationToken)
        {
            return this.InnerHandler.OpenAsync(cancellationToken);
        }

        /// <summary>
        /// Close the InternalClient instance
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {
            try
            {
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await CloseAsync(cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Close the InternalClient instance
        /// </summary>
        /// <returns></returns>
        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return InnerHandler.CloseAsync(cancellationToken);
        }

        /// <summary>
        /// Receive a message from the device queue using the default timeout.
        /// </summary>
        /// <returns>The receive message or null if there was no message until the default timeout</returns>
        public async Task<Message> ReceiveAsync()
        {
            try
            {
                // Codes_SRS_DEVICECLIENT_28_011: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable(authentication, quota exceed) error occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    return await ReceiveAsync(cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Receive a message from the device queue with the specified timeout
        /// </summary>
        /// <returns>The receive message or null if there was no message until the specified time has elapsed</returns>
        public async Task<Message> ReceiveAsync(TimeSpan timeout)
        {
            if (timeout == TimeSpan.MaxValue) return await ReceiveAsync(CancellationToken.None).ConfigureAwait(false);
            using (var cts = new CancellationTokenSource(timeout))
            {
                try
                {
                    return await ReceiveAsync(cts.Token).ConfigureAwait(false);
                }
                catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Receive a message from the device queue using the default timeout.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The receive message or null if there was no message until the default timeout</returns>
        public Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            // Codes_SRS_DEVICECLIENT_28_011: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable(authentication, quota exceed) error occurs.]
            return InnerHandler.ReceiveAsync(cancellationToken);
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public async Task CompleteAsync(string lockToken)
        {
            try
            {
                // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await CompleteAsync(lockToken, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            if (string.IsNullOrEmpty(lockToken))
            {
                throw new ArgumentNullException(nameof(lockToken));
            }

            return InnerHandler.CompleteAsync(lockToken, cancellationToken);
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task CompleteAsync(Message message)
        {
            // Codes_SRS_DEVICECLIENT_28_015: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            return CompleteAsync(message?.LockToken);
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The previously received message</returns>
        public Task CompleteAsync(Message message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            // Codes_SRS_DEVICECLIENT_28_015: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            return CompleteAsync(message.LockToken, cancellationToken);
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public async Task AbandonAsync(string lockToken)
        {
            try
            {
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await AbandonAsync(lockToken, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
            // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(lockToken))
            {
                throw new ArgumentNullException(nameof(lockToken));
            }

            // Codes_SRS_DEVICECLIENT_28_015: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            return InnerHandler.AbandonAsync(lockToken, cancellationToken);
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task AbandonAsync(Message message)
        {
            return AbandonAsync(message.LockToken);
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task AbandonAsync(Message message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return AbandonAsync(message.LockToken, cancellationToken);
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The previously received message</returns>
        public async Task RejectAsync(string lockToken)
        {
            try
            {
                // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await RejectAsync(lockToken, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(lockToken))
            {
                throw new ArgumentNullException(nameof(lockToken));
            }

            return InnerHandler.RejectAsync(lockToken, cancellationToken);
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task RejectAsync(Message message)
        {
            return RejectAsync(message.LockToken);
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task RejectAsync(Message message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return RejectAsync(message.LockToken, cancellationToken);
        }

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <returns>The message containing the event</returns>
        public async Task SendEventAsync(Message message)
        {
            try
            {
            // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            using (CancellationTokenSource cts = CancellationTokenSourceFactory())
            {
                await SendEventAsync(message, cts.Token).ConfigureAwait(false);
            }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, _diagnosticSamplingPercentage, ref _currentMessageCount);
            // Codes_SRS_DEVICECLIENT_28_019: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication or quota exceed) occurs.]
            return InnerHandler.SendEventAsync(message, cancellationToken);
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <returns>The task containing the event</returns>
        public async Task SendEventBatchAsync(IEnumerable<Message> messages)
        {
            try
            {
                // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await SendEventBatchAsync(messages, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            // Codes_SRS_DEVICECLIENT_28_019: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication or quota exceed) occurs.]
            return InnerHandler.SendEventAsync(messages, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a block blob in a storage account associated with the IoTHub for that device.
        /// If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="blobName"></param>
        /// <param name="source"></param>
        /// <returns>AsncTask</returns>
        public Task UploadToBlobAsync(String blobName, Stream source)
        {
            return UploadToBlobAsync(blobName, source, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a stream to a block blob in a storage account associated with the IoTHub for that device.
        /// If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="blobName"></param>
        /// <param name="source"></param>
        /// <returns>AsncTask</returns>
        public Task UploadToBlobAsync(String blobName, Stream source, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, blobName, source, nameof(UploadToBlobAsync));

                if (String.IsNullOrEmpty(blobName))
                {
                    throw Fx.Exception.ArgumentNull("blobName");
                }
                if (source == null)
                {
                    throw Fx.Exception.ArgumentNull("source");
                }
                if (blobName.Length > 1024)
                {
                    throw Fx.Exception.Argument("blobName", "Length cannot exceed 1024 characters");
                }
                if (blobName.Split('/').Count() > 254)
                {
                    throw Fx.Exception.Argument("blobName", "Path segment count cannot exceed 254");
                }

                HttpTransportHandler httpTransport = null;
                var context = new PipelineContext();
                context.Set(this.productInfo);

                var transportSettings = new Http1TransportSettings();

                //We need to add the certificate to the fileUpload httpTransport if DeviceAuthenticationWithX509Certificate
                if (this.Certificate != null)
                {
                    transportSettings.ClientCertificate = this.Certificate;
                }

                httpTransport = new HttpTransportHandler(context, IotHubConnectionString, transportSettings);

                return httpTransport.UploadToBlobAsync(blobName, source, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, blobName, nameof(UploadToBlobAsync));
            }
        }

        /// <summary>
        /// Registers a new delegate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>
        public async Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext)
        {
            try
            {
                // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await SetMethodHandlerAsync(methodName, methodHandler, userContext, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Registers a new delegate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>
        public async Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, methodName, methodHandler, userContext, nameof(SetMethodHandlerAsync));
                await methodsDictionarySemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (methodHandler != null)
                {
                    // codes_SRS_DEVICECLIENT_10_005: [ It shall EnableMethodsAsync when called for the first time. ]
                    await this.EnableMethodAsync(cancellationToken).ConfigureAwait(false);

                    // codes_SRS_DEVICECLIENT_10_001: [ It shall lazy-initialize the deviceMethods property. ]
                    if (this.deviceMethods == null)
                    {
                        this.deviceMethods = new Dictionary<string, Tuple<MethodCallback, object>>();
                    }
                    this.deviceMethods[methodName] = new Tuple<MethodCallback, object>(methodHandler, userContext);
                }
                else
                {
                    // codes_SRS_DEVICECLIENT_10_002: [ If the given methodName already has an associated delegate, the existing delegate shall be removed. ]
                    // codes_SRS_DEVICECLIENT_10_003: [ The given delegate will only be added if it is not null. ]
                    if (this.deviceMethods != null)
                    {
                        this.deviceMethods.Remove(methodName);

                        if (this.deviceMethods.Count == 0)
                        {
                            // codes_SRS_DEVICECLIENT_10_004: [ The deviceMethods property shall be deleted if the last delegate has been removed. ]
                            this.deviceMethods = null;
                        }

                        // codes_SRS_DEVICECLIENT_10_006: [ It shall DisableMethodsAsync when the last delegate has been removed. ]
                        await this.DisableMethodAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                methodsDictionarySemaphore.Release();
                if (Logging.IsEnabled) Logging.Exit(this, methodName, methodHandler, userContext, nameof(SetMethodHandlerAsync));
            }
        }

        /// <summary>
        /// Registers a new delegate that is called for a method that doesn't have a delegate registered for its name. 
        /// If a default delegate is already registered it will replace with the new delegate.
        /// </summary>
        /// <param name="methodHandler">The delegate to be used when a method is called by the cloud service and there is no delegate registered for that method name.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        public async Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler, object userContext)
        {
            try
            {
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await SetMethodDefaultHandlerAsync(methodHandler, userContext, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
            // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
        }

        /// <summary>
        /// Registers a new delegate that is called for a method that doesn't have a delegate registered for its name. 
        /// If a default delegate is already registered it will replace with the new delegate.
        /// </summary>
        /// <param name="methodHandler">The delegate to be used when a method is called by the cloud service and there is no delegate registered for that method name.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        public async Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler, object userContext, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, methodHandler, userContext, nameof(SetMethodDefaultHandlerAsync));

                await methodsDictionarySemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (methodHandler != null)
                {
                    // codes_SRS_DEVICECLIENT_10_005: [ It shall EnableMethodsAsync when called for the first time. ]
                    await this.EnableMethodAsync(cancellationToken).ConfigureAwait(false);

                    // codes_SRS_DEVICECLIENT_24_001: [ If the default callback has already been set, it is replaced with the new callback. ]
                    this.deviceDefaultMethodCallback = new Tuple<MethodCallback, object>(methodHandler, userContext);
                }
                else
                {
                    this.deviceDefaultMethodCallback = null;

                    // codes_SRS_DEVICECLIENT_10_006: [ It shall DisableMethodsAsync when the last delegate has been removed. ]
                    await this.DisableMethodAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                methodsDictionarySemaphore.Release();
                if (Logging.IsEnabled) Logging.Exit(this, methodHandler, userContext, nameof(SetMethodDefaultHandlerAsync));
            }
        }

        /// <summary>
        /// Registers a new delegate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>

        [Obsolete("Please use SetMethodHandlerAsync.")]
        public void SetMethodHandler(string methodName, MethodCallback methodHandler, object userContext)
        {
            if (Logging.IsEnabled) Logging.Enter(this, methodName, methodHandler, userContext, nameof(SetMethodHandler));
            try
            {
                // Dangerous: can cause deadlocks.
                SetMethodHandlerAsync(methodName, methodHandler, userContext).Wait();
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, methodName, methodHandler, userContext, nameof(SetMethodHandler));
            }
        }

        #region DEVICE STREAMING
        public Task<DeviceStreamRequest> WaitForDeviceStreamRequestAsync()
        {
            try
            {
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    return WaitForDeviceStreamRequestAsync(cts.Token);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        public async Task<DeviceStreamRequest> WaitForDeviceStreamRequestAsync(CancellationToken cancellationToken)
        {
            DeviceStreamRequest result;

            try
            {
                await deviceStreamsSemaphore.WaitAsync().ConfigureAwait(false);

                await this.EnableStreamAsync(cancellationToken).ConfigureAwait(false);

                result = await this.InnerHandler.WaitForDeviceStreamRequestAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                deviceStreamsSemaphore.Release();
            }

            return result;
        }

        public async Task AcceptDeviceStreamRequestAsync(DeviceStreamRequest request)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(_defaultDeviceStreamingTimeoutSecs)))
            {
                await AcceptDeviceStreamRequestAsync(request, cts.Token).ConfigureAwait(false);
            }
        }

        public async Task AcceptDeviceStreamRequestAsync(DeviceStreamRequest request, CancellationToken cancellationToken)
        {
            await this.InnerHandler.AcceptDeviceStreamRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task RejectDeviceStreamRequestAsync(DeviceStreamRequest request)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(_defaultDeviceStreamingTimeoutSecs)))
            {
                await this.InnerHandler.RejectDeviceStreamRequestAsync(request, cts.Token).ConfigureAwait(false);
            }
        }

        public async Task RejectDeviceStreamRequestAsync(DeviceStreamRequest request, CancellationToken cancellationToken)
        {
            await this.InnerHandler.RejectDeviceStreamRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private async Task EnableStreamAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await this.InnerHandler.EnableStreamsAsync(cancellationToken).ConfigureAwait(false);
        }
#endregion DEVICE STREAMING

        /// <summary>
        /// Registers a new delegate for the connection status changed callback. If a delegate is already associated, 
        /// it will be replaced with the new delegate.
        /// <param name="statusChangesHandler">The name of the method to associate with the delegate.</param>
        /// </summary>
        public void SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler statusChangesHandler)
        {
            // codes_SRS_DEVICECLIENT_28_025: [** `SetConnectionStatusChangesHandler` shall set connectionStatusChangesHandler **]**
            // codes_SRS_DEVICECLIENT_28_026: [** `SetConnectionStatusChangesHandler` shall unset connectionStatusChangesHandler if `statusChangesHandler` is null **]**
            if (Logging.IsEnabled) Logging.Info(this, statusChangesHandler, nameof(SetConnectionStatusChangesHandler));
            this.connectionStatusChangesHandler = statusChangesHandler;
        }

        /// <summary>
        /// The delegate for handling disrupted connection/links in the transport layer.
        /// </summary>
        internal void OnConnectionStatusChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, status, reason, nameof(OnConnectionStatusChanged));

                if (connectionStatusChangesHandler != null &&
                    (lastConnectionStatus != status || lastConnectionStatusChangeReason != reason))
                {
                    this.connectionStatusChangesHandler(status, reason);
                }
            }
            finally
            {
                lastConnectionStatus = status;
                lastConnectionStatusChangeReason = reason;
                if (Logging.IsEnabled) Logging.Exit(this, status, reason, nameof(OnConnectionStatusChanged));
            }
        }

        /// <summary>
        /// The delegate for handling direct methods received from service.
        /// </summary>
        internal async Task OnMethodCalled(MethodRequestInternal methodRequestInternal)
        {
            Tuple<MethodCallback, object> m = null;

            if (Logging.IsEnabled) Logging.Enter(this, methodRequestInternal?.Name, methodRequestInternal, nameof(OnMethodCalled));

            // codes_SRS_DEVICECLIENT_10_012: [ If the given methodRequestInternal argument is null, fail silently ]
            if (methodRequestInternal == null)
            {
                return;
            }

            MethodResponseInternal methodResponseInternal;
            byte[] requestData = methodRequestInternal.GetBytes();

            await methodsDictionarySemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                Utils.ValidateDataIsEmptyOrJson(requestData);
                this.deviceMethods?.TryGetValue(methodRequestInternal.Name, out m);
                if (m == null)
                {
                    m = this.deviceDefaultMethodCallback;
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                if (Logging.IsEnabled) Logging.Error(this, ex, nameof(OnMethodCalled));

                // codes_SRS_DEVICECLIENT_28_020: [ If the given methodRequestInternal data is not valid json, respond with status code 400 (BAD REQUEST) ]
                methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, (int)MethodResposeStatusCode.BadRequest);

                await this.SendMethodResponseAsync(methodResponseInternal, methodRequestInternal.CancellationToken).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Error(this, ex, nameof(OnMethodCalled));
                return;
            }
            finally
            {
                methodsDictionarySemaphore.Release();
            }

            if (m == null)
            {
                // codes_SRS_DEVICECLIENT_10_013: [ If the given method does not have an associated delegate and no default delegate was registered, respond with status code 501 (METHOD NOT IMPLEMENTED) ]
                methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, (int)MethodResposeStatusCode.MethodNotImplemented);
            }
            else
            {
                try
                {
                    // codes_SRS_DEVICECLIENT_10_011: [ The OnMethodCalled shall invoke the specified delegate. ]
                    // codes_SRS_DEVICECLIENT_24_002: [ The OnMethodCalled shall invoke the default delegate if there is no specified delegate for that method. ]
                    MethodResponse rv = await m.Item1(new MethodRequest(methodRequestInternal.Name, requestData), m.Item2).ConfigureAwait(false);

                    // codes_SRS_DEVICECLIENT_03_012: [If the MethodResponse does not contain result, the MethodResponseInternal constructor shall be invoked with no results.]
                    if (rv.Result == null)
                    {
                        methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, rv.Status);
                    }
                    // codes_SRS_DEVICECLIENT_03_013: [Otherwise, the MethodResponseInternal constructor shall be invoked with the result supplied.]
                    else
                    {
                        methodResponseInternal = new MethodResponseInternal(rv.Result, methodRequestInternal.RequestId, rv.Status);
                    }
                }
                catch (Exception ex)
                {
                    if (Logging.IsEnabled) Logging.Error(this, ex, nameof(OnMethodCalled));

                    // codes_SRS_DEVICECLIENT_28_021: [ If the MethodResponse from the MethodHandler is not valid json, respond with status code 500 (USER CODE EXCEPTION) ]
                    methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, (int)MethodResposeStatusCode.UserCodeException);
                }
            }

            await this.SendMethodResponseAsync(methodResponseInternal, methodRequestInternal.CancellationToken).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, methodRequestInternal.Name, methodRequestInternal, nameof(OnMethodCalled));
        }

        public void Dispose()
        {
            this.InnerHandler?.Dispose();
        }

        /// <summary>
        /// Set a callback that will be called whenever the client receives a state update 
        /// (desired or reported) from the service.  This has the side-effect of subscribing
        /// to the PATCH topic on the service.
        /// </summary>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        [Obsolete("Please use SetDesiredPropertyUpdateCallbackAsync.")]
        public Task SetDesiredPropertyUpdateCallback(DesiredPropertyUpdateCallback callback, object userContext)
        {
            // Obsoleted due to incorrect naming:
            return SetDesiredPropertyUpdateCallbackAsync(callback, userContext);
        }

        /// <summary>
        /// Set a callback that will be called whenever the client receives a state update 
        /// (desired or reported) from the service.  This has the side-effect of subscribing
        /// to the PATCH topic on the service.
        /// </summary>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        public async Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext)
        {
            try
            {
                // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await SetDesiredPropertyUpdateCallbackAsync(callback, userContext, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Set a callback that will be called whenever the client receives a state update 
        /// (desired or reported) from the service.  This has the side-effect of subscribing
        /// to the PATCH topic on the service.
        /// </summary>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        public async Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext, CancellationToken cancellationToken)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            // Codes_SRS_DEVICECLIENT_18_003: `SetDesiredPropertyUpdateCallbackAsync` shall call the transport to register for PATCHes on it's first call.
            // Codes_SRS_DEVICECLIENT_18_004: `SetDesiredPropertyUpdateCallbackAsync` shall not call the transport to register for PATCHes on subsequent calls
            if (!this.patchSubscribedWithService)
            {
                await InnerHandler.EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                patchSubscribedWithService = true;
            }

            this.desiredPropertyUpdateCallback = callback;
            this.twinPatchCallbackContext = userContext;
        }

        /// <summary>
        /// Retrieve a device twin object for the current device.
        /// </summary>
        /// <returns>The device twin object for the current device</returns>
        public async Task<Twin> GetTwinAsync()
        {
            try
            {
                // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    return await GetTwinAsync(cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Retrieve a device twin object for the current device.
        /// </summary>
        /// <returns>The device twin object for the current device</returns>
        public Task<Twin> GetTwinAsync(CancellationToken cancellationToken)
        {
            // Codes_SRS_DEVICECLIENT_18_001: `GetTwinAsync` shall call `SendTwinGetAsync` on the transport to get the twin state
            return InnerHandler.SendTwinGetAsync(cancellationToken);
        }
        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        public async Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties)
        {
            try
            {
                // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await UpdateReportedPropertiesAsync(reportedProperties, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            // Codes_SRS_DEVICECLIENT_18_006: `UpdateReportedPropertiesAsync` shall throw an `ArgumentNull` exception if `reportedProperties` is null
            if (reportedProperties == null)
            {
                throw new ArgumentNullException(nameof(reportedProperties));
            }

            // Codes_SRS_DEVICECLIENT_18_002: `UpdateReportedPropertiesAsync` shall call `SendTwinPatchAsync` on the transport to update the reported properties
            return InnerHandler.SendTwinPatchAsync(reportedProperties, cancellationToken);
        }

        //  Codes_SRS_DEVICECLIENT_18_005: When a patch is received from the service, the `callback` shall be called.
        internal void OnReportedStatePatchReceived(TwinCollection patch)
        {
            if (Logging.IsEnabled) Logging.Info(this, patch.ToJson(), nameof(OnReportedStatePatchReceived));
            this.desiredPropertyUpdateCallback(patch, this.twinPatchCallbackContext);
        }

        private Task EnableMethodAsync(CancellationToken cancellationToken)
        {
            if (this.deviceMethods == null && this.deviceDefaultMethodCallback == null)
            {
                return InnerHandler.EnableMethodsAsync(cancellationToken);
            }

            return TaskHelpers.CompletedTask;
        }

        private Task DisableMethodAsync(CancellationToken cancellationToken)
        {
            if (this.deviceMethods == null && this.deviceDefaultMethodCallback == null)
            {
                return InnerHandler.DisableMethodsAsync(cancellationToken);
            }

            return TaskHelpers.CompletedTask;
        }

        internal bool IsE2EDiagnosticSupportedProtocol()
        {
            foreach (ITransportSettings transportSetting in this.transportSettings)
            {
                var transportType = transportSetting.GetTransportType();
                if (!(transportType == TransportType.Amqp_WebSocket_Only || transportType == TransportType.Amqp_Tcp_Only
                    || transportType == TransportType.Mqtt_WebSocket_Only || transportType == TransportType.Mqtt_Tcp_Only))
                {
                    throw new NotSupportedException($"{transportType} protocol doesn't support E2E diagnostic.");
                }
            }
            return true;
        }

        private CancellationTokenSource CancellationTokenSourceFactory()
        {
            CancellationTokenSource cts;
            if (OperationTimeoutInMilliseconds == 0)
            {
                cts = new CancellationTokenSource();
            }
            else
            {
                cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(OperationTimeoutInMilliseconds));
            }

            return cts;
        }

        #region Module Specific API
        /// <summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="message">The message to send</param>
        /// <returns>The message containing the event</returns>
        public async Task SendEventAsync(string outputName, Message message)
        {
            try
            {
                // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await SendEventAsync(outputName, message, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="message">The message to send</param>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(string outputName, Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, outputName, message, nameof(SendEventAsync));

                ValidateModuleTransportHandler("SendEventAsync for a named output");

                // Codes_SRS_DEVICECLIENT_10_012: [If `outputName` is `null` or empty, an `ArgumentNullException` shall be thrown.]
                if (string.IsNullOrWhiteSpace(outputName))
                {
                    throw new ArgumentNullException(nameof(outputName));
                }

                // Codes_SRS_DEVICECLIENT_10_013: [If `message` is `null` or empty, an `ArgumentNullException` shall be thrown.]
                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                // Codes_SRS_DEVICECLIENT_10_015: [The `output` property of a given `message` shall be assigned the value `outputName` before submitting each request to the transport layer.]
                message.SystemProperties.Add(MessageSystemPropertyNames.OutputName, outputName);

                // Codes_SRS_DEVICECLIENT_10_011: [The `SendEventAsync` operation shall retry sending `message` until the `BaseClient::RetryStrategy` timespan expires or unrecoverable error(authentication or quota exceed) occurs.]
                return InnerHandler.SendEventAsync(message, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, outputName, message, nameof(SendEventAsync));
            }
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="messages">A list of one or more messages to send</param>
        /// </summary>
        /// <returns>The task containing the event</returns>
        public async Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages)
        {
            try
            {
                // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await SendEventBatchAsync(outputName, messages, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="messages">A list of one or more messages to send</param>
        /// </summary>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, outputName, messages, nameof(SendEventBatchAsync));

                ValidateModuleTransportHandler("SendEventBatchAsync for a named output");

                // Codes_SRS_DEVICECLIENT_10_012: [If `outputName` is `null` or empty, an `ArgumentNullException` shall be thrown.]
                if (string.IsNullOrWhiteSpace(outputName))
                {
                    throw new ArgumentNullException(nameof(outputName));
                }

                List<Message> messagesList = messages?.ToList();
                // Codes_SRS_DEVICECLIENT_10_013: [If `message` is `null` or empty, an `ArgumentNullException` shall be thrown]
                if (messagesList == null || messagesList.Count == 0)
                {
                    throw new ArgumentNullException(nameof(messages));
                }

                // Codes_SRS_DEVICECLIENT_10_015: [The `module-output` property of a given `message` shall be assigned the value `outputName` before submitting each request to the transport layer.]
                messagesList.ForEach(m => m.SystemProperties.Add(MessageSystemPropertyNames.OutputName, outputName));

                // Codes_SRS_DEVICECLIENT_10_014: [The `SendEventBachAsync` operation shall retry sending `messages` until the `BaseClient::RetryStrategy` timespan expires or unrecoverable error(authentication or quota exceed) occurs.]
                return InnerHandler.SendEventAsync(messagesList, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, outputName, messages, nameof(SendEventBatchAsync));
            }
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
                // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await SetInputMessageHandlerAsync(inputName, messageHandler, userContext, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Registers a new delgate for the particular input. If a delegate is already associated with
        /// the input, it will be replaced with the new delegate.
        /// <param name="inputName">The name of the input to associate with the delegate.</param>
        /// <param name="messageHandler">The delegate to be used when a message is sent to the particular inputName.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>
        /// <returns>The task containing the event</returns>
        public async Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler, object userContext, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, inputName, messageHandler, userContext, nameof(SetInputMessageHandlerAsync));

            ValidateModuleTransportHandler("SetInputMessageHandlerAsync for a named output");
            try
            {
                await this.receiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (messageHandler != null)
                {
                    // codes_SRS_DEVICECLIENT_33_003: [ It shall EnableEventReceiveAsync when called for the first time. ]
                    await this.EnableEventReceiveAsync(cancellationToken).ConfigureAwait(false);
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
                    await this.DisableEventReceiveAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                this.receiveSemaphore.Release();
                if (Logging.IsEnabled) Logging.Exit(this, inputName, messageHandler, userContext, nameof(SetInputMessageHandlerAsync));
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
                // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
                using (CancellationTokenSource cts = CancellationTokenSourceFactory())
                {
                    await SetMessageHandlerAsync(messageHandler, userContext, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
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
        public async Task SetMessageHandlerAsync(MessageHandler messageHandler, object userContext, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, messageHandler, userContext, nameof(SetMessageHandlerAsync));

            try
            {
                await this.receiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (messageHandler != null)
                {
                    // codes_SRS_DEVICECLIENT_33_003: [ It shall EnableEventReceiveAsync when called for the first time. ]
                    await this.EnableEventReceiveAsync(cancellationToken).ConfigureAwait(false);
                    this.defaultEventCallback = new Tuple<MessageHandler, object>(messageHandler, userContext);
                }
                else
                {
                    this.defaultEventCallback = null;
                    // codes_SRS_DEVICECLIENT_33_004: [ It shall DisableEventReceiveAsync when the last delegate has been removed. ]
                    await this.DisableEventReceiveAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                this.receiveSemaphore.Release();
                if (Logging.IsEnabled) Logging.Exit(this, messageHandler, userContext, nameof(SetMessageHandlerAsync));
            }
        }

        private Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            if (this.receiveEventEndpoints == null && this.defaultEventCallback == null)
            {
                return InnerHandler.EnableEventReceiveAsync(cancellationToken);
            }

            return TaskHelpers.CompletedTask;
        }

        private Task DisableEventReceiveAsync(CancellationToken cancellationToken)
        {
            if (this.receiveEventEndpoints == null && this.defaultEventCallback == null)
            {
                return InnerHandler.DisableEventReceiveAsync(cancellationToken);
            }

            return TaskHelpers.CompletedTask;
        }

        /// <summary>
        /// The delegate for handling event messages received
        /// <param name="input">The input on which a message is received</param>
        /// <param name="message">The message received</param>
        /// </summary>
        internal async Task OnReceiveEventMessageCalled(string input, Message message)
        {
            if (Logging.IsEnabled) Logging.Enter(this, input, message, nameof(OnReceiveEventMessageCalled));

            // codes_SRS_DEVICECLIENT_33_001: [ If the given eventMessageInternal argument is null, fail silently ]
            if (message == null) return;

            try
            {
                Tuple<MessageHandler, object> callback = null;
                await this.receiveSemaphore.WaitAsync().ConfigureAwait(false);
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
                MessageResponse response = await (callback?.Item1?.Invoke(message, callback.Item2) ?? Task.FromResult(MessageResponse.Completed)).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Info(this, $"{nameof(MessageResponse)} = {response}", nameof(OnReceiveEventMessageCalled));

                switch (response)
                {
                    case MessageResponse.Completed:
                        await this.CompleteAsync(message).ConfigureAwait(false);
                        break;
                    case MessageResponse.Abandoned:
                        await this.AbandonAsync(message).ConfigureAwait(false);
                        break;
                    default:
                        break;
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, input, message, nameof(OnReceiveEventMessageCalled));
            }
        }

        internal void ValidateModuleTransportHandler(string apiName)
        {
            if (string.IsNullOrEmpty(this.IotHubConnectionString.ModuleId))
            {
                throw new InvalidOperationException("{0} is available for Modules only.".FormatInvariant(apiName));
            }
        }

        #endregion Module Specific API
    }
}
