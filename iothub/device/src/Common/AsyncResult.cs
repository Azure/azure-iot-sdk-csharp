// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.Azure.Devices.Client
{
    // AsyncResult starts acquired; Complete releases.
    [Fx.Tag.SynchronizationPrimitive(Fx.Tag.BlocksUsing.ManualResetEvent, SupportsAsync = true, ReleaseMethod = "Complete")]
    [DebuggerStepThrough]
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "Uses custom scheme for cleanup")]
    internal abstract class AsyncResult : IAsyncResult
    {
        public const string DisablePrepareForRethrow = "DisablePrepareForRethrow";

        private readonly AsyncCallback _callback;
        private bool _endCalled;
        private Exception _exception;

        [Fx.Tag.SynchronizationObject]
        private ManualResetEvent _manualResetEvent;

        [Fx.Tag.SynchronizationObject(Blocking = false)]
        private readonly object _thisLock = new object();

#if DEBUG
        private UncompletedAsyncResultMarker _marker;
#endif

        protected AsyncResult(AsyncCallback callback, object state)
        {
            _callback = callback;
            AsyncState = state;

#if DEBUG
            _marker = new UncompletedAsyncResultMarker(this);
#endif
        }

        public object AsyncState { get; private set; }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_manualResetEvent != null)
                {
                    return _manualResetEvent;
                }

                lock (ThisLock)
                {
                    if (_manualResetEvent == null)
                    {
                        _manualResetEvent = new ManualResetEvent(IsCompleted);
                    }
                }

                return _manualResetEvent;
            }
        }

        public bool CompletedSynchronously { get; private set; }

        public bool IsCompleted { get; private set; }

        // used in conjunction with PrepareAsyncCompletion to allow for finally blocks
        protected Action<AsyncResult, Exception> OnCompleting { get; set; }

        protected object ThisLock => _thisLock;

        // subclasses like TraceAsyncResult can use this to wrap the callback functionality in a scope
        protected Action<AsyncCallback, IAsyncResult> VirtualCallback { get; set; }

        protected bool TryComplete(bool didCompleteSynchronously, Exception exception)
        {
            lock (ThisLock)
            {
                if (IsCompleted)
                {
                    return false;
                }

                _exception = exception;
                IsCompleted = true;
            }

#if DEBUG
            _marker.AsyncResult = null;
            _marker = null;
#endif

            CompletedSynchronously = didCompleteSynchronously;
            if (OnCompleting != null)
            {
                // Allow exception replacement, like a catch/throw pattern.
                try
                {
                    OnCompleting(this, _exception);
                }
                catch (Exception e) when (!Fx.IsFatal(e))
                {
                    _exception = e;
                }
            }

            if (didCompleteSynchronously)
            {
                // If we completedSynchronously, then there's no chance that the manualResetEvent was created so
                // we don't need to worry about a race
                Fx.Assert(_manualResetEvent == null, "No ManualResetEvent should be created for a synchronous AsyncResult.");
            }
            else
            {
                lock (ThisLock)
                {
                    if (_manualResetEvent != null)
                    {
                        _manualResetEvent.Set();
                    }
                }
            }

            if (_callback != null)
            {
                try
                {
                    if (VirtualCallback != null)
                    {
                        VirtualCallback(_callback, this);
                    }
                    else
                    {
                        _callback(this);
                    }
                }
#pragma warning disable 1634
#pragma warning suppress 56500 // transferring exception to another thread
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    throw Fx.Exception.AsError(new InvalidOperationException(CommonResources.AsyncCallbackThrewException, e));
                }
#pragma warning restore 1634
            }

            return true;
        }

        protected void Complete(bool didCompleteSynchronously)
        {
            Complete(didCompleteSynchronously, null);
        }

        protected void Complete(bool didCompleteSynchronously, Exception e)
        {
            if (!TryComplete(didCompleteSynchronously, e))
            {
                throw Fx.Exception.AsError(new InvalidOperationException(CommonResources.GetString(CommonResources.AsyncResultCompletedTwice, GetType())));
            }
        }

        [Fx.Tag.Blocking(Conditional = "!asyncResult.isCompleted")]
        protected static TAsyncResult End<TAsyncResult>(IAsyncResult result)
            where TAsyncResult : AsyncResult
        {
            if (result == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(result));
            }

            if (!(result is TAsyncResult asyncResult))
            {
                throw Fx.Exception.Argument(nameof(result), CommonResources.InvalidAsyncResult);
            }

            if (asyncResult._endCalled)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(CommonResources.AsyncResultAlreadyEnded));
            }

            asyncResult._endCalled = true;

            if (!asyncResult.IsCompleted)
            {
                lock (asyncResult.ThisLock)
                {
                    if (!asyncResult.IsCompleted && asyncResult._manualResetEvent == null)
                    {
                        asyncResult._manualResetEvent = new ManualResetEvent(asyncResult.IsCompleted);
                    }
                }
            }

            if (asyncResult._manualResetEvent != null)
            {
                asyncResult._manualResetEvent.WaitOne();
                asyncResult._manualResetEvent.Dispose();
            }

            if (asyncResult._exception != null)
            {
                // Trace before PrepareForRethrow to avoid weird callstack strings
                Fx.Exception.TraceException(asyncResult._exception, TraceEventType.Verbose);
                ExceptionDispatcher.Throw(asyncResult._exception);
            }

            return asyncResult;
        }

        // can be utilized by subclasses to write core completion code for both the sync and async paths
        // in one location, signaling chainable synchronous completion with the boolean result,
        // and leveraging PrepareAsyncCompletion for conversion to an AsyncCallback.
        // NOTE: requires that "this" is passed in as the state object to the asynchronous sub-call being used with a completion routine.
        protected delegate bool AsyncCompletion(IAsyncResult result);

#if DEBUG

        private class UncompletedAsyncResultMarker
        {
            public UncompletedAsyncResultMarker(AsyncResult result)
            {
                AsyncResult = result;
            }

            [SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode,
                Justification = "Debug-only facility")]
            public AsyncResult AsyncResult { get; set; }
        }

#endif
    }

    // Use this as your base class for AsyncResult and you don't have to define the End method.
    internal abstract class AsyncResultT<TAsyncResult> : AsyncResult
        where TAsyncResult : AsyncResultT<TAsyncResult>
    {
        protected AsyncResultT(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        public static TAsyncResult End(IAsyncResult asyncResult)
        {
            return End<TAsyncResult>(asyncResult);
        }
    }
}
