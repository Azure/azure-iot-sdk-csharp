// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    // Holder for additional client properties such as product information, PnP model Id.
    // This class does not hold any client credential information.
    internal class AdditionalClientInformation
    {
        internal ProductInfo ProductInfo { get; set; }

        internal string ModelId { get; set; }

        internal PayloadConvention PayloadConvention { get; set; }
    }
}
