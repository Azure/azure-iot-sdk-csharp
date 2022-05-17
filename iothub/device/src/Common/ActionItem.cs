// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security;
using System.Threading;

namespace Microsoft.Azure.Devices.Client
{
    internal abstract class ActionItem
    {
#if NET451
        [Fx.Tag.SecurityNote(Critical = "Stores the security context, used later in binding back into")]
        [SecurityCritical]
        private SecurityContext _context;
#endif
        private bool _isScheduled;

        protected ActionItem()
        {
        }

        public bool LowPriority { get; protected set; }

        public static void Schedule(Action<object> callback, object state)
        {
            Schedule(callback, state, false);
        }

        [Fx.Tag.SecurityNote(Critical = "Calls into critical method ScheduleCallback",
            Safe = "Schedule invoke of the given delegate under the current context")]
        public static void Schedule(Action<object> callback, object state, bool lowPriority)
        {
            Fx.Assert(callback != null, "A null callback was passed for Schedule!");

            if (PartialTrustHelpers.ShouldFlowSecurityContext || WaitCallbackActionItem.ShouldUseActivity)
            {
                new DefaultActionItem(callback, state, lowPriority).Schedule();
            }
            else
            {
                ScheduleCallback(callback, state, lowPriority);
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Called after applying the user context on the stack or (potentially) " +
            "without any user context on the stack")]
        [SecurityCritical]
        protected abstract void Invoke();

        [Fx.Tag.SecurityNote(Critical = "Access critical field context and critical property " +
            "CallbackHelper.InvokeWithContextCallback, calls into critical method " +
            "PartialTrustHelpers.CaptureSecurityContextNoIdentityFlow, calls into critical method ScheduleCallback; " +
            "since the invoked method and the capturing of the security contex are de-coupled, can't " +
            "be treated as safe")]
        [SecurityCritical]
        protected void Schedule()
        {
            if (_isScheduled)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(CommonResources.ActionItemIsAlreadyScheduled));
            }

            _isScheduled = true;
#if NET451
            if (PartialTrustHelpers.ShouldFlowSecurityContext)
            {
                _context = PartialTrustHelpers.CaptureSecurityContextNoIdentityFlow();
            }
            if (_context != null)
            {
                ScheduleCallback(CallbackHelper.InvokeWithContextCallback);
            }
            else
#endif
            {
                ScheduleCallback(CallbackHelper.InvokeWithoutContextCallback);
            }
        }

#if NET451
        [Fx.Tag.SecurityNote(Critical = "Access critical field context and critical property " +
            "CallbackHelper.InvokeWithContextCallback, calls into critical method ScheduleCallback; " +
            "since nothing is known about the given context, can't be treated as safe")]
        [SecurityCritical]
        protected void ScheduleWithContext(SecurityContext contextToSchedule)
        {
            if (contextToSchedule == null)
            {
                throw Fx.Exception.ArgumentNull("context");
            }
            if (_isScheduled)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(CommonResources.ActionItemIsAlreadyScheduled));
            }

            _isScheduled = true;
            _context = contextToSchedule.CreateCopy();
            ScheduleCallback(CallbackHelper.InvokeWithContextCallback);
        }
#endif

        [Fx.Tag.SecurityNote(Critical = "Access critical property CallbackHelper.InvokeWithoutContextCallback, " +
            "Calls into critical method ScheduleCallback; not bound to a security context")]
        [SecurityCritical]
        protected void ScheduleWithoutContext()
        {
            if (_isScheduled)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(CommonResources.ActionItemIsAlreadyScheduled));
            }

            _isScheduled = true;
            ScheduleCallback(CallbackHelper.InvokeWithoutContextCallback);
        }

        [Fx.Tag.SecurityNote(Critical = "Calls into critical methods IOThreadScheduler.ScheduleCallbackNoFlow, " +
            "IOThreadScheduler.ScheduleCallbackLowPriNoFlow")]
        [SecurityCritical]
        private static void ScheduleCallback(Action<object> callback, object state, bool lowPriority)
        {
            Fx.Assert(callback != null, "Cannot schedule a null callback");
            if (lowPriority)
            {
                IoThreadScheduler.ScheduleCallbackLowPriNoFlow(callback, state);
            }
            else
            {
                IoThreadScheduler.ScheduleCallbackNoFlow(callback, state);
            }
        }

