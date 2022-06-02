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
    public static class ExceptionExtensions
    {
        private const string ExceptionIdentifierName = "ExceptionId";

        /// <summary>
        /// Indicates whether the exception is considered fatal.
        /// </summary>
        /// <param name="exception">The exception to test.</param>
        /// <returns>True if the exception is considered fatal; otherwise, false.</returns>
        internal static bool IsFatal(this Exception exception)
        {
            return Fx.IsFatal(exception);
        }

        /// <summary>
        /// Stringify the exception, containing all relevant fields.
        /// </summary>
        /// <param name="exception">The exception to stringify.</param>
        /// <returns>The stringified exception.</returns>
        internal static string ToStringSlim(this Exception exception)
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
    }
}
