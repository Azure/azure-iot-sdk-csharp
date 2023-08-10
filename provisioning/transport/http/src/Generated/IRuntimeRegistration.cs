// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Microsoft.Rest;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    /// <summary>
    /// RuntimeRegistration operations.
    /// </summary>
    internal partial interface IRuntimeRegistration
    {
        /// <summary>
        /// Gets the registration operation status.
        /// </summary>
        /// <param name='registrationId'>
        /// Registration ID.
        /// </param>
        /// <param name='operationId'>
        /// Operation ID.
        /// </param>
        /// <param name='idScope'>
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <exception cref="Microsoft.Rest.HttpOperationException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        /// <exception cref="Microsoft.Rest.SerializationException">
        /// Thrown when unable to deserialize the response
        /// </exception>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        Task<HttpOperationResponse<RegistrationOperationStatus>> OperationStatusLookupWithHttpMessagesAsync(
            string registrationId,
            string operationId,
            string idScope,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the device registration status.
        /// </summary>
        /// <param name='registrationId'>
        /// Registration ID.
        /// </param>
        /// <param name='idScope'>
        /// </param>
        /// <param name='deviceRegistration'>
        /// Device registration
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <exception cref="Microsoft.Rest.HttpOperationException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        /// <exception cref="Microsoft.Rest.SerializationException">
        /// Thrown when unable to deserialize the response
        /// </exception>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        Task<HttpOperationResponse<Models.DeviceRegistrationResult>> DeviceRegistrationStatusLookupWithHttpMessagesAsync(
            string registrationId,
            string idScope,
            DeviceRegistration deviceRegistration = default(DeviceRegistration),
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Registers the devices.
        /// </summary>
        /// <param name='registrationId'>
        /// Registration ID.
        /// </param>
        /// <param name='idScope'>
        /// </param>
        /// <param name='deviceRegistration'>
        /// Device registration request.
        /// </param>
        /// <param name='forceRegistration'>
        /// Force the device to re-register. Setting this option may assign the
        /// device to a different IotHub.
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <exception cref="Microsoft.Rest.HttpOperationException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        /// <exception cref="Microsoft.Rest.SerializationException">
        /// Thrown when unable to deserialize the response
        /// </exception>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        Task<HttpOperationResponse<RegistrationOperationStatus>> RegisterDeviceWithHttpMessagesAsync(
            string registrationId,
            string idScope,
            DeviceRegistration deviceRegistration = default(DeviceRegistration),
            bool? forceRegistration = default(bool?),
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
