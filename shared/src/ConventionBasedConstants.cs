// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// Container for common convention based constants.
    /// </summary>
    public static class ConventionBasedConstants
    {
        /// <summary>
        /// Marker key to indicate a component-level property.
        /// </summary>
        public const string ComponentIdentifierKey = "__t";

        /// <summary>
        /// Marker value to indicate a component-level property.
        /// </summary>
        public const string ComponentIdentifierValue = "c";

        /// <summary>
        /// Represents the JSON document property name for the value of a writable property response.
        /// </summary>
        public const string ValuePropertyName = "value";

        /// <summary>
        /// Represents the JSON document property name for the Ack Code of a writable property response.
        /// </summary>
        public const string AckCodePropertyName = "ac";

        /// <summary>
        /// Represents the JSON document property name for the Ack Version of a writable property response.
        /// </summary>
        public const string AckVersionPropertyName = "av";

        /// <summary>
        /// Represents the JSON document property name for the Ack Description of a writable property response.
        /// </summary>
        public const string AckDescriptionPropertyName = "ad";
    }
}
