// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Devices.Client.Extensions
{
    internal static class ExceptionExtensions
    {
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
    }
}
