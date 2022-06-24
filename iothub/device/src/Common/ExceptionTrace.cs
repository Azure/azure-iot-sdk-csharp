// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Devices.Client.Extensions;

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

        [Fx.Tag.SecurityNote(Critical = "Calls 'System.Runtime.Interop.UnsafeNativeMethods.IsDebuggerPresent()' which is a P/Invoke method",
            Safe = "Does not leak any resource, needed for debugging")]
        public TException TraceException<TException>(TException exception, TraceEventType level)
            where TException : Exception
        {
            if (!exception.Data.Contains(_eventSourceName))
            {
                // Only trace if this is the first time an exception is thrown by this ExceptionTrace/EventSource.
                exception.Data[_eventSourceName] = _eventSourceName;
            }

            return exception;
        }
    }
}
