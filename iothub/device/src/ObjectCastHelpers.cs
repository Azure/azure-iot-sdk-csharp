// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices.Client
{
    internal class ObjectCastHelpers
    {
        internal static bool TryCast<T>(object objectToCast, out T value)
        {
            if (objectToCast is T valueRef
                || TryCastNumericTo(objectToCast, out valueRef))
            {
                value = valueRef;
                return true;
            }

            value = default;
            return false;
        }

        internal static bool TryCastNumericTo<T>(object input, out T result)
        {
            if (TryGetNumeric(input))
            {
                try
                {
                    result = (T)Convert.ChangeType(input, typeof(T), CultureInfo.InvariantCulture);
                    return true;
                }
                catch
                {
                }
            }

            result = default;
            return false;
        }

        private static bool TryGetNumeric(object expression)
        {
            if (expression == null)
            {
                return false;
            }

            return double.TryParse(
                Convert.ToString(
                    expression,
                    CultureInfo.InvariantCulture),
                NumberStyles.Any,
                NumberFormatInfo.InvariantInfo,
                out _);
        }
    }
}
