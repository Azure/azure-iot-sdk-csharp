// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public static class ExceptionAssertions
    {
        public static Exception Expected(this Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                return e;
            }

            throw new AssertFailedException("An exception was expected, but none was thrown.");
        }

        public static TArgumentException WithArgumentName<TArgumentException>(this TArgumentException exception, string parameter) where TArgumentException : ArgumentException
        {
            Assert.AreEqual(parameter, exception.ParamName);

            return exception;
        }

        public static TException WithException<TException>(this Exception exception, string message = null) where TException : Exception
        {
            Assert.IsInstanceOfType(exception, typeof(TException));

            return (TException)exception;
        }

        public static WebException WithResponseUri(this WebException exception, Uri uri)
        {
            Assert.IsInstanceOfType(exception.Response, typeof(HttpWebResponse));

            var response = (HttpWebResponse)exception.Response;
            Assert.AreEqual(uri, response.ResponseUri);

            return exception;
        }

        public static TException WithFirstInnerException<TException>(this AggregateException exception) where TException : Exception
        {
            return exception.Flatten().InnerExceptions.OfType<TException>().First();
        }

        public static WebException WithStatusCode(this WebException exception, HttpStatusCode statusCode, string statusDescription = null)
        {
            Assert.IsInstanceOfType(exception.Response, typeof(HttpWebResponse));

            var response = (HttpWebResponse)exception.Response;
            Assert.AreEqual(statusCode, response.StatusCode);

            if (statusDescription != null)
            {
                Assert.AreEqual(statusDescription, response.StatusDescription);
            }

            return exception;
        }

        public static async Task<TException> ExpectedAsync<TException>(this Task action) where TException : Exception
        {
            try
            {
                await action;
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
            return (TException)await ExpectedAsync(action, typeof(TException));
        }

        public static async Task<Exception> ExpectedAsync(this Func<Task> action, Type exceptionType)
        {
            await Task.Yield();
            try
            {
                await action();
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, exceptionType, e.ToString());
                return e;
            }

            throw new AssertFailedException($"An exception of type \"{exceptionType}\" was expected, but none was thrown.");
        }

        public static Task<TException> WithAssertion<TException>(this Task<TException> exception, Action<TException> assert) where TException : Exception
        {
            return exception.ContinueWith(e =>
            {
                assert(e.Result);
                return e.Result;
            });
        }

        public static Task<TException> WithErrorCode<TException>(this Task<TException> exception, uint errorCode) where TException : HttpListenerException
        {
            return exception.ContinueWith(e =>
            {
                Assert.AreEqual(errorCode, e.Result.ErrorCode);
                return e.Result;
            });
        }
    }
}