// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Authentication;

namespace Microsoft.Azure.Devices
{
    internal static class Fx
    {
        internal static bool IsFatal(Exception ex)
        {
            while (ex != null)
            {
                if (ex is OutOfMemoryException
                    || ex is SEHException
                    || ex is NullReferenceException)
                {
                    return true;
                }

                // These exceptions aren't themselves fatal, but since the CLR uses them to wrap other exceptions,
                // we want to check to see whether they've been used to wrap a fatal exception. If so, then they
                // count as fatal.
                if (ex is TypeInitializationException
                    || ex is TargetInvocationException)
                {
                    ex = ex.InnerException;
                }
                else if (ex is AggregateException aggEx)
                {
                    // AggregateExceptions have a collection of inner exceptions, which may themselves be other
                    // wrapping exceptions (including nested AggregateExceptions).  Recursively walk this
                    // hierarchy.  The (singular) InnerException is included in the collection.
                    ReadOnlyCollection<Exception> innerExceptions = aggEx.InnerExceptions;
                    foreach (Exception innerException in innerExceptions)
                    {
                        if (IsFatal(innerException))
                        {
                            return true;
                        }
                    }

                    break;
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        internal static bool ContainsAuthenticationException(Exception ex)
        {
            return ex != null
                && (ex is AuthenticationException
                    || ContainsAuthenticationException(ex.InnerException));
        }
    }
}
