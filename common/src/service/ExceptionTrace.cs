// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Azure.Devices.Common.Tracing;

#if NET451
using System.Globalization;
using System.Runtime.Versioning;
using System.Threading;
#endif

namespace Microsoft.Azure.Devices.Common
{
    internal class ExceptionTrace
    {
        private readonly string _eventSourceName;

        public ExceptionTrace(string eventSourceName)
        {
            _eventSourceName = eventSourceName;
        }

        public Exception AsError(Exception exception)
        {
            return TraceException(exception, TraceEventType.Error);
        }

        public Exception AsInformation(Exception exception)
        {
            return TraceException(exception, TraceEventType.Information);
        }

        public Exception AsWarning(Exception exception)
        {
            return TraceException(exception, TraceEventType.Warning);
        }

        public Exception AsVerbose(Exception exception)
        {
            return TraceException(exception, TraceEventType.Verbose);
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
        /// When throwing ObjectDisposedException, it is highly recommended that you use this ctor:
        /// public ObjectDisposedException(string objectName, string message)
        /// and provide null for objectName, but a meaningful and relevant message for message.
        /// It is recommended because end user really does not care or can do anything on the
        /// disposed object, commonly an internal or private object.
        /// </summary>
        public ObjectDisposedException ObjectDisposed(string message)
        {
            // pass in null, not disposedObject.GetType().FullName as per the above guideline
            return TraceException(new ObjectDisposedException(null, message), TraceEventType.Error);
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "Parameter 'catchLocation' used in NET451; remove when no longer supported.")]
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

            BreakOnException(exception);
        }

#if NET451
        [ResourceConsumption(ResourceScope.Process)]
#endif

        [Fx.Tag.SecurityNote(Critical = "Calls 'System.Runtime.Interop.UnsafeNativeMethods.IsDebuggerPresent()' which is a P/Invoke method",
            Safe = "Does not leak any resource, needed for debugging")]
        public TException TraceException<TException>(TException exception, TraceEventType level)
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
                        break;

                    case TraceEventType.Warning:
                        Trace.TraceWarning("An Exception is being thrown: {0}", GetDetailsForThrownException(exception));
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
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "Parameter 'exception' used in NET451; remove when no longer supported.")]
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
    }
}
