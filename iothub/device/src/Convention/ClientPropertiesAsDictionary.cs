// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    internal class ClientPropertiesAsDictionary
    {
        [JsonProperty(PropertyName = "desired", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal IDictionary<string, object> Desired { get; set; }

        [JsonProperty(PropertyName = "reported", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal IDictionary<string, object> Reported { get; set; }

        internal ClientProperties ToClientProperties(PayloadConvention payloadConvention)
        {
            WritableClientPropertyCollection writableClientPropertyCollection = new WritableClientPropertyCollection(Desired, payloadConvention);
            ClientPropertyCollection clientReportedPropertyCollection = ClientPropertyCollection.FromClientPropertiesAsDictionary(Reported, payloadConvention);

            return new ClientProperties(writableClientPropertyCollection, clientReportedPropertyCollection);
        }
    }
}
