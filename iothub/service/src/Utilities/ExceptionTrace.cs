// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
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

        [Fx.Tag.SecurityNote(Critical = "Calls 'System.Runtime.Interop.UnsafeNativeMethods.IsDebuggerPresent()' which is a P/Invoke method",
            Safe = "Does not leak any resource, needed for debugging")]
        public TException TraceException<TException>(TException ex, TraceEventType level)
            where TException : Exception
        {
            if (!ex.Data.Contains(_eventSourceName))
            {
                // Only trace if this is the first time an exception is thrown by this ExceptionTrace/EventSource.
                ex.Data[_eventSourceName] = _eventSourceName;

                switch (level)
                {
                    case TraceEventType.Critical:
                    case TraceEventType.Error:
                        Trace.TraceError("An Exception is being thrown: {0}", GetDetailsForThrownException(ex));
                        break;

                    case TraceEventType.Warning:
                        Trace.TraceWarning("An Exception is being thrown: {0}", GetDetailsForThrownException(ex));
                        break;
                }
            }

            return ex;
        }

        public static string GetDetailsForThrownException(Exception ex)
        {
            var details = new StringBuilder(ex.GetType().ToString());
            details.AppendLine("Exception ToString:");
            details.Append(ToStringSlim(ex));

            return details.ToString();
        }


        /// <summary>
        /// Stringify the exception, containing all relevant fields.
        /// </summary>
        /// <param name="ex">The exception to stringify.</param>
        /// <returns>The stringified exception.</returns>
        private static string ToStringSlim(Exception ex)
        {
            const string exceptionIdentifierName = "ExceptionId";

            // exception.Data is empty collection by default.
            if (ex.Data != null && ex.Data.Contains(exceptionIdentifierName))
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "ExceptionId: {0}-{1}: {2}",
                    ex.Data[exceptionIdentifierName],
                    ex.GetType(),
                    ex.Message);
            }
            else if (ex.Data != null)
            {
                string exceptionIdentifier = Guid.NewGuid().ToString();
                ex.Data[exceptionIdentifierName] = exceptionIdentifier;

                return string.Format(CultureInfo.InvariantCulture,
                    "ExceptionId: {0}-{1}",
                    exceptionIdentifier,
                    ex.ToString());
            }

            // In case Data collection in the exception is nullified.
            return ex.ToString();
        }
    }
}
