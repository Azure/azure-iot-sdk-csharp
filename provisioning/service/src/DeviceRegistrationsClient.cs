// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Subclient of <see cref="ProvisioningServiceClient"/> that handles all device registration state operations including
    /// getting a device registration state, deleting a device registration state, and querying device registration states.
    /// </summary>
    public class DeviceRegistrationStatesClient
    {
        private const string ServiceName = "registrations";
        private const string DeviceRegistrationStatusUriFormat = "{0}/{1}?{2}";

        private readonly IContractApiHttp _contractApiHttp;
        private readonly ServiceConnectionString _serviceConnectionString;
        private readonly RetryHandler _internalRetryHandler;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected DeviceRegistrationStatesClient()
        {
        }

        internal DeviceRegistrationStatesClient(ServiceConnectionString serviceConnectionString, IContractApiHttp contractApiHttp, RetryHandler retryHandler)
        {
            _serviceConnectionString = serviceConnectionString;
            _contractApiHttp = contractApiHttp;
            _internalRetryHandler = retryHandler;
        }

        /// <summary>
        /// Get the device registration state.
        /// </summary>
        /// <param name="registrationId">The Id of the registration to get the state of.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="DeviceRegistrationState"/> with the content of the DeviceRegistrationState in the Provisioning Device Service.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="ProvisioningServiceException">
        /// If the service was not able to retrieve the registration state for the provided <paramref name="registrationId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<DeviceRegistrationState> GetAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            cancellationToken.ThrowIfCancellationRequested();

            ContractApiResponse contractApiResponse = null;

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        contractApiResponse = await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Get,
                                GetDeviceRegistrationStatusUri(registrationId),
                                null,
                                null,
                                new ETag(),
                                cancellationToken)
                            .ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonSerializer.Deserialize<DeviceRegistrationState>(contractApiResponse.Body);
        }

        /// <summary>
        /// Delete the device registration.
        /// </summary>
        /// <param name="registrationId">The Id of the device registration to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="ProvisioningServiceException">
        /// If the service was not able to delete the registration state for the provided <paramref name="registrationId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            return DeleteAsync(new DeviceRegistrationState(registrationId), cancellationToken);
        }

        /// <summary>
        /// Delete the device registration.
        /// </summary>
        /// <param name="deviceRegistrationState">The device registration to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="deviceRegistrationState"/> is null.</exception>
        /// <exception cref="ProvisioningServiceException">
        /// When the service wasn't able to delete the registration status.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task DeleteAsync(DeviceRegistrationState deviceRegistrationState, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(deviceRegistrationState, nameof(deviceRegistrationState));

            cancellationToken.ThrowIfCancellationRequested();

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Delete,
                                GetDeviceRegistrationStatusUri(deviceRegistrationState.RegistrationId),
                                null,
                                null,
                                deviceRegistrationState.ETag,
                                cancellationToken)
                            .ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a query that lists the registration states of devices in a given enrollment group.
        /// </summary>
        /// <param name="query">The SQL query.</param>
        /// <param name="enrollmentGroupId">The enrollment group Id to query.</param>
        /// <param name="pageSize">The int with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The iterable set of query results.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> is empty or white space.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the provided <paramref name="pageSize"/> value is less than zero.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Query CreateEnrollmentGroupQuery(string query, string enrollmentGroupId, int pageSize = 0, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));
            return new Query(_serviceConnectionString, GetDeviceRegistrationStatusUri(enrollmentGroupId).ToString(), query, _contractApiHttp, pageSize, _internalRetryHandler, cancellationToken);
        }

        private static Uri GetDeviceRegistrationStatusUri(string id)
        {
            id = WebUtility.UrlEncode(id);
            return new Uri(
                string.Format(CultureInfo.InvariantCulture, DeviceRegistrationStatusUriFormat, ServiceName, id, SdkUtils.ApiVersionQueryString),
                UriKind.Relative);
        }
    }
}
