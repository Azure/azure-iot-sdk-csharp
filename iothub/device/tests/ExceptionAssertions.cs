// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    public static class ExceptionAssertions
    {
        public static async Task<TException> ExpectedAsync<TException>(this Task action) where TException : Exception
        {
            try
            {
                await action.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(TException), e.ToString());
                return (TException)e;
            }

            throw new AssertFailedException($"An exception of type \"{typeof(TException)}\" was expected, but none was thrown.");
        }

        public static async Task<TException> ExpectedAsync<TException>(this Func<Task> action) where TException : Exception
        {
            return (TException)await ExpectedAsync(action, typeof(TException)).ConfigureAwait(false);
        }

        public static async Task<Exception> ExpectedAsync(this Func<Task> action, Type exceptionType)
        {
            await Task.Yield();
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, exceptionType, e.ToString());
                return e;
            }

            throw new AssertFailedException($"An exception of type \"{exceptionType}\" was expected, but none was thrown.");
        }
    }
}
