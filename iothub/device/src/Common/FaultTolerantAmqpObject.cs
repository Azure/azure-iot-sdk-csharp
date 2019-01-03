// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Devices.Shared;

    sealed class FaultTolerantAmqpObject<T> : Singleton<T> where T : AmqpObject
    {
        readonly Func<TimeSpan, CancellationToken, Task<T>> createObjectAsync;
        readonly Action<T> closeObject;
        readonly EventHandler onObjectClosed;

        public FaultTolerantAmqpObject(Func<TimeSpan, CancellationToken, Task<T> > createObjectAsync, Action<T> closeObject)
        {
            this.createObjectAsync = createObjectAsync;
            this.closeObject = closeObject;
            this.onObjectClosed = new EventHandler(this.OnObjectClosed);
        }

        public bool TryGetOpenedObject(out T openedAmqpObject)
        {
            if (Logging.IsEnabled) Logging.Enter(this, nameof(TryGetOpenedObject));

            var taskCompletionSource = this.TaskCompletionSource;
            if (taskCompletionSource != null && taskCompletionSource.Task.Status == TaskStatus.RanToCompletion)
            {
                openedAmqpObject = taskCompletionSource.Task.Result;
                if (openedAmqpObject == null || openedAmqpObject.State != AmqpObjectState.Opened)
                {
                    openedAmqpObject = null;
                }
            }
            else
            {
                openedAmqpObject = null;
            }

            if (Logging.IsEnabled) Logging.Exit(this, nameof(TryGetOpenedObject));
            return openedAmqpObject != null;
        }

        protected override async Task<T> OnCreateAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, timeout, cancellationToken, nameof(OnCreateAsync));

                T amqpObject = await this.createObjectAsync(timeout, cancellationToken).ConfigureAwait(false);
                amqpObject.SafeAddClosed((s, e) => this.Invalidate(amqpObject));
                amqpObject.Closed += OnObjectClosed;

                return amqpObject;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, cancellationToken, nameof(OnCreateAsync));
            }
        }

        protected override void OnSafeClose(T value)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, value.Identifier, nameof(OnSafeClose));
                this.closeObject(value);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, value.Identifier, nameof(OnSafeClose));
            }
        }

        void OnObjectClosed(object sender, EventArgs e)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, sender, nameof(OnObjectClosed));

                T instance = (T)sender;
                this.closeObject(instance);
                this.Invalidate(instance);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, sender, nameof(OnObjectClosed));
            }
        }
    }
}
