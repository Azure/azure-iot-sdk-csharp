// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary> The onboarding request. </summary>
    internal class OnboardingRequest
    {
        /// <summary> Initializes a new instance of OnboardingRequest. </summary>
        /// <param name="registrationId"> The device registrationId. Must match the X.509 Certificate CN. </param>
        /// <param name="resourceMetadata"> Additional resource metadata. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="registrationId"/> or <paramref name="resourceMetadata"/> is null. </exception>
        public OnboardingRequest(string registrationId, ResourceMetadata resourceMetadata)
        {
            if (registrationId == null)
            {
                throw new ArgumentNullException(nameof(registrationId));
            }
            if (resourceMetadata == null)
            {
                throw new ArgumentNullException(nameof(resourceMetadata));
            }

            RegistrationId = registrationId;
            ResourceMetadata = resourceMetadata;
        }

        /// <summary> The device registrationId. Must match the X.509 Certificate CN. </summary>
        [JsonProperty(PropertyName = "registrationId")]
        public string RegistrationId { get; }
        /// <summary> Additional resource metadata. </summary>
        [JsonProperty(PropertyName = "resourceMetadata")]
        public ResourceMetadata ResourceMetadata { get; }
    }
}
