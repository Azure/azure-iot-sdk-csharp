// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Device onboarding.
    /// </summary>
    public class DeviceOnboardingResult
    {
        /// <summary>
        /// Initializes a new instance of the DeviceOnboardingResult class.
        /// </summary>
        public DeviceOnboardingResult(string operationId = default, Device result = default)
        {
            Id = operationId;
            Result = result;
        }

        /// <summary>
        /// Operation ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Resulting device details
        /// </summary>
        public Device Result { get; set; }
    }

    /// <summary> The device object. </summary>
    public class Device
    {
        /// <summary> Initializes a new instance of Device. </summary>
        /// <param name="registrationId"> The registrationId. </param>
        /// <param name="onboardingStatus"> The status of the onboarding process. </param>
        public Device(string registrationId, string onboardingStatus)
        {
            RegistrationId = registrationId;
            OnboardingStatus = onboardingStatus;
        }

        /// <summary> The registrationId. </summary>
        public string RegistrationId { get; }
        /// <summary> The status of the onboarding process. </summary>
        public string OnboardingStatus { get; }
    }
}