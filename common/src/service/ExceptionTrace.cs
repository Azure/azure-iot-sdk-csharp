// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root
// for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using Microsoft.Azure.Devices.Common.Tracing;

namespace Microsoft.Azure.Devices.Common
{
    internal class ExceptionTrace
    {
        private readonly string _eventSourceName;

        public ExceptionTrace(string eventSourceName)
        {
            _eventSourceName = eventSourceName;
        }

        public Exception AsError(Exception exception, EventTraceActivity activity = null)
        {
            return TraceException(exception, TraceEventType.Error, activity);
        }

        public Exception AsInformation(Exception exception, EventTraceActivity activity = null)
        {
            return TraceException(exception, TraceEventType.Information, activity);
        }

        public Exception AsWarning(Exception exception, EventTraceActivity activity = null)
        {
            return TraceException(exception, TraceEventType.Warning, activity);
        }

        public Exception AsVerbose(Exception exception, EventTraceActivity activity = null)
        {
            return TraceException(exception, TraceEventType.Verbose, activity);
        }

        public ArgumentException Argument(string paramName, string message)
        {
            return TraceException(new ArgumentException(message, paramName), TraceEventType.Error);
        }

        public ArgumentNullException ArgumentNull(string paramName)
        {
            return TraceException(new ArgumentNullException(paramName), TraceEventType.Error);
        }

        public ArgumentNullException ArgumentNull(string paramName, string message)
        {
            return TraceException(new ArgumentNullException(paramName, message), TraceEventType.Error);
        }

        public ArgumentException ArgumentNullOrEmpty(string paramName)
        {
            return Argument(paramName, CommonResources.GetString(CommonResources.ArgumentNullOrEmpty, paramName));
        }

        public ArgumentException ArgumentNullOrWhiteSpace(string paramName)
        {
            return Argument(paramName, CommonResources.GetString(CommonResources.ArgumentNullOrWhiteSpace, paramName));
        }

        public ArgumentOutOfRangeException ArgumentOutOfRange(string paramName, object actualValue, string message)
        {
            return TraceException(new ArgumentOutOfRangeException(paramName, actualValue, message), TraceEventType.Error);
        }

        /// <summary>
        /// When throwing ObjectDisposedException, it is highly recommended that you use this ctor: public ObjectDisposedException(string objectName, string message)
        /// and provide null for objectName, but a meaningful and relevant message for message.
        /// It is recommended because end user really does not care or can do anything on the disposed object, commonly an internal or private object.
        /// </summary>
        public ObjectDisposedException ObjectDisposed(string message)
        {
            // pass in null, not disposedObject.GetType().FullName as per the above guideline
            return TraceException<ObjectDisposedException>(new ObjectDisposedException(null, message), TraceEventType.Error);
        }

        public void TraceHandled(Exception exception, string catchLocation, EventTraceActivity activity = null)
        {
#if NET451 && DEBUG
            Trace.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "IotHub/TraceHandled ThreadID=\"{0}\" catchLocation=\"{1}\" exceptionType=\"{2}\" exception=\"{3}\"",
                Thread.CurrentThread.ManagedThreadId,
                catchLocation,
                exception.GetType(),
                exception.ToStringSlim()));
#endif

            ////MessagingClientEtwProvider.Provider.HandledExceptionWithFunctionName(
            ////    activity, catchLocation, exception.ToStringSlim(), string.Empty);

            BreakOnException(exception);
        }

        public void TraceUnhandled(Exception exception)
        {
            ////MessagingClientEtwProvider.Provider.EventWriteUnhandledException(this.eventSourceName + ": " + exception.ToStringSlim());
        }

#if NET451
        [ResourceConsumption(ResourceScope.Process)]
