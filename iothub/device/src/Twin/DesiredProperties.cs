// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// These are twin properties requested by a service application for a change in property value by a service application.
    /// </summary>
    /// <remarks>
    /// These are read-only from a device perspective.
    /// <para>
    /// This class can be inherited from and set by unit tests for mocking purposes.
    /// </para>
    /// </remarks>
    public class DesiredProperties : PropertyCollection
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <remarks>
        /// This class can be inherited from and set by unit tests for mocking purposes.
        /// </remarks>
        protected internal DesiredProperties(Dictionary<string, object> desiredProperties)
            : base(desiredProperties, true)
        {
        }
    }
}
