﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using Common;
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
             +---+inherits----------->  Delegating   <------inherits-----------------+
             |                       |  Handler      |                               |
             |           +--inherits->               <--inherits----+                |
             |           |           +-------^-------+              |                |
             |           |                   |inherits              |                |
             |           |                   |                      |                |
+------------+       +---+---------+      +--+----------+       +---+--------+       +--------------+
|            |       |             |      |             |       | Protocol   |       | <<abstract>> |
| GateKeeper |  use  | Retry       | use  |  Error      |  use  | Routing    |  use  | Transport    |
| Delegating +-------> Delegating  +------>  Delegating +-------> Delegating +-------> Delegating   |
| Handler    |       | Handler     |      |  Handler    |       | Handler    |       | Handler      |
|            |       |             |      |             |       |            |       |              |
| overrides: |       | overrides:  |      |  overrides  |       | overrides: |       | overrides:   |
|  Open      |       |  Open       |      |   Open      |       |  Open      |       |  Receive     |
|  Close     |       |  SendEvent  |      |   SendEvent |       |            |       |              |
|            |       |  SendEvents |      |   SendEvents|       +------------+       +--^--^---^----+
+------------+       |  Receive    |      |   Receive   |                               |  |   |
                     |  Reject     |      |   Reject    |                               |  |   |
                     |  Abandon    |      |   Abandon   |                               |  |   |
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
TODO: revisit DefaultDelegatingHandler - it seems redundant as long as we have to many overloads in most of the classes.
*/

    /// <summary>
    /// Contains methods that a device can use to send messages to and receive from the service.
    /// </summary>
    internal class InternalClient : IDisposable
    {
        IotHubConnectionString iotHubConnectionString = null;
        int _diagnosticSamplingPercentage = 0;
        ITransportSettings[] transportSettings;
        SemaphoreSlim methodsDictionarySemaphore = new SemaphoreSlim(1, 1);
        readonly SemaphoreSlim receiveSemaphore = new SemaphoreSlim(1, 1);
        volatile Dictionary<string, Tuple<MessageHandler, object>> receiveEventEndpoints;
        volatile Tuple<MessageHandler, object> defaultEventCallback;
        DeviceClientConnectionStatusManager connectionStatusManager = new DeviceClientConnectionStatusManager();
        private ProductInfo productInfo = new ProductInfo();

        /// <summary>
        /// Stores Methods supported by the client device and their associated delegate.
        /// </summary>
        volatile Dictionary<string, Tuple<MethodCallback, object>> deviceMethods;

        volatile Tuple<MethodCallback, object> deviceDefaultMethodCallback;

        volatile ConnectionStatusChangesHandler connectionStatusChangesHandler;

        internal delegate Task OnMethodCalledDelegate(MethodRequestInternal methodRequestInternal);

        internal delegate Task OnConnectionClosedDelegate(object sender, ConnectionEventArgs e);

        internal delegate void OnConnectionOpenedDelegate(object sender, ConnectionEventArgs e);

        internal delegate Task OnReceiveEventMessageCalledDelegate(string input, Message message);

        /// <summary>
        /// Callback to call whenever the twin's desired state is updated by the service
        /// </summary>
        internal DesiredPropertyUpdateCallback desiredPropertyUpdateCallback;

        /// <summary>
        /// Has twin funcitonality been enabled with the service?
        /// </summary>
        Boolean patchSubscribedWithService = false;

        /// <summary>
        /// userContext passed when registering the twin patch callback
        /// </summary>
        Object twinPatchCallbackContext = null;

        private int _currentMessageCount = 0;

        public InternalClient(IotHubConnectionString iotHubConnectionString, ITransportSettings[] transportSettings, IDeviceClientPipelineBuilder pipelineBuilder)
        {
#if NET451
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
#endif

            this.iotHubConnectionString = iotHubConnectionString;

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

            if (Logging.IsEnabled) Logging.Associate(this, innerHandler, nameof(InternalClient));
            this.InnerHandler = innerHandler;
            
            if (Logging.IsEnabled) Logging.Associate(this, transportSettings, nameof(InternalClient));
            this.transportSettings = transportSettings;
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
        /// Stores the timeout used in the operation retries.
        /// </summary>
        // Codes_SRS_DEVICECLIENT_28_002: [This property shall be defaulted to 240000 (4 minutes).]
        public uint OperationTimeoutInMilliseconds { get; set; } = DeviceClient.DefaultOperationTimeoutInMilliseconds;

        internal X509Certificate2 Certificate { get; set; }

        internal IDelegatingHandler InnerHandler { get; set; }

        internal Task SendMethodResponseAsync(MethodResponseInternal methodResponse)
        {
            return ApplyTimeout(operationTimeoutCancellationToken =>
            {
                return this.InnerHandler.SendMethodResponseAsync(methodResponse, operationTimeoutCancellationToken);
            });
        }

        internal IotHubConnectionString IotHubConnectionString => this.iotHubConnectionString;

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

        CancellationTokenSource GetOperationTimeoutCancellationTokenSource()
        {
            return new CancellationTokenSource(TimeSpan.FromMilliseconds(OperationTimeoutInMilliseconds));
        }

        /// <summary>
        /// Explicitly open the InternalClient instance.
        /// </summary>

        public Task OpenAsync()
        {
            // Codes_SRS_DEVICECLIENT_28_007: [ The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable(authentication, quota exceed) error occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.OpenAsync(true, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Close the InternalClient instance
        /// </summary>
        /// <returns></returns>
        public Task CloseAsync()
        {
            return this.InnerHandler.CloseAsync();
        }

        /// <summary>
        /// Receive a message from the device queue using the default timeout.
        /// </summary>
        /// <returns>The receive message or null if there was no message until the default timeout</returns>
        public Task<Message> ReceiveAsync()
        {
            // Codes_SRS_DEVICECLIENT_28_011: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable(authentication, quota exceed) error occurs.]
            return ApplyTimeoutMessage(operationTimeoutCancellationToken => this.InnerHandler.ReceiveAsync(operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Receive a message from the device queue with the specified timeout
        /// </summary>
        /// <returns>The receive message or null if there was no message until the specified time has elapsed</returns>
        public Task<Message> ReceiveAsync(TimeSpan timeout)
        {
            // Codes_SRS_DEVICECLIENT_28_011: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable(authentication, quota exceed) error occurs.]
            return ApplyTimeoutMessage(operationTimeoutCancellationToken => this.InnerHandler.ReceiveAsync(timeout, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task CompleteAsync(string lockToken)
        {
            if (string.IsNullOrEmpty(lockToken))
            {
                throw Fx.Exception.ArgumentNull("lockToken");
            }

            // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.CompleteAsync(lockToken, operationTimeoutCancellationToken.Token));
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task CompleteAsync(Message message)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull("message");
            }
            // Codes_SRS_DEVICECLIENT_28_015: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            return this.CompleteAsync(message.LockToken);
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task AbandonAsync(string lockToken)
        {
            if (string.IsNullOrEmpty(lockToken))
            {
                throw Fx.Exception.ArgumentNull("lockToken");
            }
            // Codes_SRS_DEVICECLIENT_28_015: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.AbandonAsync(lockToken, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task AbandonAsync(Message message)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull("message");
            }

            return this.AbandonAsync(message.LockToken);
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task RejectAsync(string lockToken)
        {
            if (string.IsNullOrEmpty(lockToken))
            {
                throw Fx.Exception.ArgumentNull("lockToken");
            }

            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.RejectAsync(lockToken, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task RejectAsync(Message message)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull("message");
            }
            // Codes_SRS_DEVICECLIENT_28_017: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.RejectAsync(message.LockToken, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(Message message)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull("message");
            }

            IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, _diagnosticSamplingPercentage, ref _currentMessageCount);
            // Codes_SRS_DEVICECLIENT_28_019: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication or quota exceed) occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.SendEventAsync(message, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(IEnumerable<Message> messages)
        {
            if (messages == null)
            {
                throw Fx.Exception.ArgumentNull("messages");
            }

            // Codes_SRS_DEVICECLIENT_28_019: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication or quota exceed) occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.SendEventAsync(messages, operationTimeoutCancellationToken));
        }

        Task ApplyTimeout(Func<CancellationTokenSource, Task> operation)
        {
            if (OperationTimeoutInMilliseconds == 0)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                return operation(cancellationTokenSource)
                    .WithTimeout(TimeSpan.MaxValue, () => Resources.OperationTimeoutExpired, cancellationTokenSource.Token);
            }

            CancellationTokenSource operationTimeoutCancellationTokenSource = GetOperationTimeoutCancellationTokenSource();

            var result = operation(operationTimeoutCancellationTokenSource)
                .WithTimeout(TimeSpan.FromMilliseconds(OperationTimeoutInMilliseconds), () => Resources.OperationTimeoutExpired, operationTimeoutCancellationTokenSource.Token);

            return result.ContinueWith(t =>
            {

                // operationTimeoutCancellationTokenSource will be disposed by GC. 
                // Cannot dispose here since we don't know if both tasks created by WithTimeout ran to completion.
                if (t.IsCanceled)
                {
                    throw new TimeoutException(Resources.OperationTimeoutExpired);
                }

                if (t.IsFaulted)
                {
                    throw t.Exception.InnerException;
                }
            });
        }

        Task ApplyTimeout(Func<CancellationToken, Task> operation)
        {
            if (OperationTimeoutInMilliseconds == 0)
            {
                return operation(CancellationToken.None)
                    .WithTimeout(TimeSpan.MaxValue, () => Resources.OperationTimeoutExpired, CancellationToken.None);
            }

            CancellationTokenSource operationTimeoutCancellationTokenSource = GetOperationTimeoutCancellationTokenSource();

            if (Logging.IsEnabled)
            {
                Logging.Info(this, $"{operationTimeoutCancellationTokenSource.GetHashCode()}", nameof(ApplyTimeout));
            }

            var result = operation(operationTimeoutCancellationTokenSource.Token)
                .WithTimeout(TimeSpan.FromMilliseconds(OperationTimeoutInMilliseconds), () => Resources.OperationTimeoutExpired, operationTimeoutCancellationTokenSource.Token);

            return result.ContinueWith(t =>
            {
                // operationTimeoutCancellationTokenSource will be disposed by GC. 
                // Cannot dispose here since we don't know if both tasks created by WithTimeout ran to completion.
                if (t.IsFaulted)
                {
                    throw t.Exception.InnerException;
                }
            }, TaskContinuationOptions.NotOnCanceled);
        }

        Task<Message> ApplyTimeoutMessage(Func<CancellationToken, Task<Message>> operation)
        {
            if (OperationTimeoutInMilliseconds == 0)
            {
                return operation(CancellationToken.None)
                    .WithTimeout(TimeSpan.MaxValue, () => Resources.OperationTimeoutExpired, CancellationToken.None);
            }

            CancellationTokenSource operationTimeoutCancellationTokenSource = GetOperationTimeoutCancellationTokenSource();

            var result = operation(operationTimeoutCancellationTokenSource.Token)
                .WithTimeout(TimeSpan.FromMilliseconds(OperationTimeoutInMilliseconds), () => Resources.OperationTimeoutExpired, operationTimeoutCancellationTokenSource.Token);

            return result.ContinueWith(t =>
            {
                // operationTimeoutCancellationTokenSource will be disposed by GC. 
                // Cannot dispose here since we don't know if both tasks created by WithTimeout ran to completion.
                if (t.IsFaulted)
                {
                    throw t.Exception.InnerException;
                }
                return t.Result;
            });
        }

        Task<Twin> ApplyTimeoutTwin(Func<CancellationToken, Task<Twin>> operation)
        {
            if (OperationTimeoutInMilliseconds == 0)
            {
                return operation(CancellationToken.None)
                    .WithTimeout(TimeSpan.MaxValue, () => Resources.OperationTimeoutExpired, CancellationToken.None);
            }

            CancellationTokenSource operationTimeoutCancellationTokenSource = GetOperationTimeoutCancellationTokenSource();

            var result = operation(operationTimeoutCancellationTokenSource.Token)
                .WithTimeout(TimeSpan.FromMilliseconds(OperationTimeoutInMilliseconds), () => Resources.OperationTimeoutExpired, operationTimeoutCancellationTokenSource.Token);

            return result.ContinueWith(t =>
            {
                // operationTimeoutCancellationTokenSource will be disposed by GC. 
                // Cannot dispose here since we don't know if both tasks created by WithTimeout ran to completion.
                if (t.IsFaulted)
                {
                    throw t.Exception.InnerException;
                }
                return t.Result;
            });
        }

        /// <summary>
        /// Uploads a stream to a block blob in a storage account associated with the IoTHub for that device.
        /// If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="blobName"></param>
        /// <param name="source"></param>
        /// <returns>AsncTask</returns>
        public Task UploadToBlobAsync(String blobName, System.IO.Stream source)
        {
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

            httpTransport = new HttpTransportHandler(context, iotHubConnectionString, transportSettings);

            return httpTransport.UploadToBlobAsync(blobName, source);
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
                await methodsDictionarySemaphore.WaitAsync().ConfigureAwait(false);

                if (methodHandler != null)
                {
                    // codes_SRS_DEVICECLIENT_10_005: [ It shall EnableMethodsAsync when called for the first time. ]
                    await this.EnableMethodAsync().ConfigureAwait(false);

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
                        await this.DisableMethodAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                methodsDictionarySemaphore.Release();
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
                await methodsDictionarySemaphore.WaitAsync().ConfigureAwait(false);
                if (methodHandler != null)
                {
                    // codes_SRS_DEVICECLIENT_10_005: [ It shall EnableMethodsAsync when called for the first time. ]
                    await this.EnableMethodAsync().ConfigureAwait(false);

                    // codes_SRS_DEVICECLIENT_24_001: [ If the default callback has already been set, it is replaced with the new callback. ]
                    this.deviceDefaultMethodCallback = new Tuple<MethodCallback, object>(methodHandler, userContext);
                }
                else
                {
                    this.deviceDefaultMethodCallback = null;

                    // codes_SRS_DEVICECLIENT_10_006: [ It shall DisableMethodsAsync when the last delegate has been removed. ]
                    await this.DisableMethodAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                methodsDictionarySemaphore.Release();
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
            methodsDictionarySemaphore.Wait();

            try
            {
                if (methodHandler != null)
                {
                    // codes_SRS_DEVICECLIENT_10_001: [ It shall lazy-initialize the deviceMethods property. ]
                    if (this.deviceMethods == null)
                    {
                        this.deviceMethods = new Dictionary<string, Tuple<MethodCallback, object>>();

                        // codes_SRS_DEVICECLIENT_10_005: [ It shall EnableMethodsAsync when called for the first time. ]
                        ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.EnableMethodsAsync(operationTimeoutCancellationToken)).Wait();
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
                            // codes_SRS_DEVICECLIENT_10_006: [ It shall DisableMethodsAsync when the last delegate has been removed. ]
                            ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.DisableMethodsAsync(operationTimeoutCancellationToken)).Wait();

                            // codes_SRS_DEVICECLIENT_10_004: [ The deviceMethods property shall be deleted if the last delegate has been removed. ]
                            this.deviceMethods = null;
                        }
                    }
                }
            }
            finally
            {
                methodsDictionarySemaphore.Release();
            }
        }

        /// <summary>
        /// Registers a new delegate for the connection status changed callback. If a delegate is already associated, 
        /// it will be replaced with the new delegate.
        /// <param name="statusChangesHandler">The name of the method to associate with the delegate.</param>
        /// </summary>
        public void SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler statusChangesHandler)
        {
            // codes_SRS_DEVICECLIENT_28_025: [** `SetConnectionStatusChangesHandler` shall set connectionStatusChangesHandler **]**
            // codes_SRS_DEVICECLIENT_28_026: [** `SetConnectionStatusChangesHandler` shall unset connectionStatusChangesHandler if `statusChangesHandler` is null **]**
            this.connectionStatusChangesHandler = statusChangesHandler;
        }

        /// <summary>
        /// The delegate for handling disrupted connection/links in the transport layer.
        /// </summary>
        internal void OnConnectionOpened(object sender, ConnectionEventArgs e)
        {
            ConnectionStatusChangeResult result = this.connectionStatusManager.ChangeTo(e.ConnectionType, ConnectionStatus.Connected);
            if (result.IsClientStatusChanged && (connectionStatusChangesHandler != null))
            {
                // codes_SRS_DEVICECLIENT_28_024: [** `OnConnectionOpened` shall invoke the connectionStatusChangesHandler if ConnectionStatus is changed **]**  
                this.connectionStatusChangesHandler(ConnectionStatus.Connected, e.ConnectionStatusChangeReason);
            }
        }

        /// <summary>
        /// The delegate for handling disrupted connection/links in the transport layer.
        /// </summary>
        internal async Task OnConnectionClosed(object sender, ConnectionEventArgs e)
        {
            ConnectionStatusChangeResult result = null;

            // codes_SRS_DEVICECLIENT_28_023: [** `OnConnectionClosed` shall notify ConnectionStatusManager of the connection updates. **]**
            if (e.ConnectionStatus == ConnectionStatus.Disconnected_Retrying)
            {
                try
                {
                    // codes_SRS_DEVICECLIENT_28_022: [** `OnConnectionClosed` shall invoke the RecoverConnections operation. **]**          
                    await ApplyTimeout(async operationTimeoutCancellationTokenSource =>
                    {
                        result = this.connectionStatusManager.ChangeTo(e.ConnectionType, ConnectionStatus.Disconnected_Retrying, ConnectionStatus.Connected);
                        if (result.IsClientStatusChanged && (connectionStatusChangesHandler != null))
                        {
                            this.connectionStatusChangesHandler(e.ConnectionStatus, e.ConnectionStatusChangeReason);
                        }

                        using (CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(result.StatusChangeCancellationTokenSource.Token, operationTimeoutCancellationTokenSource.Token))
                        {
                            await this.InnerHandler.RecoverConnections(sender, e.ConnectionType, linkedTokenSource.Token).ConfigureAwait(false);
                        }

                    }).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // codes_SRS_DEVICECLIENT_28_027: [** `OnConnectionClosed` shall invoke the connectionStatusChangesHandler if RecoverConnections throw exception **]**
                    result = this.connectionStatusManager.ChangeTo(e.ConnectionType, ConnectionStatus.Disconnected);
                    if (result.IsClientStatusChanged && (connectionStatusChangesHandler != null))
                    {
                        this.connectionStatusChangesHandler(ConnectionStatus.Disconnected, ConnectionStatusChangeReason.Retry_Expired);
                    }
                }
            }
            else
            {
                result = this.connectionStatusManager.ChangeTo(e.ConnectionType, ConnectionStatus.Disabled);
                if (result.IsClientStatusChanged && (connectionStatusChangesHandler != null))
                {
                    this.connectionStatusChangesHandler(ConnectionStatus.Disabled, e.ConnectionStatusChangeReason);
                }
            }
        }

        /// <summary>
        /// The delegate for handling direct methods received from service.
        /// </summary>
        internal async Task OnMethodCalled(MethodRequestInternal methodRequestInternal)
        {
            Tuple<MethodCallback, object> m = null;

            // codes_SRS_DEVICECLIENT_10_012: [ If the given methodRequestInternal argument is null, fail silently ]
            if (methodRequestInternal != null)
            {
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
                catch (Exception)
                {
                    // codes_SRS_DEVICECLIENT_28_020: [ If the given methodRequestInternal data is not valid json, respond with status code 400 (BAD REQUEST) ]
                    methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, (int)MethodResposeStatusCode.BadRequest);
                    await this.SendMethodResponseAsync(methodResponseInternal).ConfigureAwait(false);
                    return;
                }
                finally
                {
                    methodsDictionarySemaphore.Release();
                }

                if (m != null)
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
                    catch (Exception)
                    {
                        // codes_SRS_DEVICECLIENT_28_021: [ If the MethodResponse from the MethodHandler is not valid json, respond with status code 500 (USER CODE EXCEPTION) ]
                        methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, (int)MethodResposeStatusCode.UserCodeException);
                    }
                }
                else
                {
                    // codes_SRS_DEVICECLIENT_10_013: [ If the given method does not have an associated delegate and no default delegate was registered, respond with status code 501 (METHOD NOT IMPLEMENTED) ]
                    methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, (int)MethodResposeStatusCode.MethodNotImplemented);
                }
                await this.SendMethodResponseAsync(methodResponseInternal).ConfigureAwait(false);
            }
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
            return SetDesiredPropertyUpdateCallbackAsync(callback, userContext);
        }

        /// <summary>
        /// Set a callback that will be called whenever the client receives a state update 
        /// (desired or reported) from the service.  This has the side-effect of subscribing
        /// to the PATCH topic on the service.
        /// </summary>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        public Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext)
        {
            // Codes_SRS_DEVICECLIENT_18_007: `SetDesiredPropertyUpdateCallbackAsync` shall throw an `ArgumentNull` exception if `callback` is null
            if (callback == null)
            {
                throw Fx.Exception.ArgumentNull("callback");
            }

            return ApplyTimeout(async operationTimeoutCancellationToken =>
            {
                // Codes_SRS_DEVICECLIENT_18_003: `SetDesiredPropertyUpdateCallbackAsync` shall call the transport to register for PATCHes on it's first call.
                // Codes_SRS_DEVICECLIENT_18_004: `SetDesiredPropertyUpdateCallbackAsync` shall not call the transport to register for PATCHes on subsequent calls
                if (!this.patchSubscribedWithService)
                {
                    await this.InnerHandler.EnableTwinPatchAsync(operationTimeoutCancellationToken).ConfigureAwait(false);
                    patchSubscribedWithService = true;
                }

                this.desiredPropertyUpdateCallback = callback;
                this.twinPatchCallbackContext = userContext;
            });
        }

        /// <summary>
        /// Retrieve a device twin object for the current device.
        /// </summary>
        /// <returns>The device twin object for the current device</returns>
        public Task<Twin> GetTwinAsync()
        {
            return ApplyTimeoutTwin(async operationTimeoutCancellationToken =>
            {
                // Codes_SRS_DEVICECLIENT_18_001: `GetTwinAsync` shall call `SendTwinGetAsync` on the transport to get the twin state
                return await this.InnerHandler.SendTwinGetAsync(operationTimeoutCancellationToken).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties)
        {
            // Codes_SRS_DEVICECLIENT_18_006: `UpdateReportedPropertiesAsync` shall throw an `ArgumentNull` exception if `reportedProperties` is null
            if (reportedProperties == null)
            {
                throw Fx.Exception.ArgumentNull("reportedProperties");
            }
            return ApplyTimeout(async operationTimeoutCancellationToken =>
            {
                // Codes_SRS_DEVICECLIENT_18_002: `UpdateReportedPropertiesAsync` shall call `SendTwinPatchAsync` on the transport to update the reported properties
                await this.InnerHandler.SendTwinPatchAsync(reportedProperties, operationTimeoutCancellationToken).ConfigureAwait(false);
            });
        }

        //  Codes_SRS_DEVICECLIENT_18_005: When a patch is received from the service, the `callback` shall be called.
        internal void OnReportedStatePatchReceived(TwinCollection patch)
        {
            if (this.desiredPropertyUpdateCallback != null)
            {
                this.desiredPropertyUpdateCallback(patch, this.twinPatchCallbackContext);
            }
        }

        private async Task EnableMethodAsync()
        {
            if (this.deviceMethods == null && this.deviceDefaultMethodCallback == null)
            {
                await ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.EnableMethodsAsync(operationTimeoutCancellationToken)).ConfigureAwait(false);
            }
        }

        private async Task DisableMethodAsync()
        {
            if (this.deviceMethods == null && this.deviceDefaultMethodCallback == null)
            {
                await ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.DisableMethodsAsync(operationTimeoutCancellationToken)).ConfigureAwait(false);
            }
        }

        internal bool IsE2EDiagnosticSupportedProtocol()
        {
            foreach (ITransportSettings transportSetting in this.transportSettings)
            {
                var transportType = transportSetting.GetTransportType();
                if (!(transportType == TransportType.Amqp_WebSocket_Only || transportType == TransportType.Amqp_Tcp_Only
                    || transportType == TransportType.Mqtt_WebSocket_Only || transportType == TransportType.Mqtt_Tcp_Only))
                {
                    throw new NotSupportedException($"{transportType} protocal doesn't support E2E diagnostic.");
                }
            }
            return true;
        }

        #region Module Specific API
        /// <summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="message">The message to send</param>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(string outputName, Message message)
        {
            ValidateModuleTransportHandler("SendEventAsync for a named output");

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
            ValidateModuleTransportHandler("SendEventBatchAsync for a named output");

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
            ValidateModuleTransportHandler("SetInputMessageHandlerAsync for a named output");
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
                MessageResponse response = await (callback?.Item1?.Invoke(message, callback.Item2) ?? Task.FromResult(MessageResponse.Completed));

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

        void ValidateModuleTransportHandler(string apiName)
        {
            if (string.IsNullOrEmpty(this.iotHubConnectionString.ModuleId))
            {
                throw new InvalidOperationException("{0} is available for Modules only.".FormatInvariant(apiName));
            }
        }

        #endregion Module Specific API
    }
}
