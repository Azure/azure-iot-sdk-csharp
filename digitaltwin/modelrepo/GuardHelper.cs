using System;
using System.Globalization;

namespace Microsoft.Azure.DigitalTwin.Model.Service
{
    /// <summary>
    /// Helper class for null checks, empty string, white spaces
    /// </summary>
    internal static class GuardHelper
    {
        public const string ParameterNullErrorMessageFormat = "The parameter named {0} can't be null.";
        public const string ParameterNullWhiteSpaceErrorMessageFormat = "The parameter named {0} can't be null, empty string or white space.";

        /// <summary>
        /// Throw ArgumentNullException if the value is null reference.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">The argument name.</param>
        internal static void ThrowIfNull(object argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                var errorMessage = string.Format(CultureInfo.InvariantCulture, ParameterNullErrorMessageFormat, argumentName);
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
                var errorMessage = string.Format(CultureInfo.InvariantCulture, ParameterNullWhiteSpaceErrorMessageFormat, argumentName);
                throw new ArgumentNullException(argumentName, errorMessage);
            }
        }
    }
}
