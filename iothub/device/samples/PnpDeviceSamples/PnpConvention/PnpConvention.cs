// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace PnpHelpers
{
    public class PnpConvention
    {
        /// <summary>
        /// Helper to retrieve the property value from the <see cref="TwinCollection"/> property update patch which was received as a result of service-initiated update.
        /// </summary>
        /// <typeparam name="T">The data type of the property, as defined in the DTDL interface.</typeparam>
        /// <param name="collection">The <see cref="TwinCollection"/> property update patch received as a result of service-initiated update.</param>
        /// <param name="propertyName">The property name, as defined in the DTDL interface.</param>
        /// <param name="propertyValue">The corresponding property value.</param>
        /// <param name="componentName">The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <returns>A boolean indicating if the <see cref="TwinCollection"/> property update patch received contains the property update.</returns>
        public static bool TryGetPropertyFromTwin<T>(TwinCollection collection, string propertyName, out T propertyValue, string componentName = null)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            // If the desired property update is for a root component or nested component, verify that property patch received contains the desired property update.
            propertyValue = default;

            if (string.IsNullOrWhiteSpace(componentName))
            {
                if (collection.Contains(propertyName))
                {
                    propertyValue = (T)collection[propertyName];
                    return true;
                }
            }

            if (collection.Contains(componentName))
            {
                JObject componentProperty = collection[componentName];
                if (componentProperty.ContainsKey(propertyName))
                {
                    propertyValue = componentProperty.Value<T>(propertyName);
                    return true;
                }
            }

            return false;
        }
    }
}
