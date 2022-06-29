// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Devices.Provisioning.Client.Extensions
{
    internal static class CommonExtensions
    {
        /// <summary>
        /// Throw ArgumentNullException if the value is null reference, empty or white space.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">The argument name.</param>
        internal static void ThrowIfNullOrWhiteSpace(this string argumentValue, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(argumentValue))
            {
                string errorMessage = $"The parameter named {argumentName} can't be null, empty or white space.";
                throw new ArgumentNullException(argumentName, errorMessage);
            }
        }
    }
}
