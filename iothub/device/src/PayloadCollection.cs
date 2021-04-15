// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public class PayloadCollection
    {
        internal IDictionary<string, object> Collection { get; set; } = new Dictionary<string, object>();

        internal IPayloadConvention Convention { get; set; }

        internal PayloadCollection(IPayloadConvention payloadConvention = default)
        {
            Convention = payloadConvention ?? DefaultPayloadConvention.Instance;
        }

        internal byte[] GetPayloadObjectBytes()
        {
            return Convention.GetObjectBytes(Collection);
        }
    }
}
