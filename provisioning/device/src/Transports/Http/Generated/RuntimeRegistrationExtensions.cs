// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Extension methods for RuntimeRegistration.
    /// </summary>
    internal static partial class RuntimeRegistrationExtensions
    {
        /// <summary>
        /// Gets the registration operation status.
        /// </summary>
        /// <param name='operations'>
        /// The operations group for this extension method.
        /// </param>
        /// <param name='registrationId'>
        /// Registration ID.
        /// </param>
        /// <param name='operationId'>
        /// Operation ID.
        /// </param>
        /// <param name='idScope'>
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        public static async Task<RegistrationOperationStatus> OperationStatusLookupAsync(
            this IRuntimeRegistration operations,
            string registrationId,
            string operationId,
            string idScope,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Rest.HttpOperationResponse<RegistrationOperationStatus> _result = await operations.OperationStatusLookupWithHttpMessagesAsync(
                                                        registrationId,
                                                        operationId,
                                                        idScope,
                                                        null,
                                                        cancellationToken).ConfigureAwait(false))
            {
                return _result.Body;
            }
        }

        /// <summary>
        /// Gets the device registration status.
        /// </summary>
        /// <param name='operations'>
        /// The operations group for this extension method.
        /// </param>
        /// <param name='registrationId'>
        /// Registration ID.
        /// </param>
        /// <param name='idScope'>
        /// </param>
        /// <param name='deviceRegistration'>
        /// Device registration
        /// </param>
        public static DeviceRegistrationResult DeviceRegistrationStatusLookup(
            this IRuntimeRegistration operations,
            string registrationId,
            string idScope,
            DeviceRegistration deviceRegistration = default(DeviceRegistration))
        {
            return operations.DeviceRegistrationStatusLookupAsync(registrationId, idScope, deviceRegistration).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the device registration status.
        /// </summary>
        /// <param name='operations'>
        /// The operations group for this extension method.
        /// </param>
        /// <param name='registrationId'>
        /// Registration ID.
        /// </param>
        /// <param name='idScope'>
        /// </param>
        /// <param name='deviceRegistration'>
        /// Device registration
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        public static async Task<DeviceRegistrationResult> DeviceRegistrationStatusLookupAsync(
            this IRuntimeRegistration operations,
            string registrationId,
            string idScope,
            DeviceRegistration deviceRegistration = default(DeviceRegistration),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Rest.HttpOperationResponse<DeviceRegistrationResult> _result = await operations.DeviceRegistrationStatusLookupWithHttpMessagesAsync(
                                                    registrationId,
                                                    idScope,
                                                    deviceRegistration,
                                                    null,
                                                    cancellationToken).ConfigureAwait(false))
            {
                return _result.Body;
            }
        }

        /// <summary>
        /// Registers the devices.
        /// </summary>
        /// <param name='operations'>
        /// The operations group for this extension method.
        /// </param>
        /// <param name='registrationId'>
        /// Registration ID.
        /// </param>
        /// <param name='idScope'>
        /// </param>
        /// <param name='deviceRegistration'>
        /// Device registration request.
        /// </param>
        /// <param name='forceRegistration'>
        /// Force the device to re-register. Setting this option may assign the device
        /// to a different IotHub.
        /// </param>
        public static RegistrationOperationStatus RegisterDevice(
            this IRuntimeRegistration operations,
            string registrationId,
            string idScope,
            DeviceRegistration deviceRegistration = default(DeviceRegistration),
            bool? forceRegistration = default(bool?))
        {
            return operations.RegisterDeviceAsync(registrationId, idScope, deviceRegistration, forceRegistration).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Registers the devices.
        /// </summary>
        /// <param name='operations'>
        /// The operations group for this extension method.
        /// </param>
        /// <param name='registrationId'>
        /// Registration ID.
        /// </param>
        /// <param name='idScope'>
        /// </param>
        /// <param name='deviceRegistration'>
        /// Device registration request.
        /// </param>
        /// <param name='forceRegistration'>
        /// Force the device to re-register. Setting this option may assign the device
        /// to a different IotHub.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        public static async Task<RegistrationOperationStatus> RegisterDeviceAsync(
            this IRuntimeRegistration operations,
            string registrationId,
            string idScope,
            DeviceRegistration deviceRegistration = default(DeviceRegistration),
            bool? forceRegistration = default(bool?),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Rest.HttpOperationResponse<RegistrationOperationStatus> _result = await operations.RegisterDeviceWithHttpMessagesAsync(registrationId, idScope, deviceRegistration, forceRegistration, null, cancellationToken).ConfigureAwait(false))
            {
                _result.Body.RetryAfter = _result.Response.Headers.RetryAfter?.Delta;
                return _result.Body;
            }
        }
    }
}
