// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Function link details 
    /// </summary>
    public class CustomAllocationDefinition
    {
        /// <summary>
        /// The webhook URL used for allocation requests.
        /// </summary>
        [JsonProperty(PropertyName = "webhookUrl", Required = Required.Always)]
        public string Webhook { get; set; }

        /// <summary>
        /// The API version of the provisioning service types (such as IndividualEnrollment) sent in the custom allocation request. Supported versions include: "2018-09-01-preview"
        /// </summary>
        [JsonProperty(PropertyName = "apiVersion", Required = Required.Always)]
        public string ApiVersion { get; set; }
    }
}
