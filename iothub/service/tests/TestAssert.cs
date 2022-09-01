﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Api.Test
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.Devices.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public static class TestAssert
    {
        public static TException Throws<TException>(Action action, string errorMessage = null) where TException : Exception
        {
            errorMessage = errorMessage ?? "Failed";
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new AssertFailedException($"{errorMessage}. Expected:{typeof(TException).ToString()} Actual:{ex.GetType().ToString()}", ex);
            }

            throw new AssertFailedException($"{errorMessage}. Expected {typeof(TException).ToString()} exception but no exception is thrown.");
        }

        public static TException Throws<TException>(Func<Task> action, string errorMessage = null) where TException : Exception
        {
            return Throws<TException>(() => action().Wait(), errorMessage);
        }

        public static async Task<TException> ThrowsAsync<TException>(Func<Task> action, string errorMessage = null) where TException : Exception
        {
            errorMessage = errorMessage ?? "Failed";
            try
            {
                await action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new AssertFailedException(
                    $"{errorMessage}. Expected:{typeof(TException).ToString()} Actual:{ex.GetType().ToString()}", ex);
            }

            throw new AssertFailedException($"{errorMessage}. Expected {typeof(TException).ToString()} exception but no exception is thrown");
        }
    }
}
