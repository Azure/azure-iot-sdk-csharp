// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Azure.Devices.Extensions;

namespace Microsoft.Azure.Devices.PlugAndPlay
{
    /// <summary>
    /// A helper class for formatting command requests and properties as per plug and play convention.
    /// </summary>
    public static class PnpHelper
    {
        /// <summary>
        /// Create a property patch value for component-level property updates.
        /// </summary>
        /// <remarks>
        /// Service requires the component patch to be in the following format, so that service can identify that this is a component update, and not a root-level property update:
        /// {
        ///     "op": "add",
        ///     "path": "/componentName",
        ///     "value": {
        ///         "samplePropertyName1": 24,
        ///         "samplePropertyName2": "completed",
        ///         ...,
        ///         "$metadata": {}
        ///     }
        /// }
        ///
        /// This helper formats the patch value in the expected format.
        /// </remarks>
        /// <param name="propertyKeyValuePairs">The dictionary of property key values pairs that are to be updated.</param>
        /// <returns>The dictionary containing the property key value pairs for a component-level property update.</returns>
        public static Dictionary<string, object> CreatePatchValueForComponentUpdate(Dictionary<string, object> propertyKeyValuePairs)
        {
            propertyKeyValuePairs.ThrowIfNull(nameof(propertyKeyValuePairs));

            const string metadataKey = "$metadata";
            propertyKeyValuePairs.Add(metadataKey, new object());

            return propertyKeyValuePairs;
        }
    }
}
