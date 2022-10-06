// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The collection of desired property update requests received from service.
    /// </summary>
    public class DesiredPropertyCollection : PropertyCollection
    {
        internal DesiredPropertyCollection(Dictionary<string, object> desiredProperties)
            : base(desiredProperties)
        {
        }
    }
}
