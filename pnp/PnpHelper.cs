// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace PnpHelpers
{
    public static class PnpHelper
    {
        public const string TelemetryComponentPropertyName = "$.sub";
        private const string PropertyComponentIdentifier = "\"__t\": \"c\",";

        public static string CreateReportedPropertiesPatch(string propertyName, object propertyValue, string componentName = null)
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                return $"" +
                    $"{{" +
                    $"  \"{propertyName}\": " +
                    $"      {{ " +
                    $"          \"value\": \"{propertyValue}\" " +
                    $"      }} " +
                    $"}}";
            }

            return $"" +
                $"{{" +
                $"  \"{componentName}\": " +
                $"      {{" +
                $"          {PropertyComponentIdentifier}" +
                $"          \"value\": \"{propertyValue}\" " +
                $"      }} " +
                $"}}";
        }
    }
}
