// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    internal static class ExceptionAssertions
    {
        public static async Task<TException> ExpectedAsync<TException>(this Task action) where TException : Exception
        {
            try
            {
                await action.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.Should().BeAssignableTo<TException>(e.ToString());
                return (TException)e;
            }

            throw new AssertFailedException($"An exception of type \"{typeof(TException)}\" was expected, but none was thrown.");
        }
    }
}
