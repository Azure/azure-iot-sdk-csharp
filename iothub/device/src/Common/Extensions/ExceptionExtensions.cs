// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Microsoft.Azure.Devices.Client.Extensions
{
    internal static class ExceptionExtensions
    {
        private const string ExceptionIdentifierName = "ExceptionId";
        private static MethodInfo prepForRemotingMethodInfo;

        public static bool IsFatal(this Exception exception)
        {
            return Fx.IsFatal(exception);
        }

        public static IEnumerable<Exception> Unwind(this Exception exception, bool unwindAggregate = false)
        {
            while (exception != null)
            {
                yield return exception;
                if (!unwindAggregate)
                {
                    exception = exception.InnerException;
                    continue;
                }
                ReadOnlyCollection<Exception> excepetions = (exception as AggregateException)?.InnerExceptions;
                if (excepetions != null)
                {
                    foreach (Exception ex in excepetions)
                    {
                        foreach (Exception innerEx in ex.Unwind(true))
                        {
                            yield return innerEx;
                        }
                    }
                }
                exception = exception.InnerException;
            }
        }

        public static IEnumerable<TException> Unwind<TException>(this Exception exception)
        {
            return exception.Unwind().OfType<TException>();
        }

        public static Exception PrepareForRethrow(this Exception exception)
        {
            Fx.Assert(exception != null, "The specified Exception is null.");

            if (!ShouldPrepareForRethrow(exception))
            {
                return exception;
            }

            if (PartialTrustHelpers.UnsafeIsInFullTrust())
            {
                // Racing here is harmless
                if (prepForRemotingMethodInfo == null)
                {
#if NET451
                    prepForRemotingMethodInfo =
                        typeof(Exception).GetMethod(
                            "PrepForRemoting",
                            BindingFlags.Instance | BindingFlags.NonPublic,
                            null,
                            new Type[] { },
                            new ParameterModifier[] { });
#else
                    prepForRemotingMethodInfo =
                        typeof(Exception).GetMethod(
                            "PrepForRemoting",
                            BindingFlags.Instance | BindingFlags.NonPublic,
                            null,
                            Array.Empty<Type>(),
                            Array.Empty<ParameterModifier>());
#endif
                }

                if (prepForRemotingMethodInfo != null)
                {
                    // PrepForRemoting is not thread-safe. When the same exception instance is thrown by multiple threads
                    // the remote stack trace string may not format correctly. However, We don't lock this to protect us from it given
                    // it is discouraged to throw the same exception instance from multiple threads and the side impact is ignorable.
#if NET451
                    prepForRemotingMethodInfo.Invoke(exception, new object[] { });
#else
                    prepForRemotingMethodInfo.Invoke(exception, Array.Empty<object>());
#endif
                }
            }

            return exception;
        }

        public static Exception DisablePrepareForRethrow(this Exception exception)
        {
            exception.Data[AsyncResult.DisablePrepareForRethrow] = string.Empty;
            return exception;
        }

        public static string ToStringSlim(this Exception exception)
        {
            // exception.Data is empty collection by default.
            if (exception.Data != null
                && exception.Data.Contains(ExceptionIdentifierName))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "ExceptionId: {0}-{1}: {2}",
                    exception.Data[ExceptionIdentifierName],
                    exception.GetType(),
                    exception.Message);
            }
            else if (exception.Data != null)
            {
                string exceptionIdentifier = Guid.NewGuid().ToString();
                exception.Data[ExceptionIdentifierName] = exceptionIdentifier;

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "ExceptionId: {0}-{1}",
                    exceptionIdentifier,
                    exception.ToString());
            }

            // In case Data collection in the exception is nullified.
            return exception.ToString();
        }

        public static string GetReferenceCode(this Exception exception)
        {
            return exception.Data != null
                && exception.Data.Contains(ExceptionIdentifierName)
                    ? (string)exception.Data[ExceptionIdentifierName]
                    : null;
        }

        private static bool ShouldPrepareForRethrow(Exception exception)
        {
            while (exception != null)
            {
                if (exception.Data != null
                    && exception.Data.Contains(AsyncResult.DisablePrepareForRethrow))
                {
                    return false;
                }

                exception = exception.InnerException;
            }

            return true;
        }

        /// <summary>
        /// Throw ArgumentNullException if the value is null reference.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">The argument name.</param>
        public static void ThrowIfNull(this object argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                string errorMessage = $"The parameter named {argumentName} can't be null.";
                throw new ArgumentNullException(argumentName, errorMessage);
            }
        }

        /// <summary>
        /// Throw ArgumentNullException if the value is null reference, empty, or white space.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">The argument name.</param>
        public static void ThrowIfNullOrWhiteSpace(this string argumentValue, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(argumentValue))
            {
                string errorMessage = $"The parameter named {argumentName} can't be null, empty, or white space.";
                throw new ArgumentNullException(argumentName, errorMessage);
            }
        }
    }
}
