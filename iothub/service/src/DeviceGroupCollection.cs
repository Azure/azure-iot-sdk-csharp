// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Devices
{
    ///<summary>
    ///A paged collection of device groups
    ///</summary>
    public class DeviceGroupCollection
    {
        ///<summary>
        ///The device groups
        ///</summary>
        public List<DeviceGroup> Value { get; set; }

        ///<summary>
        ///A URL to retrieve the next page of results. The last page of results does not contain a nextLink property.
        ///</summary>
        public string NextLink { get; }
    }
}