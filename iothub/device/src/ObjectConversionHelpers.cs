// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices.Client
{
    internal class ObjectConversionHelpers
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
                // If the cannot be cast to <T> directly we need to try to convert it using the serializer.
                // If it can be successfully converted, go ahead and return it.
                value = payloadConvention.PayloadSerializer.ConvertFromJsonObject<T>(objectToCastOrConvert);
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
                    result = (T)Convert.ChangeType(input, typeof(T), CultureInfo.InvariantCulture);
                    return true;
                }
                catch
                {
                    // In case the value cannot be converted,
                    // then return false with the default value of the type <T> passed in.
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
