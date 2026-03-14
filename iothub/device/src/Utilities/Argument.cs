// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Argument validation.
    /// </summary>
    /// <remarks>
    /// This class should contain only common argument validation.
    /// Be sure to document exceptions thrown by these methods on your public methods.
    /// </remarks>
    internal static class Argument
    {
        /// <summary>
        /// Throws if <paramref name="value"/> is null.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        internal static void AssertNotNull<T>(T value, string name)
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
        internal static void AssertNotNullOrWhiteSpace(string value, string name)
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
        /// Checks an argument to ensure that its value doesn't exceed the specified ceiling baseline.
        /// </summary>
        /// <param name="argumentValue">The value of the argument.</param>
        /// <param name="ceilingValue">The ceiling value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void AssertNotGreaterThan(double argumentValue, double ceilingValue, string argumentName)
        {
            if (argumentValue > ceilingValue)
            {
                throw new ArgumentOutOfRangeException(
                    argumentName,
                    argumentValue,
                    $"The value of '{argumentName}' cannot be greater than '{ceilingValue}'. It is currently '{argumentValue}'.");
            }
        }

        /// <summary>
        /// Checks an argument to ensure that its 64-bit signed value isn't negative.
        /// </summary>
        /// <param name="argumentValue">The value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        internal static void AssertNotNegativeValue(long argumentValue, string argumentName)
        {
            if (argumentValue < 0)
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
    }
}
