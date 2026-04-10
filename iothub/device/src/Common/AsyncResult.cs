// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

#if NET451
using System.Transactions;
#endif

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

        private static AsyncCallback s_asyncCompletionWrapperCallback;
        private AsyncCallback _callback;
        private bool _endCalled;
        private Exception _exception;
        private AsyncCompletion _nextAsyncCompletion;
#if NET451
        private IAsyncResult _deferredTransactionalResult;
        private TransactionSignalScope _transactionContext;
#endif

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

#if NET451
            if (thisPtr._transactionContext != null && !thisPtr._transactionContext.Signal(result))
            {
                // The TransactionScope isn't cleaned up yet and can't be done on this thread.  Must defer
                // the callback (which is likely to attempt to commit the transaction) until later.
                return;
            }
#endif

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
#if NET451
            if (_transactionContext != null)
            {
                // It might be an old, leftover one, if an exception was thrown within the last using (PrepareTransactionalCall()) block.
                if (_transactionContext.IsPotentiallyAbandoned)
                {
                    _transactionContext = null;
                }
                else
                {
                    _transactionContext.Prepared();
                }
            }
#endif
            _nextAsyncCompletion = callback;
            if (s_asyncCompletionWrapperCallback == null)
            {
                s_asyncCompletionWrapperCallback = new AsyncCallback(AsyncCompletionWrapperCallback);
            }
            return s_asyncCompletionWrapperCallback;
        }

#if NET451
        protected IDisposable PrepareTransactionalCall(Transaction transaction)
        {
            if (_transactionContext != null && !_transactionContext.IsPotentiallyAbandoned)
            {
                ThrowInvalidAsyncResult("PrepareTransactionalCall should only be called as the object of non-nested using statements. If the Begin succeeds, Check/SyncContinue must be called before another PrepareTransactionalCall.");
            }
            return _transactionContext = transaction == null ? null : new TransactionSignalScope(this, transaction);
        }
#endif

        protected bool CheckSyncContinue(IAsyncResult result)
        {
            return TryContinueHelper(result, out AsyncCompletion dummy);
        }

        protected bool SyncContinue(IAsyncResult result)
        {
            return TryContinueHelper(result, out AsyncCompletion callback)
                ? callback(result)
                : false;
        }

        private bool TryContinueHelper(IAsyncResult result, out AsyncCompletion callback)
        {
            if (result == null)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(CommonResources.InvalidNullAsyncResult));
            }

            callback = null;

            if (result.CompletedSynchronously)
            {
#if NET451
                // Once we pass the check, we know that we own forward progress, so transactionContext is correct. Verify its state.
                if (_transactionContext != null)
                {
                    if (_transactionContext.State != TransactionSignalState.Completed)
                    {
                        ThrowInvalidAsyncResult("Check/SyncContinue cannot be called from within the PrepareTransactionalCall using block.");
                    }
                    else if (_transactionContext.IsSignalled)
                    {
                        // This is most likely to happen when result.CompletedSynchronously registers differently here and in the callback, which
                        // is the fault of 'result'.
                        ThrowInvalidAsyncResult(result);
                    }
                }
#endif
            }
#if NET451
            else if (object.ReferenceEquals(result, _deferredTransactionalResult))
            {
                // The transactionContext may not be current if forward progress has been made via the callback. Instead,
                // use deferredTransactionalResult to see if we are supposed to execute a post-transaction callback.
                //
                // Once we pass the check, we know that we own forward progress, so transactionContext is correct. Verify its state.
                if (_transactionContext == null || !_transactionContext.IsSignalled)
                {
                    ThrowInvalidAsyncResult(result);
                }
                _deferredTransactionalResult = null;
            }
#endif
            else
            {
                return false;
            }

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
#if NET451
            _transactionContext = null;
#endif
            _nextAsyncCompletion = null;
            return result;
        }

        protected static void ThrowInvalidAsyncResult(IAsyncResult result)
        {
            throw Fx.Exception.AsError(new InvalidOperationException(CommonResources.GetString(CommonResources.InvalidAsyncResultImplementation, result.GetType())));
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

#if NET451
        [Serializable]
        class TransactionSignalScope : SignalGateT<IAsyncResult>, IDisposable
        {
            private bool _disposed;

            [NonSerialized]
            private TransactionScope _transactionScope;

            [NonSerialized]
            private readonly AsyncResult _parent;

            public TransactionSignalScope(AsyncResult result, Transaction transaction)
            {
                Fx.Assert(transaction != null, "Null Transaction provided to AsyncResult.TransactionSignalScope.");
                _parent = result;
                _transactionScope = Fx.CreateTransactionScope(transaction);
            }

            public TransactionSignalState State { get; private set; }

            public bool IsPotentiallyAbandoned => State == TransactionSignalState.Abandoned
                || State == TransactionSignalState.Completed
                && !IsSignalled;

            public void Prepared()
            {
                if (State != TransactionSignalState.Ready)
                {
                    ThrowInvalidAsyncResult("PrepareAsyncCompletion should only be called once per PrepareTransactionalCall.");
                }

                State = TransactionSignalState.Prepared;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing && !_disposed)
                {
                    _disposed = true;

                    if (State == TransactionSignalState.Ready)
                    {
                        State = TransactionSignalState.Abandoned;
                    }
                    else if (State == TransactionSignalState.Prepared)
                    {
                        State = TransactionSignalState.Completed;
                    }
                    else
                    {
                        ThrowInvalidAsyncResult("PrepareTransactionalCall should only be called in a using. Dispose called multiple times.");
                    }

                    try
                    {
                        Fx.CompleteTransactionScope(ref _transactionScope);
                    }
                    catch (Exception exception)
                    {
                        if (Fx.IsFatal(exception))
                        {
                            throw;
                        }

                        // Complete and Dispose are not expected to throw.  If they do it can mess up the AsyncResult state machine.
                        throw Fx.Exception.AsError(new InvalidOperationException(CommonResources.AsyncTransactionException));
                    }

                    // This will release the callback to run, or tell us that we need to defer the callback to Check/SyncContinue.
                    //
                    // It's possible to avoid this Interlocked when CompletedSynchronously is true, but we have no way of knowing that
                    // from here, and adding a way would add complexity to the AsyncResult transactional calling pattern. This
                    // unnecessary Interlocked only happens when: PrepareTransactionalCall is called with a non-null transaction,
                    // PrepareAsyncCompletion is reached, and the operation completes synchronously or with an exception.
                    if (State == TransactionSignalState.Completed
                        && Unlock(out IAsyncResult result))
                    {
                        if (_parent._deferredTransactionalResult != null)
                        {
                            ThrowInvalidAsyncResult(_parent._deferredTransactionalResult);
                        }
                        _parent._deferredTransactionalResult = result;
                    }
                }
            }

            void IDisposable.Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
#endif

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
