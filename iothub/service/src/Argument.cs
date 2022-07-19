// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Argument validation helper class.
    /// </summary>
    internal class Argument
    {
        /// <summary>
        /// Throws if the provided argument is null or empty.
        /// </summary>
        /// <param name="argument">The argument to check if it is null or empty.</param>
        /// <param name="argumentName">The name of the argument</param>
        /// <exception cref="ArgumentNullException">Thrown if the argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the argument is empty.</exception>
        internal static void RequireNotNullOrEmpty(string argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }

            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentException("Argument cannot be null or whitespace", argumentName);
            }
        }

        /// <summary>
        /// Throws if the provided argument is null.
        /// </summary>
        /// <param name="argument">The argument to check if it is null.</param>
        /// <param name="argumentName">The name of the argument</param>
        /// <exception cref="ArgumentNullException">Thrown if the argument is null.</exception>
        internal static void RequireNotNull(object argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Throws if the provided enumerable argument is null or has no entries.
        /// </summary>
        /// <typeparam name="T">The type of the entries in the collection</typeparam>
        /// <param name="argument">The argument to check if it is null or empty.</param>
        /// <param name="argumentName">The name of the argument</param>
        /// <exception cref="ArgumentNullException">Thrown if the enumerable argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the enumerable argument has no entries.</exception>
        internal static void RequireNotNullOrEmpty<T>(IEnumerable<T> argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }

            if (argument.Count() == 0)
            {
                throw new ArgumentException("Collection must have at least one entry", argumentName);
            }
        }

        /// <summary>
        /// Throws if the provided URI argument is null or empty.
        /// </summary>
        /// <param name="argument">The argument to check if it is null or empty.</param>
        /// <param name="argumentName">The name of the argument</param>
        /// <exception cref="ArgumentNullException">Thrown if the argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the argument is empty.</exception>
        internal static void RequireNotNullOrEmpty(Uri argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }

            RequireNotNullOrEmpty(argument.ToString(), argumentName);
        }
    }
}
