// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace Microsoft.Azure.Devices.Common
{
    /// <summary>
    /// Extension methods for <see cref="Exception"/> class.
    /// </summary>
    [Obsolete("Not recommended for external use.")]
    public static class ExceptionExtensions
    {
        private const string ExceptionIdentifierName = "ExceptionId";

        /// <summary>
        /// Indicates whether the exception is considered fatal.
        /// </summary>
        /// <param name="exception">The exception to test.</param>
        /// <returns>True if the exception is considered fatal; otherwise, false.</returns>
        public static bool IsFatal(this Exception exception)
        {
            return Fx.IsFatal(exception);
        }

        /// <summary>
        /// Unwinds the inner exceptions from a given exception.
        /// </summary>
        /// <param name="exception">The exception to unwind.</param>
        /// <returns>The list of inner exceptions within the specified exception.</returns>
        public static IEnumerable<Exception> Unwind(this Exception exception)
        {
            while (exception != null)
            {
                yield return exception;
                exception = exception.InnerException;
            }
        }

        /// <summary>
        /// Unwinds the inner exceptions from a given exception.
        /// </summary>
        /// <param name="exception">The exception to unwind.</param>
        /// <param name="targetTypes">The list of types that the inner exception should be an instance of.</param>
        /// <returns>The list of inner exceptions within the specified exception that are an instance of the supplied target types.</returns>
        public static IEnumerable<Exception> Unwind(this Exception exception, params Type[] targetTypes)
        {
            return exception.Unwind().Where(e => targetTypes.Any(t => t.IsInstanceOfType(e)));
        }

        /// <summary>
        /// Unwinds the inner exceptions from a given exception.
        /// </summary>
        /// <typeparam name="TException">The type of exception that the inner exception should be an instance of.</typeparam>
        /// <param name="exception">The exception to unwind.</param>
        /// <returns>The list of inner exceptions within the specified exception that are an instance of the supplied target type.</returns>
        public static IEnumerable<TException> Unwind<TException>(this Exception exception)
        {
            return exception.Unwind().OfType<TException>();
        }

        /// <summary>
        /// Stringify the exception, containing all relevant fields.
        /// </summary>
        /// <param name="exception">The exception to stringify.</param>
        /// <returns>The stringified exception.</returns>
        public static string ToStringSlim(this Exception exception)
        {
            // exception.Data is empty collection by default.
            if (exception.Data != null && exception.Data.Contains(ExceptionIdentifierName))
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "ExceptionId: {0}-{1}: {2}",
                    exception.Data[ExceptionIdentifierName],
                    exception.GetType(),
                    exception.Message);
            }
            else if (exception.Data != null)
            {
                string exceptionIdentifier = Guid.NewGuid().ToString();
                exception.Data[ExceptionIdentifierName] = exceptionIdentifier;

                return string.Format(CultureInfo.InvariantCulture,
                    "ExceptionId: {0}-{1}",
                    exceptionIdentifier,
                    exception.ToString());
            }

            // In case Data collection in the exception is nullified.
            return exception.ToString();
        }

        /// <summary>
        /// Gets the exception Id from the given exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>The exception Id.</returns>
        public static string GetReferenceCode(this Exception exception)
        {
            return exception.Data != null && exception.Data.Contains(ExceptionIdentifierName)
                ? (string)exception.Data[ExceptionIdentifierName]
                : null;
        }

        /// <summary>
        /// Indicates if the supplied exception is an IoT Hub exception and its error code belongs to the supplied error code list.
        /// </summary>
        /// <param name="ex">The exception to test.</param>
        /// <param name="errorCodeList">The list containing possible error codes.</param>
        /// <returns>True if the exception is an IoT Hub exception, and its error code is present in the supplied error code list; otherwise, false.</returns>
        public static bool CheckIotHubErrorCode(this Exception ex, params ErrorCode[] errorCodeList)
        {
            foreach (ErrorCode errorCode in errorCodeList)
            {
                if (ex is IotHubException exception && exception.Code == errorCode)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
