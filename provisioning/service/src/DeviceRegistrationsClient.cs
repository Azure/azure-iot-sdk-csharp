﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Subclient of <see cref="ProvisioningServiceClient"/> that handles all device registration state operations including
    /// getting a device registration state, deleting a device registration state, and querying device registration states.
    /// </summary>
    public class DeviceRegistrationStatesClient
    {
        private const string DeviceRegistrationStatusUriFormat = "registrations/{0}";
        private const string DeviceRegistrationQueryUriFormat = "registrations/{0}/query";

        private readonly ContractApiHttp _contractApiHttp;
        private readonly RetryHandler _internalRetryHandler;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected DeviceRegistrationStatesClient()
        {
        }

        internal DeviceRegistrationStatesClient(ContractApiHttp contractApiHttp, RetryHandler retryHandler)
        {
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

            HttpResponseMessage response = null;

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        response = await _contractApiHttp
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

            string payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<DeviceRegistrationState>(payload);
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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The iterable set of query results.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> is empty or white space.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        /// <example>
        /// Iterate over device registration states in an enrollment group:
        /// <code language="csharp">
        /// AsyncPageable&lt;DeviceRegistrationState&gt; deviceRegistrationStatesQuery = dpsServiceClient.DeviceRegistrationStates.CreateEnrollmentGroupQuery("SELECT * FROM enrollmentGroups", "myEnrollmentGroupId");
        /// await foreach (DeviceRegistrationState queriedState in deviceRegistrationStatesQuery)
        /// {
        ///     Console.WriteLine(queriedState.RegistrationId);
        /// }
        /// </code>
        /// Iterate over pages of device registration states in an enrollment group:
        /// <code language="csharp">
        /// IAsyncEnumerable&lt;Page&lt;DeviceRegistrationState&gt;&gt; deviceRegistrationStatesQuery = dpsServiceClient.DeviceRegistrationState.CreateQuery("SELECT * FROM enrollmentGroups", "myEnrollmentGroupId").AsPages();
        /// await foreach (Page&lt;DeviceRegistrationState&gt; queriedStatePage in deviceRegistrationStatesQuery)
        /// {
        ///     foreach (DeviceRegistrationState queriedState in queriedStatePage.Values)
        ///     {
        ///         Console.WriteLine(queriedState.RegistrationId);
        ///     }
        ///     
        ///     // Note that this is disposed for you while iterating item-by-item, but not when
        ///     // iterating page-by-page. That is why this sample has to manually call dispose
        ///     // on the response object here.
        ///     queriedStatePage.GetRawResponse().Dispose();
        /// }
        /// </code>
        /// </example>
        public AsyncPageable<DeviceRegistrationState> CreateEnrollmentGroupQuery(string query, string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Creating query.", nameof(CreateEnrollmentGroupQuery));

            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                async Task<Page<DeviceRegistrationState>> NextPageFunc(string continuationToken, int? pageSizeHint)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await QueryBuilder
                        .BuildAndSendRequestAsync<DeviceRegistrationState>(
                            _contractApiHttp,
                            _internalRetryHandler,
                            query,
                            GetDeviceRegistrationQueryUri(enrollmentGroupId), continuationToken, pageSizeHint, cancellationToken)
                        .ConfigureAwait(false);
                }

                async Task<Page<DeviceRegistrationState>> FirstPageFunc(int? pageSizeHint)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await QueryBuilder
                        .BuildAndSendRequestAsync<DeviceRegistrationState>(
                            _contractApiHttp,
                            _internalRetryHandler,
                            query,
                            GetDeviceRegistrationQueryUri(enrollmentGroupId), null, pageSizeHint, cancellationToken)
                        .ConfigureAwait(false);
                }

                return PageableHelpers.CreateAsyncEnumerable(FirstPageFunc, NextPageFunc, null);
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, $"Creating query threw an exception: {ex}", nameof(CreateEnrollmentGroupQuery));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Creating query.", nameof(CreateEnrollmentGroupQuery));
            }
        }

        private static Uri GetDeviceRegistrationStatusUri(string id)
        {
            id = WebUtility.UrlEncode(id);
            return new Uri(
                string.Format(CultureInfo.InvariantCulture, DeviceRegistrationStatusUriFormat, id),
                UriKind.Relative);
        }

        private static Uri GetDeviceRegistrationQueryUri(string id)
        {
            id = WebUtility.UrlEncode(id);
            return new Uri(
                string.Format(CultureInfo.InvariantCulture, DeviceRegistrationQueryUriFormat, id),
                UriKind.Relative);
        }
    }
}
