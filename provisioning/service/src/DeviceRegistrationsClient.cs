// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;

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

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected DeviceRegistrationStatesClient()
        {
        }

        internal DeviceRegistrationStatesClient(ServiceConnectionString serviceConnectionString, IContractApiHttp contractApiHttp)
        {
            _serviceConnectionString = serviceConnectionString;
            _contractApiHttp = contractApiHttp;
        }

        /// <summary>
        /// Retrieve the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will return the DeviceRegistrationState for the provided id. It will retrieve
        /// the correspondent DeviceRegistrationState from the Device Provisioning Service, and return it in the
        /// <see cref="DeviceRegistrationState"/> object.
        /// </remarks>
        /// <param name="id">The string that identifies the DeviceRegistrationState. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="DeviceRegistrationState"/> with the content of the DeviceRegistrationState in the Provisioning Device Service.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="id"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="id"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to retrieve the registration state for the provided <paramref name="id"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<DeviceRegistrationState> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(id, nameof(id));

            ContractApiResponse contractApiResponse = await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Get,
                    GetDeviceRegistrationStatusUri(id),
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<DeviceRegistrationState>(contractApiResponse.Body);
        }

        /// <summary>
        /// Delete the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will remove the DeviceRegistrationState from the Device Provisioning Service using the
        /// provided id. It will delete the registration status regardless the eTag.
        /// </remarks>
        /// <param name="id">The string that identifies the DeviceRegistrationState. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="id"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="id"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to delete the registration state for the provided <paramref name="id"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            return DeleteAsync(new DeviceRegistrationState(id), cancellationToken);
        }

        /// <summary>
        /// Delete the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will remove the DeviceRegistrationState from the Device Provisioning Service using the
        /// provided <see cref="DeviceRegistrationState"/> information. The Device Provisioning Service will care about the
        /// id and the eTag on the DeviceRegistrationState. If you want to delete the DeviceRegistrationState regardless the
        /// eTag, you can use the <see cref="DeleteAsync(string, CancellationToken)"/> passing only the id.
        /// </remarks>
        /// <param name="deviceRegistrationState">The <see cref="DeviceRegistrationState"/> that identifies the DeviceRegistrationState.
        /// It cannot be null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="deviceRegistrationState"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// When the service wasn't able to delete the registration status.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task DeleteAsync(DeviceRegistrationState deviceRegistrationState, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(deviceRegistrationState, nameof(deviceRegistrationState));

            await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Delete,
                    GetDeviceRegistrationStatusUri(deviceRegistrationState.RegistrationId),
                    null,
                    null,
                    deviceRegistrationState.ETag,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a device registration state query.
        /// </summary>
        /// <param name="query">The <see cref="QuerySpecification"/> with the SQL query. It cannot be null.</param>
        /// <param name="enrollmentGroupId">The enrollment group Id to query.</param>
        /// <param name="pageSize">The int with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> is empty or white space.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the provided <paramref name="pageSize"/> value is less than zero.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Query CreateQuery(string query, string enrollmentGroupId, int pageSize = 0, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));
            return new Query(_serviceConnectionString, GetDeviceRegistrationStatusUri(enrollmentGroupId).ToString(), query, _contractApiHttp, pageSize, cancellationToken);
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
