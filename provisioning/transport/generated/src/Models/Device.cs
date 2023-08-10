// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary> The device object. </summary>
    public class Device
    {
        /// <summary> Initializes a new instance of Device. </summary>
        /// <param name="registrationId"> The registrationId. </param>
        /// <param name="onboardingStatus"> The status of the onboarding process. </param>
        /// <param name="resourceMetadata"> The response metadata. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="registrationId"/> or <paramref name="resourceMetadata"/> is null. </exception>
        internal Device(string registrationId, DeviceOnboardingStatus onboardingStatus, ResponseMetadata resourceMetadata)
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
            OnboardingStatus = onboardingStatus;
            ResourceMetadata = resourceMetadata;
        }

        /// <summary> The registrationId. </summary>
        [JsonProperty(PropertyName = "registrationId")]
        public string RegistrationId { get; }
        /// <summary> The status of the onboarding process. </summary>
        [JsonProperty(PropertyName = "onboardingStatus")]
        public DeviceOnboardingStatus OnboardingStatus { get; }
        /// <summary> The response metadata. </summary>
        [JsonProperty(PropertyName = "resourceMetadata")]
        public ResponseMetadata ResourceMetadata { get; }
    }
}
