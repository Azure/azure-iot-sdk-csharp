// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Azure.Devices.Extensions;

namespace Microsoft.Azure.Devices.PlugAndPlayConvention
{
    /// <summary>
    /// A helper class for formatting command requests and properties as per plug and play convention.
    /// </summary>
    public static class PnpConventionHelper
    {
        /// <summary>
        /// Create a property patch for component-level property updates.
        /// </summary>
        /// <param name="propertyKeyValuePairs">The dictionary of property key values pairs that are to be updated.</param>
        /// <returns>The dictionary containing the property key value pairs for a component-level property update.</returns>
        public static Dictionary<string, object> CreatePatchForComponentUpdate(Dictionary<string, object> propertyKeyValuePairs)
        {
            propertyKeyValuePairs.ThrowIfNull(nameof(propertyKeyValuePairs));

            string metadataKey = "$metadata";
            propertyKeyValuePairs.Add(metadataKey, new object());

            return propertyKeyValuePairs;
        }

    }
}
