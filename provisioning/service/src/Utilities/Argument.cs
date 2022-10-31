// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Argument validation helper class.
    /// </summary>
    internal class Argument
    {
        /// <summary>
        /// Throws if <paramref name="value"/> is null.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public static void AssertNotNull<T>(T value, string name)
        {
            if (value is null)
            {
                throw new ArgumentNullException(name);
            }
        }

        /// <summary>
        /// Throws if <paramref name="value"/> is null, an empty string, or consists only of white-space characters.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is an empty string or consists only of white-space characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public static void AssertNotNullOrWhiteSpace(string value, string name)
        {
            if (value is null)
            {
                throw new ArgumentNullException(name);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty or contain only white-space characters.", name);
            }
        }

        /// <summary>
        /// Checks an argument to ensure that its value isn't negative.
        /// </summary>
        /// <param name="argumentValue">The value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        internal static void AssertNotNegativeValue<T>(T argumentValue, string argumentName)
        {
            // Currently we check "argumentValue" in types of uint and TimeSpan only,
            // and we might need to add check for other types as well in the future.
            if (argumentValue is uint intVal && intVal < 0
                || argumentValue is TimeSpan timeVal && timeVal < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    argumentName,
                    argumentValue,
                    $"The value of '{argumentName}' cannot be negative. It is currently '{argumentValue}'.");
            }
        }

        /// <summary>
        /// Throws if <paramref name="value"/> is null or an empty collection.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is an empty collection.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        internal static void AssertNotNullOrEmpty<T>(IEnumerable<T> value, string name)
        {
            if (value is null)
            {
                throw new ArgumentNullException(name);
            }

            // .NET Framework's Enumerable.Any() always allocates an enumerator, so we optimize for collections here.
            if (value is ICollection<T> collectionOfT
                && collectionOfT.Count == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", name);
            }

            if (value is ICollection collection
                && collection.Count == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", name);
            }

            using IEnumerator<T> e = value.GetEnumerator();
            if (!e.MoveNext())
            {
                throw new ArgumentException("Value cannot be an empty collection.", name);
            }
        }

        /// <summary>
        /// Throws if the provided object argument is null or, when <see cref="object.ToString()"/>
        /// is called, is white space.
        /// </summary>
        /// <param name="argument">The argument to check if it is null or empty.</param>
        /// <param name="argumentName">The name of the argument</param>
        /// <exception cref="ArgumentNullException">Thrown if the argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the argument is empty.</exception>
        internal static void AssertNotNullOrWhiteSpace<T>(T argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }

            AssertNotNullOrWhiteSpace(argument.ToString(), argumentName);
        }

        internal static void ValidateBufferBounds(byte[] buffer, int offset, int size)
        {
            AssertNotNull(buffer, nameof(buffer));

            if (offset < 0 || offset > buffer.Length || size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "The buffer bound is invalid.");
            }

            int remainingBufferSpace = buffer.Length - offset;
            if (size > remainingBufferSpace)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "The buffer bound is invalid.");
            }
        }
    }
}
