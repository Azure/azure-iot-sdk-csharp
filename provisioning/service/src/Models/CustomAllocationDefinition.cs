// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Defines a custom allocation
    /// </summary>
    public class CustomAllocationDefinition
    {
        /// <summary>
        /// The webhook URL used for allocation requests.
        /// </summary>
        [JsonProperty(PropertyName = "webhookUrl", Required = Required.Always)]
#pragma warning disable CA1056 // Uri properties should not be strings
        public string WebhookUrl { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings

        /// <summary>
        /// The API version of the provisioning service types (such as IndividualEnrollment) sent in the custom allocation request.
        /// </summary>
        [JsonProperty(PropertyName = "apiVersion", Required = Required.Always)]
        public string ApiVersion { get; set; }
    }
}