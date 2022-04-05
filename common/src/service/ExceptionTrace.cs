// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;

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
        /// When throwing ObjectDisposedException, it is highly recommended that you use this ctor: public ObjectDisposedException(string objectName, string message)
        /// and provide null for objectName, but a meaningful and relevant message for message.
        /// It is recommended because end user really does not care or can do anything on the disposed object, commonly an internal or private object.
        /// </summary>
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

            return exception;
        }

        public static string GetDetailsForThrownException(Exception e)
        {
            var details = new StringBuilder(e.GetType().ToString());
            details.AppendLine("Exception ToString:");
            details.Append(e.ToStringSlim());

            return details.ToString();
        }
    }
}
