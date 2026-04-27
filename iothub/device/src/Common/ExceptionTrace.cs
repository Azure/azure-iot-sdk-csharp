// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Devices.Client.Extensions;

#if NET451
using System.Globalization;
using System.Runtime.Versioning;
using System.Threading;
#endif

namespace Microsoft.Azure.Devices.Client
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
            return TraceException<Exception>(exception, TraceEventType.Error);
        }

        public Exception AsInformation(Exception exception)
        {
            return TraceException<Exception>(exception, TraceEventType.Information);
        }

        public Exception AsWarning(Exception exception)
        {
            return TraceException<Exception>(exception, TraceEventType.Warning);
        }

        public Exception AsVerbose(Exception exception)
        {
            return TraceException<Exception>(exception, TraceEventType.Verbose);
        }

        public ArgumentException Argument(string paramName, string message)
        {
            return TraceException<ArgumentException>(new ArgumentException(message, paramName), TraceEventType.Error);
        }

        public ArgumentNullException ArgumentNull(string paramName)
        {
            return TraceException<ArgumentNullException>(new ArgumentNullException(paramName), TraceEventType.Error);
        }

        public ArgumentNullException ArgumentNull(string paramName, string message)
        {
            return TraceException<ArgumentNullException>(new ArgumentNullException(paramName, message), TraceEventType.Error);
        }

        public ArgumentOutOfRangeException ArgumentOutOfRange(string paramName, object actualValue, string message)
        {
            return TraceException(new ArgumentOutOfRangeException(paramName, actualValue, message), TraceEventType.Error);
        }

        // When throwing ObjectDisposedException, it is highly recommended that you use this ctor
        // [C#]
        // public ObjectDisposedException(string objectName, string message);
        // And provide null for objectName but meaningful and relevant message for message.
        // It is recommended because end user really does not care or can do anything on the disposed object, commonly an internal or private object.
        public ObjectDisposedException ObjectDisposed(string message)
        {
            // pass in null, not disposedObject.GetType().FullName as per the above guideline
            return TraceException<ObjectDisposedException>(new ObjectDisposedException(null, message), TraceEventType.Error);
        }

        [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Unused parameter catchLocation is inside of the DEBUG compilation flag.")]
        public void TraceHandled(Exception exception, string catchLocation)
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

#if NET451
                switch (level)
                {
                    case TraceEventType.Critical:
                    case TraceEventType.Error:
                        Trace.TraceError($"An Exception is being thrown: {GetDetailsForThrownException(exception)}");
                        break;

                    case TraceEventType.Warning:
                        Trace.TraceWarning($"An Exception is being thrown: {GetDetailsForThrownException(exception)}");
                        break;
                }
#endif
            }

            BreakOnException(exception);
            return exception;
        }

        public static string GetDetailsForThrownException(Exception e)
        {
            string details = e.GetType().ToString();

#if NET451
            const int maxStackFrames = 10;
            // Include the current callstack (this ensures we see the Stack in case exception is not output when caught)
            var stackTrace = new StackTrace();
            string stackTraceString = stackTrace.ToString();

            if (stackTrace.FrameCount > maxStackFrames)
            {
                string[] frames = stackTraceString.Split(new[] { Environment.NewLine }, maxStackFrames + 1, StringSplitOptions.RemoveEmptyEntries);
                stackTraceString = string.Join(Environment.NewLine, frames, 0, maxStackFrames) + "...";
            }

            details += Environment.NewLine + stackTraceString;
#endif
            details += Environment.NewLine + "Exception ToString:" + Environment.NewLine;
            details += e.ToStringSlim();
            return details;
        }

        [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Unused parameters are inside of the NET451 compilation flag.")]
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
                        // This is intended to "crash" the process so that a debugger can be attached.  If a managed
                        // debugger is already attached, it will already be able to hook these exceptions.  We don't
                        // want to simulate an unmanaged crash (DebugBreak) in that case.
                        if (!Debugger.IsAttached && !UnsafeNativeMethods.IsDebuggerPresent())
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