#endif

        [Fx.Tag.SecurityNote(Critical = "Calls 'System.Runtime.Interop.UnsafeNativeMethods.IsDebuggerPresent()' which is a P/Invoke method",
            Safe = "Does not leak any resource, needed for debugging")]
        public TException TraceException<TException>(TException exception, TraceEventType level, EventTraceActivity activity = null)
            where TException : Exception
        {
            if (!exception.Data.Contains(_eventSourceName))
            {
                // Only trace if this is the first time an exception is thrown by this ExceptionTrace/EventSource.
                exception.Data[_eventSourceName] = _eventSourceName;

                switch (level)
                {
                    case TraceEventType.Critical:
                    case TraceEventType.Error:
                        Trace.TraceError("An Exception is being thrown: {0}", GetDetailsForThrownException(exception));
                        ////if (MessagingClientEtwProvider.Provider.IsEnabled(
                        ////        EventLevel.Error,
                        ////        MessagingClientEventSource.Keywords.Client,
                        ////        MessagingClientEventSource.Channels.DebugChannel))
                        ////{
                        ////    MessagingClientEtwProvider.Provider.ThrowingExceptionError(activity, GetDetailsForThrownException(exception));
                        ////}

                        break;

                    case TraceEventType.Warning:
                        Trace.TraceWarning("An Exception is being thrown: {0}", GetDetailsForThrownException(exception));
                        ////if (MessagingClientEtwProvider.Provider.IsEnabled(
                        ////        EventLevel.Warning,
                        ////        MessagingClientEventSource.Keywords.Client,
                        ////        MessagingClientEventSource.Channels.DebugChannel))
                        ////{
                        ////    MessagingClientEtwProvider.Provider.ThrowingExceptionWarning(activity, GetDetailsForThrownException(exception));
                        ////}

                        break;

                    default:
#if DEBUG
                        ////if (MessagingClientEtwProvider.Provider.IsEnabled(
                        ////        EventLevel.Verbose,
                        ////        MessagingClientEventSource.Keywords.Client,
                        ////        MessagingClientEventSource.Channels.DebugChannel))
                        ////{
                        ////    MessagingClientEtwProvider.Provider.ThrowingExceptionVerbose(activity, GetDetailsForThrownException(exception));
                        ////}
#endif

                        break;
                }
            }

            BreakOnException(exception);
            return exception;
        }

        public static string GetDetailsForThrownException(Exception e)
        {
            var details = new StringBuilder(e.GetType().ToString());

#if NET451
            const int MaxStackFrames = 10;

            // Include the current callstack (this ensures we see the Stack in case exception is not output when caught)
            var stackTrace = new StackTrace();
            string stackTraceString = stackTrace.ToString();
            if (stackTrace.FrameCount > MaxStackFrames)
            {
                string[] frames = stackTraceString.Split(new[] { Environment.NewLine }, MaxStackFrames + 1, StringSplitOptions.RemoveEmptyEntries);
                stackTraceString = string.Join(Environment.NewLine, frames, 0, MaxStackFrames) + "...";
            }

            details.Append(Environment.NewLine);
            details.AppendLine(stackTraceString);
#endif
            details.AppendLine("Exception ToString:");
            details.Append(e.ToStringSlim());

            return details.ToString();
        }

        [SuppressMessage(FxCop.Category.Performance, FxCop.Rule.MarkMembersAsStatic, Justification = "CSDMain #183668")]
        [Fx.Tag.SecurityNote(Critical = "Calls into critical method UnsafeNativeMethods.IsDebuggerPresent and UnsafeNativeMethods.DebugBreak",
            Safe = "Safe because it's a no-op in retail builds.")]
        internal void BreakOnException(Exception exception)
        {
#if DEBUG

            if (Fx.BreakOnExceptionTypes != null)
            {
                foreach (Type breakType in Fx.BreakOnExceptionTypes)
                {
#if NET451
                    if (breakType.IsAssignableFrom(exception.GetType()))
                    {
                        // This is intended to "crash" the process so that a debugger can be attached. If a managed
                        // debugger is already attached, it will already be able to hook these exceptions. We don't want
                        // to simulate an unmanaged crash (DebugBreak) in that case.
                        if (!Debugger.IsAttached && !Interop.UnsafeNativeMethods.IsDebuggerPresent())
                        {
                            Debugger.Launch();
                        }
                    }
#endif
                }
            }

#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void TraceFailFast(string message)
        {
            ////            EventLogger logger = null;
            ////#pragma warning disable 618
            ////            logger = new EventLogger(this.seventSourceName, Fx.Trace);
            ////#pragma warning restore 618
            ////            TraceFailFast(message, logger);
        }

        // Generate an event Log entry for failfast purposes To force a Watson on a dev machine, do the following:
        // 1. Set \HKLM\SOFTWARE\Microsoft\PCHealth\ErrorReporting ForceQueueMode = 0
        // 2. In the command environment, set COMPLUS_DbgJitDebugLaunchSetting=0
        ////[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.MarkMembersAsStatic, Justification = "CSDMain #183668")]
        ////[MethodImpl(MethodImplOptions.NoInlining)]
        ////internal void TraceFailFast(string message, EventLogger logger)
        ////{
        ////    if (logger != null)
        ////    {
        ////        try
        ////        {
        ////            string stackTrace = null;
        ////            try
        ////            {
        ////                stackTrace = new StackTrace().ToString();
        ////            }
        ////            catch (Exception exception)
        ////            {
        ////                stackTrace = exception.Message;
        ////                if (Fx.IsFatal(exception))
        ////                {
        ////                    throw;
        ////                }
        ////            }
        ////            finally
        ////            {
        ////                logger.LogEvent(TraceEventType.Critical,
        ////                    FailFastEventLogCategory,
        ////                    (uint)EventLogEventId.FailFast,
        ////                    message,
        ////                    stackTrace);
        ////            }
        ////        }
        ////        catch (Exception ex)
        ////        {
        ////            logger.LogEvent(TraceEventType.Critical,
        ////                FailFastEventLogCategory,
        ////                (uint)EventLogEventId.FailFastException,
        ////                ex.ToString());
        ////            if (Fx.IsFatal(ex))
        ////            {
        ////                throw;
        ////            }
        ////        }
        ////    }
        ////}
    }
}
