// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client
{
    internal abstract class Singleton<TValue> : IDisposable where TValue : class
    {
        private readonly object _syncLock;
        
        private TaskCompletionSource<TValue> _taskCompletionSource;
        private volatile bool _isDisposed;

        public Singleton()
        {
            _syncLock = new object();
        }

        protected TaskCompletionSource<TValue> TaskCompletionSource => _taskCompletionSource;

        // Test verification only
        internal TValue Value
        {
            get
            {
                TaskCompletionSource<TValue> thisTaskCompletionSource = _taskCompletionSource;
                return thisTaskCompletionSource != null && thisTaskCompletionSource.Task.Status == TaskStatus.RanToCompletion
                    ? thisTaskCompletionSource.Task.Result
                    : null;
            }
        }

        public Task OpenAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return GetOrCreateAsync(timeout, cancellationToken);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            Dispose();

            return TaskHelpers.CompletedTask;
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                TaskCompletionSource<TValue> thisTaskCompletionSource = _taskCompletionSource;
                if (thisTaskCompletionSource != null && thisTaskCompletionSource.Task.Status == TaskStatus.RanToCompletion)
                {
                    OnSafeClose(thisTaskCompletionSource.Task.Result);
                }
            }
        }

        public async Task<TValue> GetOrCreateAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var timeoutHelper = new TimeoutHelper(timeout);

            while (!_isDisposed && timeoutHelper.GetRemainingTime() > TimeSpan.Zero)
            {
                if (TryGet(out TaskCompletionSource<TValue> tcs))
                {
                    return await tcs.Task.ConfigureAwait(false);
                }

                tcs = new TaskCompletionSource<TValue>();
                if (TrySet(tcs))
                {
                    await CreateValueAsync(tcs, timeoutHelper.GetRemainingTime(), cancellationToken).ConfigureAwait(false);
                }
            }

            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            else
            {
                throw new TimeoutException("Timed out trying to create {0}".FormatInvariant(GetType().Name));
            }
        }

        protected void Invalidate(TValue instance)
        {
            lock (_syncLock)
            {
                if (_taskCompletionSource != null
                    && _taskCompletionSource.Task.Status == TaskStatus.RanToCompletion
                    && _taskCompletionSource.Task.Result == instance)
                {
                    Volatile.Write<TaskCompletionSource<TValue>>(ref _taskCompletionSource, null);
                }
            }
        }

        protected abstract Task<TValue> OnCreateAsync(TimeSpan timeout, CancellationToken cancellationToken);

        protected abstract void OnSafeClose(TValue value);

        private bool TryGet(out TaskCompletionSource<TValue> tcs)
        {
            tcs = Volatile.Read<TaskCompletionSource<TValue>>(ref _taskCompletionSource);
            return tcs != null;
        }

        private bool TrySet(TaskCompletionSource<TValue> tcs)
        {
            lock (_syncLock)
            {
                if (_taskCompletionSource == null)
                {
                    Volatile.Write<TaskCompletionSource<TValue>>(ref _taskCompletionSource, tcs);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool TryRemove()
        {
            lock (_syncLock)
            {
                if (_taskCompletionSource != null)
                {
                    Volatile.Write<TaskCompletionSource<TValue>>(ref _taskCompletionSource, null);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private async Task CreateValueAsync(TaskCompletionSource<TValue> tcs, TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                TValue value = await OnCreateAsync(timeout, cancellationToken).ConfigureAwait(false);
                tcs.SetResult(value);

                if (_isDisposed)
                {
                    OnSafeClose(value);
                }
            }
            catch (Exception ex)
            {
                if (Fx.IsFatal(ex))
                {
                    throw;
                }

                TryRemove();
                tcs.SetException(ex);
            }
        }
    }
}
