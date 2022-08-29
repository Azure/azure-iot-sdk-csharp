// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Common
{
    internal static class SerializationUtilities
    {
        private static readonly Dictionary<Type, PropertyValueType> s_typeToIntMap = new()
        {
            { typeof(byte), PropertyValueType.Byte },
            { typeof(sbyte), PropertyValueType.SByte },
            { typeof(char), PropertyValueType.Char },
            { typeof(short), PropertyValueType.Int16 },
            { typeof(ushort), PropertyValueType.UInt16 },
            { typeof(int), PropertyValueType.Int32 },
            { typeof(uint), PropertyValueType.UInt32 },
            { typeof(long), PropertyValueType.Int64 },
            { typeof(ulong), PropertyValueType.UInt64 },
            { typeof(float), PropertyValueType.Single },
            { typeof(double), PropertyValueType.Double },
            { typeof(decimal), PropertyValueType.Decimal },
            { typeof(bool), PropertyValueType.Boolean },
            { typeof(Guid), PropertyValueType.Guid },
            { typeof(string), PropertyValueType.String },
            { typeof(Uri), PropertyValueType.Uri },
            { typeof(DateTime), PropertyValueType.DateTime },
            { typeof(DateTimeOffset), PropertyValueType.DateTimeOffset },
            { typeof(TimeSpan), PropertyValueType.TimeSpan },
        };

        public static PropertyValueType GetTypeId(object value)
        {
            if (value == null)
            {
                return PropertyValueType.Null;
            }

            if (s_typeToIntMap.TryGetValue(value.GetType(), out PropertyValueType typeId))
            {
                return typeId;
            }

            return PropertyValueType.Unknown;
        }
    }
}
