// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Devices.Client
{
    internal static class Fx
    {
        private static readonly Type[] s_fatalExceptionTypes = new[]
        {
            typeof(OperationCanceledException),
            typeof(ObjectDisposedException),
            typeof(ArgumentException),
            typeof(OutOfMemoryException),
            typeof(SEHException),
            typeof(NullReferenceException),
        };

        internal static bool IsFatal(Exception ex)
        {
            if (s_fatalExceptionTypes.Any(fatalType => fatalType.IsAssignableFrom(ex.GetType())))
            {
                return true;
            }

            // These exceptions aren't themselves fatal, but since the CLR uses them to wrap other exceptions,
            // we want to check to see whether they've been used to wrap a fatal exception. If so, then they
            // count as fatal.
            if (ex is TypeInitializationException
                || ex is TargetInvocationException)
            {
                return IsFatal(ex.InnerException);
            }
            else if (ex is AggregateException aggEx)
            {
                // AggregateExceptions have a collection of inner exceptions, which may themselves be other
                // wrapping exceptions (including nested AggregateExceptions). Recursively walk this
                // hierarchy. The (singular) InnerException is included in the collection.
                return aggEx.InnerExceptions.Any(ie => IsFatal(ie));
            }

            return false;
        }
    }
}
