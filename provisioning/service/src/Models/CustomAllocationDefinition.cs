// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("webhookUrl")]
        public Uri WebhookUrl { get; set; }

        /// <summary>
        /// The API version of the provisioning service types (such as IndividualEnrollment) sent in the custom allocation request.
        /// </summary>
        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; }
    }
}