#if NET451
        [Fx.Tag.SecurityNote(Critical = "Extract the security context stored and reset the critical field")]
        [SecurityCritical]
        private SecurityContext ExtractContext()
        {
            Fx.Assert(_context != null, "Cannot bind to a null context; context should have been set by now");
            Fx.Assert(_isScheduled, "Context is extracted only while the object is scheduled");
            SecurityContext result = _context;
            _context = null;
            return result;
        }
#endif

        [Fx.Tag.SecurityNote(Critical = "Calls into critical static method ScheduleCallback")]
        [SecurityCritical]
        private void ScheduleCallback(Action<object> callback)
        {
            ScheduleCallback(callback, this, LowPriority);
        }

        [SecurityCritical]
        private static class CallbackHelper
        {
            [Fx.Tag.SecurityNote(Critical = "Stores a delegate to a critical method")]
            private static Action<object> s_invokeWithoutContextCallback;

            [Fx.Tag.SecurityNote(Critical = "Stores a delegate to a critical method")]
            private static ContextCallback s_onContextAppliedCallback;

#if NET451
            [Fx.Tag.SecurityNote(Critical = "Stores a delegate to a critical method")]
            private static Action<object> s_invokeWithContextCallback;
            [Fx.Tag.SecurityNote(Critical = "Provides access to a critical field; Initialize it with " +
                "a delegate to a critical method")]
            public static Action<object> InvokeWithContextCallback
            {
                get
                {
                    if (s_invokeWithContextCallback == null)
                    {
                        s_invokeWithContextCallback = new Action<object>(InvokeWithContext);
                    }
                    return s_invokeWithContextCallback;
                }
            }
#endif

            [Fx.Tag.SecurityNote(Critical = "Provides access to a critical field; Initialize it with " +
                "a delegate to a critical method")]
            public static Action<object> InvokeWithoutContextCallback
            {
                get
                {
                    if (s_invokeWithoutContextCallback == null)
                    {
                        s_invokeWithoutContextCallback = new Action<object>(InvokeWithoutContext);
                    }
                    return s_invokeWithoutContextCallback;
                }
            }

            [Fx.Tag.SecurityNote(Critical = "Provides access to a critical field; Initialize it with " +
                "a delegate to a critical method")]
            public static ContextCallback OnContextAppliedCallback
            {
                get
                {
                    if (s_onContextAppliedCallback == null)
                    {
                        s_onContextAppliedCallback = new ContextCallback(OnContextApplied);
                    }
                    return s_onContextAppliedCallback;
                }
            }

#if NET451
            [Fx.Tag.SecurityNote(Critical = "Called by the scheduler without any user context on the stack")]
            private static void InvokeWithContext(object state)
            {
                SecurityContext context = ((ActionItem)state).ExtractContext();
                SecurityContext.Run(context, OnContextAppliedCallback, state);
            }
#endif

            [Fx.Tag.SecurityNote(Critical = "Called by the scheduler without any user context on the stack")]
            private static void InvokeWithoutContext(object state)
            {
                var tempState = (ActionItem)state;
                tempState.Invoke();
                tempState._isScheduled = false;
            }

            [Fx.Tag.SecurityNote(Critical = "Called after applying the user context on the stack")]
            private static void OnContextApplied(object o)
            {
                var tempState = (ActionItem)o;
                tempState.Invoke();
                tempState._isScheduled = false;
            }
        }

        private class DefaultActionItem : ActionItem
        {
            [Fx.Tag.SecurityNote(Critical = "Stores a delegate that will be called later, at a particular context")]
            [SecurityCritical]
            private readonly Action<object> _callback;

            [Fx.Tag.SecurityNote(Critical = "Stores an object that will be passed to the delegate that will be " +
                "called later, at a particular context")]
            [SecurityCritical]
            private readonly object _state;

            [Fx.Tag.SecurityNote(Critical = "Access critical fields callback and state",
                Safe = "Doesn't leak information or resources")]
            public DefaultActionItem(Action<object> callback, object state, bool isLowPriority)
            {
                Fx.Assert(callback != null, "Shouldn't instantiate an object to wrap a null callback");
                LowPriority = isLowPriority;
                _callback = callback;
                _state = state;
            }

            [Fx.Tag.SecurityNote(Critical = "Implements a the critical abstract ActionItem.Invoke method, " +
                "Access critical fields callback and state")]
            [SecurityCritical]
            protected override void Invoke()
            {
                _callback(_state);
            }
        }
    }
}
