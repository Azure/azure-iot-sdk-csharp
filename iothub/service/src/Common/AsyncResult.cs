// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Azure.Devices.Common.Tracing;

#if NET451
using System.Transactions;
#endif

namespace Microsoft.Azure.Devices.Common
{
    // AsyncResult starts acquired; Complete releases.
    [Fx.Tag.SynchronizationPrimitive(Fx.Tag.BlocksUsing.ManualResetEvent, SupportsAsync = true, ReleaseMethod = "Complete")]
    [DebuggerStepThrough]
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "Uses custom scheme for cleanup")]
    internal abstract class AsyncResult : IAsyncResult
    {
        public const string DisablePrepareForRethrow = "DisablePrepareForRethrow";

        private static AsyncCallback s_asyncCompletionWrapperCallback;
        private readonly AsyncCallback _callback;
        private bool _endCalled;
        private Exception _exception;
        private AsyncCompletion _nextAsyncCompletion;
#if NET451
        private IAsyncResult _deferredTransactionalResult;
#endif

        [Fx.Tag.SynchronizationObject]
        private ManualResetEvent _manualResetEvent;

        [Fx.Tag.SynchronizationObject(Blocking = false)]
        private readonly object _thisLock;

#if DEBUG
        private UncompletedAsyncResultMarker _marker;
#endif

        protected AsyncResult(AsyncCallback callback, object state)
        {
            _callback = callback;
            AsyncState = state;
            _thisLock = new object();

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

        public bool HasCallback => _callback != null;

        public bool IsCompleted { get; private set; }

        // used in conjunction with PrepareAsyncCompletion to allow for finally blocks
        protected Action<AsyncResult, Exception> OnCompleting { get; set; }

        // Override this property to provide the ActivityId when completing with exception
        protected internal virtual EventTraceActivity Activity => null;

#if NET451
        // Override this property to change the trace level when completing with exception
        protected virtual TraceEventType TraceEventType => TraceEventType.Verbose;
#endif

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
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }
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

                    throw Fx.Exception.AsError(new CallbackException(CommonResources.AsyncCallbackThrewException, e));
                }
#pragma warning restore 1634
            }

            return true;
        }

        protected bool TryComplete(bool didcompleteSynchronously)
        {
            return TryComplete(didcompleteSynchronously, null);
        }

        protected void Complete(bool didCompleteSynchronously)
        {
            Complete(didCompleteSynchronously, null);
        }

        protected void Complete(bool didCompleteSynchronously, Exception e)
        {
            if (!TryComplete(didCompleteSynchronously, e))
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(CommonResources.GetString(Resources.AsyncResultCompletedTwice, GetType())));
            }
        }

        private static void AsyncCompletionWrapperCallback(IAsyncResult result)
        {
            if (result == null)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(CommonResources.InvalidNullAsyncResult));
            }

            if (result.CompletedSynchronously)
            {
                return;
            }

            var thisPtr = (AsyncResult)result.AsyncState;

            AsyncCompletion callback = thisPtr.GetNextCompletion();
            if (callback == null)
            {
                ThrowInvalidAsyncResult(result);
            }

            bool completeSelf = false;
            Exception completionException = null;
            try
            {
                completeSelf = callback(result);
            }
            catch (Exception e)
            {
                completeSelf = true;
                completionException = e;
            }

            if (completeSelf)
            {
                thisPtr.Complete(false, completionException);
            }
        }

        protected AsyncCallback PrepareAsyncCompletion(AsyncCompletion callback)
        {
            _nextAsyncCompletion = callback;
            if (s_asyncCompletionWrapperCallback == null)
            {
                s_asyncCompletionWrapperCallback = new AsyncCallback(AsyncCompletionWrapperCallback);
            }
            return s_asyncCompletionWrapperCallback;
        }

        protected bool CheckSyncContinue(IAsyncResult result)
        {
            return TryContinueHelper(result, out AsyncCompletion dummy);
        }

        protected bool SyncContinue(IAsyncResult result)
        {
            if (TryContinueHelper(result, out AsyncCompletion callback))
            {
                return callback(result);
            }
            else
            {
                return false;
            }
        }

        private bool TryContinueHelper(IAsyncResult result, out AsyncCompletion callback)
        {
            if (result == null)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(CommonResources.InvalidNullAsyncResult));
            }

            callback = null;
            if (!result.CompletedSynchronously)
            {
                return false;
            }
#if NET451
            else
            {
                if (ReferenceEquals(result, _deferredTransactionalResult))
                {
                    _deferredTransactionalResult = null;
                }
            }
#endif

            callback = GetNextCompletion();
            if (callback == null)
            {
                ThrowInvalidAsyncResult("Only call Check/SyncContinue once per async operation (once per PrepareAsyncCompletion).");
            }
            return true;
        }

        private AsyncCompletion GetNextCompletion()
        {
            AsyncCompletion result = _nextAsyncCompletion;
            _nextAsyncCompletion = null;
            return result;
        }

        protected static void ThrowInvalidAsyncResult(IAsyncResult result)
        {
            throw Fx.Exception.AsError(
                new InvalidOperationException(
                    CommonResources.GetString(Resources.InvalidAsyncResultImplementation, result.GetType())));
        }

        protected static void ThrowInvalidAsyncResult(string debugText)
        {
            string message = CommonResources.InvalidAsyncResultImplementationGeneric;
            if (debugText != null)
            {
#if DEBUG
                message += " " + debugText;
#endif
            }
            throw Fx.Exception.AsError(new InvalidOperationException(message));
        }

        [Fx.Tag.Blocking(Conditional = "!asyncResult.isCompleted")]
        protected static TAsyncResult End<TAsyncResult>(IAsyncResult result)
            where TAsyncResult : AsyncResult
        {
            if (result == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(result));
            }

            var asyncResult = result as TAsyncResult;

            if (asyncResult == null)
            {
                throw Fx.Exception.Argument(nameof(result), Resources.InvalidAsyncResult);
            }

            if (asyncResult._endCalled)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(Resources.AsyncResultAlreadyEnded));
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
#if NET451
                asyncResult._manualResetEvent.Close();
#else
                asyncResult._manualResetEvent.Dispose();
#endif
            }

            if (asyncResult._exception != null)
            {
                // Trace before PrepareForRethrow to avoid weird callstack strings
#if NET451
                Fx.Exception.TraceException(asyncResult._exception, asyncResult.TraceEventType);
#else
                Fx.Exception.TraceException(asyncResult._exception, TraceEventType.Verbose);
#endif
                ExceptionDispatcher.Throw(asyncResult._exception);
            }

            return asyncResult;
        }

        private enum TransactionSignalState
        {
            Ready = 0,
            Prepared,
            Completed,
            Abandoned,
        }

        // can be utilized by subclasses to write core completion code for both the sync and async paths
        // in one location, signalling chainable synchronous completion with the boolean result,
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

            public AsyncResult AsyncResult { get; set; }
        }

#endif
    }

    // Use this as your base class for AsyncResult and you don't have to define the End method.
    internal abstract class AsyncResult<TAsyncResult> : AsyncResult
        where TAsyncResult : AsyncResult<TAsyncResult>
    {
        protected AsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        public static TAsyncResult End(IAsyncResult asyncResult)
        {
            return End<TAsyncResult>(asyncResult);
        }
    }
}
