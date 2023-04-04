// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class ObjectConversionHelper
    {
        internal static bool TryCastOrConvert<T>(object objectToCastOrConvert, PayloadConvention payloadConvention, out T value)
        {
            // If the object is of type T or can be cast to type T, go ahead and return it.
            if (TryCast(objectToCastOrConvert, out value))
            {
                return true;
            }

            try
            {
                // We'll serialize the object back to JSON using the user's configured payload convention
                // and then to the type of their choosing with the same payload convention.
                value = payloadConvention.GetObject<T>(payloadConvention.GetObjectBytes(objectToCastOrConvert));
                return true;
            }
            catch
            {
                // In case the value cannot be converted using the serializer,
                // then return false with the default value of the type <T> passed in.
            }

            value = default;
            return false;
        }

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
                    result = (T)Convert.ChangeType(input.ToString(), typeof(T), CultureInfo.InvariantCulture);
                    return true;
                }
                catch { }
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

            // This call requires parameter labels to avoid confusion with an overload.
            return double.TryParse(
                s: Convert.ToString(
                    expression,
                    CultureInfo.InvariantCulture),
                style: NumberStyles.Any,
                provider: NumberFormatInfo.InvariantInfo,
                result: out _);
        }
    }
}
