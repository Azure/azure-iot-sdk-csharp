// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.TransientFaultHandling;
    using Microsoft.Azure.Devices.Shared;

    internal class RetryDelegatingHandler : DefaultDelegatingHandler
    {
        const int UndeterminedPosition = -1;
        const string StopRetrying = "StopRetrying";

        // RetryCount is used for testing purpose and is equal to MaxValue in prod.
        internal static int RetryCount = Int32.MaxValue;

        class SendMessageState
        {
            public int Iteration { get; set; }

            public long InitialStreamPosition { get; set; }

            public ExceptionDispatchInfo OriginalError { get; set; }
        }

        internal class IotHubTransientErrorIgnoreStrategy : ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex)
            {
                if (!(ex is IotHubClientTransientException))
                    return false;
                if (ex.Data[StopRetrying] == null)
                    return true;
                if ((bool)ex.Data[StopRetrying])
                {
                    ex.Data.Remove(StopRetrying);
                    return false;
                }
                return true;
            }
        }

        internal class IotHubRuntimeOperationRetryStrategy : RetryStrategy
        {
            internal readonly RetryStrategy retryStrategy;
            readonly ShouldRetry throttlingRetryStrategy;

            public IotHubRuntimeOperationRetryStrategy(RetryStrategy retryStrategy)
                : base(null, false)
            {
                this.retryStrategy = retryStrategy;
                this.throttlingRetryStrategy = new ExponentialBackoff(RetryCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(5)).GetShouldRetry();
            }

            public override ShouldRetry GetShouldRetry()
            {
                return this.ShouldRetry;
            }

            bool ShouldRetry(int retryCount, Exception lastException, out TimeSpan retryInterval)
            {
                if (lastException is IotHubThrottledException)
                {
                    return this.throttlingRetryStrategy(retryCount, lastException, out retryInterval);
                }
                return this.retryStrategy.GetShouldRetry()(retryCount, lastException, out retryInterval);
            }
        }

        internal RetryPolicy internalRetryPolicy;
        
        public void SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            this.internalRetryPolicy = new RetryPolicy(
                new IotHubTransientErrorIgnoreStrategy(), 
                new IotHubRuntimeOperationRetryStrategy(new RetryStrategyWrapper(retryPolicy)));
        }

        public RetryDelegatingHandler(IPipelineContext context)
            : base(context)
        {
            RetryStrategy retryStrategy = new ExponentialBackoff(RetryCount, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));
            this.internalRetryPolicy = new RetryPolicy(new IotHubTransientErrorIgnoreStrategy(), new IotHubRuntimeOperationRetryStrategy(retryStrategy));
        }
        

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                var sendState = new SendMessageState();
                await this.internalRetryPolicy.ExecuteAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return Common.TaskConstants.Completed;
                    }
                    return this.SendMessageWithRetryAsync(sendState, message, () => base.SendEventAsync(message, cancellationToken));
                }, cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
            }
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal method, CancellationToken cancellationToken)
        {
            try
            {
                var sendState = new SendMessageState();
                await this.internalRetryPolicy.ExecuteAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return Common.TaskConstants.Completed;
                    }
                    return base.SendMethodResponseAsync(method, cancellationToken);
                }, cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
            }
        }

        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            try
            {
                var sendState = new SendMessageState();
                IEnumerable<Message> messageList = messages as IList<Message> ?? messages.ToList();
                await this.internalRetryPolicy.ExecuteAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return Common.TaskConstants.Completed;
                    }
                    return this.SendMessageWithRetryAsync(sendState, messageList, () => base.SendEventAsync(messageList, cancellationToken));
                }, cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
            }
        }

        public override async Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await this.internalRetryPolicy.ExecuteAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        var tcs = new TaskCompletionSource<Message>();
                        tcs.TrySetCanceled();
                        return tcs.Task;
                    }

                    return base.ReceiveAsync(cancellationToken);
                }, cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
                throw;
            }
        }

        public override async Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                return await this.internalRetryPolicy.ExecuteAsync(() => base.ReceiveAsync(timeout, cancellationToken), cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
                throw;
            }
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await this.internalRetryPolicy.ExecuteAsync(() => base.EnableMethodsAsync(cancellationToken), cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
            }
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await this.internalRetryPolicy.ExecuteAsync(() => base.DisableMethodsAsync(cancellationToken), cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
            }
        }
        
        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                await this.internalRetryPolicy.ExecuteAsync(() => base.EnableTwinPatchAsync(cancellationToken), cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
            }
        }
        
        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Question for the masses:
                // If I'm returning a Task, do I want to await this so the catch block below can catch anything that happens inside base.SendTwinGetAsync?
                // Other methods in here do this both ways (with await and without) so it's not clear which is better.
                return await this.internalRetryPolicy.ExecuteAsync(() => base.SendTwinGetAsync(cancellationToken), cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
                throw;
            }
        }
        
        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties,  CancellationToken cancellationToken)
        {
            try
            {
                await this.internalRetryPolicy.ExecuteAsync(() => base.SendTwinPatchAsync(reportedProperties, cancellationToken), cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
            }
        }

        public override async Task RecoverConnections(object o, ConnectionType connectionType, CancellationToken cancellationToken)
        {
            try
            {
                await this.internalRetryPolicy.ExecuteAsync(() =>
                {
                    return base.RecoverConnections(o, connectionType, cancellationToken);
                }, cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
            }
        }

        public override async Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                await this.internalRetryPolicy.ExecuteAsync(() => base.CompleteAsync(lockToken, cancellationToken), cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
            }
        }

        public override async Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                await this.internalRetryPolicy.ExecuteAsync(() => base.AbandonAsync(lockToken, cancellationToken), cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
            }
        }

        public override async Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                await this.internalRetryPolicy.ExecuteAsync(() => base.RejectAsync(lockToken, cancellationToken), cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
            }
        }

        public override async Task OpenAsync(bool explicitOpen, CancellationToken cancellationToken)
        {
            try
            {
                await this.internalRetryPolicy.ExecuteAsync(() => base.OpenAsync(explicitOpen, cancellationToken), cancellationToken);
            }
            catch (IotHubClientTransientException ex)
            {
                GetNormalizedIotHubException(ex).Throw();
            }
        }

        async Task SendMessageWithRetryAsync(SendMessageState sendState, IEnumerable<Message> messages, Func<Task> action)
        {
            if (sendState.Iteration == 0)
            {
                foreach (Message message in messages)
                {
                    long messageStreamPosition = message.BodyStream.CanSeek ? message.BodyStream.Position : UndeterminedPosition;
                    if (messageStreamPosition == UndeterminedPosition || sendState.InitialStreamPosition == UndeterminedPosition)
                    {
                        sendState.InitialStreamPosition = UndeterminedPosition;
                    }
                    else
                    {
                        sendState.InitialStreamPosition = messageStreamPosition > sendState.InitialStreamPosition ? messageStreamPosition : sendState.InitialStreamPosition;
                    }
                    message.TryResetBody(messageStreamPosition);
                }

                await TryExecuteActionAsync(sendState, action);
                return;
            }

            EnsureStreamsAreInOriginalState(sendState, messages);

            await TryExecuteActionAsync(sendState, action);
        }

        async Task SendMessageWithRetryAsync(SendMessageState sendState, Message message, Func<Task> action)
        {
            if (sendState.Iteration == 0)
            {
                sendState.InitialStreamPosition = message.BodyStream.CanSeek ? message.BodyStream.Position : UndeterminedPosition;
                message.TryResetBody(sendState.InitialStreamPosition);

                await TryExecuteActionAsync(sendState, action);
                return;
            }

            EnsureStreamIsInOriginalState(sendState, message);

            await TryExecuteActionAsync(sendState, action);
        }

        static void EnsureStreamsAreInOriginalState(SendMessageState sendState, IEnumerable<Message> messages)
        {
            IEnumerable<Message> messageList = messages as IList<Message> ?? messages.ToList();

            //We do not retry if:
            //1. any message was attempted to read the body stream and the stream is not seekable; 
            //2. any message has initial stream position different from zero.

            if (sendState.InitialStreamPosition != 0)
            {
                sendState.OriginalError.SourceException.Data[StopRetrying] = true;
                sendState.OriginalError.Throw();
            }

            foreach (Message message in messageList)
            {               
                if (!message.IsBodyCalled || message.TryResetBody(0))
                {
                    continue;
                }

                sendState.OriginalError.SourceException.Data[StopRetrying] = true;
                sendState.OriginalError.Throw();
            }
        }

        static void EnsureStreamIsInOriginalState(SendMessageState sendState, Message message)
        {
            //We do not retry if a message was attempted to read the body stream and the stream is not seekable;             
            if (!message.IsBodyCalled || message.TryResetBody(sendState.InitialStreamPosition))
            {
                return;
            }

            sendState.OriginalError.SourceException.Data[StopRetrying] = true;
            sendState.OriginalError.Throw();
        }

        static async Task TryExecuteActionAsync(SendMessageState sendState, Func<Task> action)
        {
            sendState.Iteration++;
            try
            {
                await action();
            }
            catch (IotHubClientTransientException ex)
            {
                sendState.OriginalError = ExceptionDispatchInfo.Capture(ex);
                throw;
            }
        }

        static ExceptionDispatchInfo GetNormalizedIotHubException(IotHubClientTransientException ex)
        {
            if (ex.InnerException != null)
            {
                return ExceptionDispatchInfo.Capture(ex.InnerException);
            }
            return ExceptionDispatchInfo.Capture(ex);
        }
    }
}
