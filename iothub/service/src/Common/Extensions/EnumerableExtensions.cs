// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Devices.Common
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Avoid allocating if possible (if ICollection is supported)
        /// </summary>
        public static bool AnySlim<T>(this IEnumerable<T> enumerable)
        {
            return enumerable is ICollection collection
                ? collection.Count != 0
                : enumerable.Any();
        }

        /// <summary>
        /// If the underlying type already supports an IList implementation then don't allocate a new List.
        /// </summary>
        public static IList<T> ToListSlim<T>(this IEnumerable<T> enumerable, bool requireMutable = false)
        {
            return enumerable is IList<T> list && !(requireMutable && list.IsReadOnly)
                ? list
                : enumerable.ToList();
        }
    }
}
