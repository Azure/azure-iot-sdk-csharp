// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Devices.Extensions
{
    internal static class Extensions
    {
        /// <summary>
        /// Append the "$metadata" identifier to the property values, which helps the service identify the patch as a component update.
        /// </summary>
        /// <param name="propertyKeyValuePairs">The dictionary of property key values pairs to update to.</param>
        internal static void AddComponentUpdateIdentifier(this Dictionary<string, object> propertyKeyValuePairs)
        {
            Argument.RequireNotNull(propertyKeyValuePairs, nameof(propertyKeyValuePairs));

            const string metadataKey = "$metadata";
            propertyKeyValuePairs.Add(metadataKey, new object());
        }
    }
}
