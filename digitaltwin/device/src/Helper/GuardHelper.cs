// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Text.RegularExpressions;

using Azure.Iot.DigitalTwin.Device.Model;

namespace Azure.Iot.DigitalTwin.Device.Helper
{
    /// <summary>
    /// The Guard Helper.
    /// </summary>
    internal static class GuardHelper
    {
        private static readonly TimeSpan s_regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        private static readonly Regex s_interfaceIdPatternRegex = new Regex(@"^(http|https|ftp|file)\://[a-z0-9]+(\.[a-zA-Z0-9]*)+(/[a-z0-9]+)+/(\d+\.)?(\d+\.)?(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);
        private const int interfaceMaxLenght = 256;

        /// <summary>
        /// Throw ArgumentNullException if the value is null reference.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">The argument name.</param>
        public static void ThrowIfNull(object argumentValue, string argumentName)
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
        public static void ThrowIfNullOrWhiteSpace(string argumentValue, string argumentName)
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
        public static void ThrowIfInvalidInterfaceId(string argumentValue, string argumentName)
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(argumentValue, argumentName);
            // TODO: update regex
            //if (!s_interfaceIdPatternRegex.IsMatch(argumentValue))
            //{
            //    throw new ArgumentException(DigitalTwinConstants.InvalidInterfaceIdErrorMessage, argumentName);
            //}
        }

        /// <summary>
        /// Throw ArgumentException if the interface id is of invalid length. The maximum allowed size of interface id is 64 ASCII characters.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">The argument name.</param>
        public static void ThrowIfInterfaceIdLengthInvalid(string argumentValue, string argumentName)
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(argumentValue, argumentName);
            if (argumentValue.Length > DigitalTwinConstants.MaxInterfaceIdLength)
            {
                throw new ArgumentException(DigitalTwinConstants.InterfaceIdLengthErrorMessage, argumentName);
            }
        }

        /// <summary>
        /// Throw ArgumentException if the value is not allowed Digital Twin Type.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        public static void ThrowIfInvalidDigitalTwinType(object argumentValue)
        {
            if (argumentValue != null)
            {
                Type type = argumentValue.GetType();
                if (type != typeof(DataCollection) &&
                    type != typeof(int) &&
                    type != typeof(string) &&
                    type != typeof(long) &&
                    type != typeof(DateTime) &&
                    type != typeof(float) &&
                    type != typeof(double) &&
                    type != typeof(TimeSpan) &&
                    type != typeof(bool))
                {
                    throw new ArgumentException(DigitalTwinConstants.InvalidDigitalTwinTypeErrorMessage);
                }
            }
        }
    }
}
