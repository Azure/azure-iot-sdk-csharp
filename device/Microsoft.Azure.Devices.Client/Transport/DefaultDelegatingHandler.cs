// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Common;

    abstract class DefaultDelegatingHandler : IDelegatingHandler
    {
        static readonly Task<Message> DummyResultObject = Task.FromResult((Message)null);

        public IDelegatingHandler InnerHandler { get; protected set; }

        protected DefaultDelegatingHandler()
            : this(null)
        {
        }

        protected DefaultDelegatingHandler(IDelegatingHandler innerHandler)
        {
            this.InnerHandler = innerHandler;
        }

        public virtual Task OpenAsync(bool explicitOpen, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.OpenAsync(explicitOpen, cancellationToken) ?? TaskConstants.Completed;
        }
        
        public virtual Task CloseAsync()
        {
            if (this.InnerHandler == null)
            {
                return TaskConstants.Completed;
            }
            else
            {
                Task closeTask = this.InnerHandler.CloseAsync();
                closeTask.ContinueWith(t => GC.SuppressFinalize(this), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                return closeTask;
            }
        }

        public virtual Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            return this.InnerHandler?.ReceiveAsync(cancellationToken) ?? DummyResultObject;
        }

        public virtual Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.ReceiveAsync(timeout, cancellationToken) ?? DummyResultObject;
        }

        public virtual Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.CompleteAsync(lockToken, cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.AbandonAsync(lockToken, cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.RejectAsync(lockToken, cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.SendEventAsync(message, cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.SendEventAsync(messages, cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            return this.InnerHandler?.EnableMethodsAsync(cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task SendMethodResponseAsync(Method method, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.SendMethodResponseAsync(method, cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task EnableTwinAsync(CancellationToken cancellationToken)
        {
            return this.InnerHandler?.EnableTwinAsync(cancellationToken) ?? TaskConstants.Completed;
        }

        public virtual Task SendTwinGetAsync(Twin twin, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.SendTwinGetAsync(twin, cancellationToken) ?? TaskConstants.Completed;
        }
        
        public virtual Task SendTwinUpdateAsync(Twin twin, TwinProperties properties, CancellationToken cancellationToken)
        {
            return this.InnerHandler?.SendTwinUpdateAsync(twin, properties, cancellationToken) ?? TaskConstants.Completed;
        }

        private TwinUpdateCallback twinUpdateHandler;
        public virtual TwinUpdateCallback TwinUpdateHandler
        {
            set
            {
                if (this.InnerHandler != null)
                {
                    this.InnerHandler.TwinUpdateHandler = value;
                }
                else
                {
                    this.twinUpdateHandler = value;
                }
            }
        }

        private MethodCallCallback methodCallHandler;
        public virtual MethodCallCallback MethodCallHandler
        {
            set
            {
                if (this.InnerHandler != null)
                {
                    this.InnerHandler.MethodCallHandler = value;
                }
                else
                {
                    this.methodCallHandler = value;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            this.InnerHandler?.Dispose();
        }

        public void Dispose()
        {
            this.Dispose(true);   
            GC.SuppressFinalize(this);
        }

        ~DefaultDelegatingHandler()
        {
            this.Dispose(false);
        }
    }
}
