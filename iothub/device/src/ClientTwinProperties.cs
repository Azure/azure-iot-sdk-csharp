// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    internal class ClientTwinProperties
    {
        internal ClientTwinProperties()
        {
            Desired = new Dictionary<string, object>();
            Reported = new Dictionary<string, object>();
        }

        [JsonProperty(PropertyName = "desired", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal IDictionary<string, object> Desired { get; set; }

        [JsonProperty(PropertyName = "reported", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal IDictionary<string, object> Reported { get; set; }

        internal ClientProperties ToClientProperties(PayloadConvention payloadConvention)
        {

            ClientPropertyCollection writablePropertyCollection = ClientPropertyCollection.FromClientTwinDictionary(Desired, payloadConvention);
            ClientPropertyCollection propertyCollection = ClientPropertyCollection.FromClientTwinDictionary(Reported, payloadConvention);

            return new ClientProperties(writablePropertyCollection, propertyCollection);
        }
    }
}
