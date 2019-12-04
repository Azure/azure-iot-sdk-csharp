// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Azure.IoT.DigitalTwin.Device.Model;

namespace Azure.IoT.DigitalTwin.Device.Helper
{
    /// <summary>
    /// The Guard Helper.
    /// </summary>
    internal static class GuardHelper
    {
        /// <summary>
        /// Throw ArgumentNullException if the value is null reference.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">The argument name.</param>
        internal static void ThrowIfNull(object argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                var errorMessage = string.Format(CultureInfo.InvariantCulture, DigitalTwinConstants.ParameterNullErrorMessageFormat, argumentName);
                throw new ArgumentNullException(argumentName, errorMessage);
            }
        }

        /// <summary>
        /// Throw ArgumentNullException if the value is null reference, empty string or white space.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">The argument name.</param>
        internal static void ThrowIfNullOrWhiteSpace(string argumentValue, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(argumentValue))
            {
                var errorMessage = string.Format(CultureInfo.InvariantCulture, DigitalTwinConstants.ParameterNullWhiteSpaceErrorMessageFormat, argumentName);
                throw new ArgumentNullException(argumentName, errorMessage);
            }
        }

        /// <summary>
        /// Throw ArgumentException if the value is invalid interface id.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">The argument name.</param>
        internal static void ThrowIfInvalidInterfaceId(string argumentValue, string argumentName)
        {
            var regex = new Regex("^urn:[a-zA-z0-9_:]{1,252}$");

            if (!regex.IsMatch(argumentValue))
            {
                throw new ArgumentException(DigitalTwinConstants.InvalidInterfaceIdErrorMessage, argumentName);
            }
        }

        internal static void ThrowIfInvalidInterfaceInstanceName(string argumentValue, string argumentName)
        {
            var regex = new Regex("^[a-zA-z0-9_]{1,256}$");

            if (!regex.IsMatch(argumentValue))
            {
                throw new ArgumentException(DigitalTwinConstants.InvalidInterfaceInstanceNameErrorMessage, argumentName);
            }
        }
    }
}
