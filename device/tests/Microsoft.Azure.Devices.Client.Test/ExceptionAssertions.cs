// ------------------------------------------------------------------------------------------------
// <copyright file="Fail.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------------
namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
#if !NETSTANDARD1_3
    using System.Web.Http;
#endif
    using Microsoft.Azure.Devices.Client;
#if !NUNIT
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using NUnit.Framework;
    using TestClassAttribute = NUnit.Framework.TestFixtureAttribute;
    using TestMethodAttribute = NUnit.Framework.TestAttribute;
    using ClassInitializeAttribute = NUnit.Framework.OneTimeSetUpAttribute;
    using ClassCleanupAttribute = NUnit.Framework.OneTimeTearDownAttribute;
    using TestCategoryAttribute = NUnit.Framework.CategoryAttribute;
    using IgnoreAttribute = MSTestIgnoreAttribute;
    using AssertFailedException = NUnit.Framework.AssertionException;
#endif

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
#if !NUNIT
            Assert.IsInstanceOfType(exception, typeof(TException));
#else
            Assert.That(exception, Is.InstanceOf<TException>());
#endif

            return (TException)exception;
        }

        public static WebException WithResponseUri(this WebException exception, Uri uri)
        {
#if !NUNIT
            Assert.IsInstanceOfType(exception.Response, typeof(HttpWebResponse));
#else
            Assert.That(exception.Response, Is.InstanceOf<HttpWebResponse>());
#endif

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
#if !NUNIT
            Assert.IsInstanceOfType(exception.Response, typeof(HttpWebResponse));
#else
            Assert.That(exception.Response, Is.InstanceOf<HttpWebResponse>());
#endif

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
#if !NUNIT
                Assert.IsInstanceOfType(e, typeof(TException), e.ToString());
#else
                Assert.That(e, Is.InstanceOf<TException>(), e.ToString());
#endif
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
#if !NUNIT
                Assert.IsInstanceOfType(e, exceptionType, e.ToString());
#else
                Assert.That(e, Is.InstanceOf(exceptionType), e.ToString());
#endif
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

#if !NETSTANDARD1_3
        public static Task<HttpResponseException> WithStatusCode(this Task<HttpResponseException> exception, HttpStatusCode statusCode)
        {
            return exception.ContinueWith(e =>
            {
                Assert.AreEqual(statusCode, e.Result.Response.StatusCode);
                return e.Result;
            });
        }

        public static HttpResponseException WithStatusCode(this HttpResponseException exception, HttpStatusCode statusCode)
        {
            Assert.AreEqual(statusCode, exception.Response.StatusCode);
            return exception;
        }
#endif

        public static TException WithSingleInnerException<TException>(this AggregateException exception)
            where TException : Exception
        {
            return exception.Flatten().InnerExceptions.Cast<TException>().Single();
        }
    }
}