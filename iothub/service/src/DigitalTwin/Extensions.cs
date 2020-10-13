// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Devices.Extensions
{
    internal static class Extensions
    {
        public const string ParameterNullErrorMessageFormat = "The parameter named {0} can't be null.";
        public const string ParameterNullWhiteSpaceErrorMessageFormat = "The parameter named {0} can't be null, empty or white space.";

        /// <summary>
        /// Throw ArgumentNullException if the value is null reference.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">The argument name.</param>
        public static void ThrowIfNull(this object argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                string errorMessage = string.Format(CultureInfo.InvariantCulture, ParameterNullErrorMessageFormat, argumentName);
                throw new ArgumentNullException(argumentName, errorMessage);
            }
        }

        /// <summary>
        /// Throw ArgumentNullException if the value is null reference, empty or white space.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">The argument name.</param>
        internal static void ThrowIfNullOrWhiteSpace(this string argumentValue, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(argumentValue))
            {
                string errorMessage = string.Format(CultureInfo.InvariantCulture, ParameterNullWhiteSpaceErrorMessageFormat, argumentName);
                throw new ArgumentNullException(argumentName, errorMessage);
            }
        }

        /// <summary>
        /// Append the "$metadata" identifier to the property values, which helps the service identify the patch as a component update.
        /// </summary>
        /// <param name="propertyKeyValuePairs">The dictionary of property key values pairs to update to.</param>
        internal static void AddComponentUpdateIdentifier(this Dictionary<string, object> propertyKeyValuePairs)
        {
            propertyKeyValuePairs.ThrowIfNull(nameof(propertyKeyValuePairs));

            const string metadataKey = "$metadata";
            propertyKeyValuePairs.Add(metadataKey, new object());
        }
    }
}
