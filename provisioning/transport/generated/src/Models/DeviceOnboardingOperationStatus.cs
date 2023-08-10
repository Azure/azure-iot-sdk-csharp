// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary> Provides status details for long running operations. </summary>
    public partial class DeviceOnboardingOperationStatus
    {
        /// <summary> Initializes a new instance of DeviceOnboardingOperationStatus. </summary>
        /// <param name="id"> The unique ID of the operation. </param>
        /// <param name="status"> The status of the operation. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="id"/> is null. </exception>
        internal DeviceOnboardingOperationStatus(string id, OperationState status)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            Status = status;
        }

        /// <summary> Initializes a new instance of DeviceOnboardingOperationStatus. </summary>
        /// <param name="id"> The unique ID of the operation. </param>
        /// <param name="status"> The status of the operation. </param>
        /// <param name="error"> Error object that describes the error when status is &quot;Failed&quot;. </param>
        /// <param name="result"> The result of the operation. </param>
        internal DeviceOnboardingOperationStatus(string id, OperationState status, AzureCoreFoundationsError error, Device result)
        {
            Id = id;
            Status = status;
            Error = error;
            Result = result;
        }

        /// <summary> The unique ID of the operation. </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; }
        /// <summary> The status of the operation. </summary>
        [JsonProperty(PropertyName = "status")]
        public OperationState Status { get; }
        /// <summary> Error object that describes the error when status is &quot;Failed&quot;. </summary>
        [JsonProperty(PropertyName = "error")]
        public AzureCoreFoundationsError Error { get; }
        /// <summary> The result of the operation. </summary>
        [JsonProperty(PropertyName = "result")]
        public Device Result { get; }

        /// <summary>
        /// Gets or sets the Retry-After header.
        /// </summary>
        public TimeSpan? RetryAfter { get; set; }
    }
}
