// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service
{

    internal class HttpProvisioningServiceClient : ProvisioningServiceClient, IDisposable
    {
        const string EnrollmentUriFormat = "enrollments/{0}?{1}";
        const string BulkEnrollmentUriFormat = "enrollments?{0}";
        const string EnrollmentGroupUriFormat = "enrollmentGroups/{0}?{1}";
        const string QueryEnrollmentUriFormat = "enrollments/query?{0}";
        const string QueryEnrollmentGroupUriFormat = "enrollmentGroups/query?{0}";
        const string DeviceProvisioningUriFormat = "registrations/{0}?{1}";
        const string DeviceProvisioningsUriFormat = "registrations/{0}/query?{1}";

        const string ContinuationTokenHeader = "x-ms-continuation";
        const string PageSizeHeader = "x-ms-max-item-count";

        private const string ApiVersionQueryString = ClientApiVersionHelper.ApiVersionQueryString;

        static readonly Regex IDRegex = new Regex(@"^[A-Za-z0-9\-:.+%_#*?!(),=@;$']{1,128}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromSeconds(100);

        IHttpClientHelper httpClientHelper;
        readonly string drsName;

        internal HttpProvisioningServiceClient(IotHubConnectionString connectionString)
        {
            this.drsName = connectionString.IotHubName;
            this.httpClientHelper = new HttpClientHelper(
                connectionString.HttpsEndpoint,
                connectionString,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                DefaultOperationTimeout,
                client => { });
        }

        public override Task OpenAsync()
        {
            return TaskHelpers.CompletedTask;
        }

        public override Task CloseAsync()
        {
            return TaskHelpers.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.httpClientHelper != null)
                {
                    this.httpClientHelper.Dispose();
                    this.httpClientHelper = null;
                }
            }
        }

        internal void ThrowIfClosed()
        {
            if (this.httpClientHelper == null)
            {
                throw new ObjectDisposedException("DeviceRegistrationClient", ApiResources.ProvisioningServiceClientAlreadyClosed);
            }
        }

        public override Task<Enrollment> AddEnrollmentAsync(Enrollment enrollment)
        {
            return this.AddEnrollmentAsync(enrollment, CancellationToken.None);
        }

        public override Task<Enrollment> AddEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            ValidateRegistrationAndDeviceId(enrollment);

            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.Conflict,
                    responseMessage => Task.FromResult((Exception) new EnrollmentAlreadyExistsException(enrollment.RegistrationId))
                }
            };

            return this.httpClientHelper.PutAsync(GetEnrollmentUri(enrollment.RegistrationId), enrollment, PutOperationType.CreateEntity, errorMappingOverrides, cancellationToken);
        }

        public override Task<BulkOperationResult> AddEnrollmentsAsync(IEnumerable<Enrollment> enrollments)
        {
            return this.AddEnrollmentsAsync(enrollments, CancellationToken.None);
        }

        public override Task<BulkOperationResult> AddEnrollmentsAsync(IEnumerable<Enrollment> enrollments, CancellationToken cancellationToken)
        {
            ThrowIfClosed();

            ValidateRegistrationAndDeviceIdForEnrollmentList(enrollments);

            var operation = new BulkOperation()
            {
                Mode = BulkOperationMode.Create,
                Enrollments = enrollments
            };

            return this.httpClientHelper.PostAsync<BulkOperation, BulkOperationResult>(
                GetBulkEnrollmentUri(), 
                operation,
                new Dictionary<HttpStatusCode, 
                Func<HttpResponseMessage, Task<Exception>>>(), 
                null, 
                cancellationToken);
        }

        public override Task<Enrollment> GetEnrollmentAsync(string registrationId)
        {
            return this.GetEnrollmentAsync(registrationId, CancellationToken.None);
        }

        public override Task<Enrollment> GetEnrollmentAsync(string registrationId, CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.NotFound,
                    responseMessage => Task.FromResult((Exception) new EnrollmentNotFoundException(registrationId))
                }
            };

            return this.httpClientHelper.GetAsync<Enrollment>(GetEnrollmentUri(registrationId), errorMappingOverrides, null, false, cancellationToken);
        }

        public override Task RemoveEnrollmentAsync(Enrollment enrollment)
        {
            return this.RemoveEnrollmentAsync(enrollment, CancellationToken.None);
        }

        public override Task RemoveEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken)
        {
            return this.RemoveEnrollmentAsync(enrollment.RegistrationId, enrollment, CancellationToken.None);
        }

        public override Task RemoveEnrollmentAsync(string registrationId)
        {
            return this.RemoveEnrollmentAsync(registrationId, CancellationToken.None);
        }

        public override Task RemoveEnrollmentAsync(string registrationId, CancellationToken cancellationToken)
        {
            var eTag = new ETagHolder { ETag = "*" };
            return this.RemoveEnrollmentAsync(registrationId, eTag, cancellationToken);
        }

        Task RemoveEnrollmentAsync(string registrationId, IETagHolder eTagHolder, CancellationToken cancellationToken)
        {
            ThrowIfClosed();

            if (string.IsNullOrWhiteSpace(eTagHolder.ETag))
            {
                throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingEnrollment);
            }

            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.NotFound,
                    responseMessage => Task.FromResult((Exception) new EnrollmentNotFoundException(registrationId))
                }
            };

            return this.httpClientHelper.DeleteAsync<IETagHolder>(GetEnrollmentUri(registrationId), eTagHolder, errorMappingOverrides, null, cancellationToken);
        }

        public override Task RemoveEnrollmentsAsync(IEnumerable<Enrollment> enrollments)
        {
            return RemoveEnrollmentsAsync(enrollments, CancellationToken.None);
        }

        public override Task RemoveEnrollmentsAsync(IEnumerable<Enrollment> enrollments, CancellationToken cancellationToken)
        {
            ThrowIfClosed();

            foreach (Enrollment enrollment in enrollments)
            {
                if (string.IsNullOrWhiteSpace(enrollment.ETag))
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingEnrollment);
                }
            }

            var operation = new BulkOperation()
            {
                Mode = BulkOperationMode.Delete,
                Enrollments = enrollments
            };

            return this.httpClientHelper.PostAsync<BulkOperation, BulkOperationResult>(GetBulkEnrollmentUri(), operation,
                new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>(), null, cancellationToken);
        }

        public override Task RemoveEnrollmentsAsync(IEnumerable<string> registrationIds)
        {
            return this.RemoveEnrollmentsAsync(registrationIds, CancellationToken.None);
        }

        public override Task RemoveEnrollmentsAsync(IEnumerable<string> registrationIds, CancellationToken cancellationToken)
        {
            var enrollments = new List<Enrollment>();
            foreach (string registrationId in registrationIds)
            {
                enrollments.Add(new Enrollment(registrationId)
                {
                    ETag = "*"
                });
            }

            return this.RemoveEnrollmentsAsync(enrollments, cancellationToken);
        }

        public override Task<Enrollment> UpdateEnrollmentAsync(Enrollment enrollment)
        {
            return this.UpdateEnrollmentAsync(enrollment, CancellationToken.None);
        }

        public override Task<Enrollment> UpdateEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken)
        {
            return this.UpdateEnrollmentAsync(enrollment, false, cancellationToken);
        }

        public override Task<Enrollment> UpdateEnrollmentAsync(Enrollment enrollment, bool forceUpdate)
        {
            return this.UpdateEnrollmentAsync(enrollment, forceUpdate, CancellationToken.None);
        }

        public override Task<Enrollment> UpdateEnrollmentAsync(Enrollment enrollment, bool forceUpdate, CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            ValidateRegistrationAndDeviceId(enrollment);

            if (string.IsNullOrWhiteSpace(enrollment.ETag) && !forceUpdate)
            {
                throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingEnrollment);
            }

            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.Conflict,
                    responseMessage => Task.FromResult((Exception) new EnrollmentAlreadyExistsException(enrollment.RegistrationId))
                },
                {
                    HttpStatusCode.PreconditionFailed,
                    async (responseMessage) => new PreconditionFailedException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                }
            };

            PutOperationType operationType = forceUpdate ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity;

            return this.httpClientHelper.PutAsync(GetEnrollmentUri(enrollment.RegistrationId), enrollment, operationType, errorMappingOverrides, cancellationToken);
        }

        public override Task<BulkOperationResult> UpdateEnrollmentsAsync(IEnumerable<Enrollment> enrollments)
        {
            return this.UpdateEnrollmentsAsync(enrollments, false, CancellationToken.None);
        }

        public override Task<BulkOperationResult> UpdateEnrollmentsAsync(IEnumerable<Enrollment> enrollments, CancellationToken cancellationToken)
        {
            return this.UpdateEnrollmentsAsync(enrollments, false, cancellationToken);
        }

        public override Task<BulkOperationResult> UpdateEnrollmentsAsync(IEnumerable<Enrollment> enrollments, bool forceUpdate)
        {
            return this.UpdateEnrollmentsAsync(enrollments, forceUpdate, CancellationToken.None);
        }

        public override Task<BulkOperationResult> UpdateEnrollmentsAsync(IEnumerable<Enrollment> enrollments, bool forceUpdate, CancellationToken cancellationToken)
        {
            ThrowIfClosed();

            ValidateRegistrationAndDeviceIdForEnrollmentList(enrollments);

            foreach (Enrollment enrollment in enrollments)
            {
                if (string.IsNullOrWhiteSpace(enrollment.ETag) && !forceUpdate)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingEnrollment);
                }
            }

            var operation = new BulkOperation()
            {
                Mode = forceUpdate ? BulkOperationMode.Update : BulkOperationMode.UpdateIfMatchETag,
                Enrollments = enrollments
            };

            return this.httpClientHelper.PostAsync<BulkOperation, BulkOperationResult>(GetBulkEnrollmentUri(), operation,
                new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>(), null, cancellationToken);
        }

        public override Task<EnrollmentGroup> AddEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup)
        {
            return this.AddEnrollmentGroupAsync(enrollmentGroup, CancellationToken.None);
        }

        public override Task<EnrollmentGroup> AddEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            ValidateEnrollmentGroup(enrollmentGroup);

            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.Conflict,
                    responseMessage => Task.FromResult((Exception) new EnrollmentGroupAlreadyExistsException(enrollmentGroup.EnrollmentGroupId))
                }
            };

            return this.httpClientHelper.PutAsync(GetEnrollmentGroupUri(enrollmentGroup.EnrollmentGroupId), enrollmentGroup, PutOperationType.CreateEntity, errorMappingOverrides, cancellationToken);
        }

        public override Task<EnrollmentGroup> GetEnrollmentGroupAsync(string enrollmentGroupId)
        {
            return this.GetEnrollmentGroupAsync(enrollmentGroupId, CancellationToken.None);
        }

        public override Task<EnrollmentGroup> GetEnrollmentGroupAsync(string enrollmentGroupId, CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.NotFound,
                    responseMessage => Task.FromResult((Exception) new EnrollmentGroupNotFoundException(enrollmentGroupId))
                }
            };

            return this.httpClientHelper.GetAsync<EnrollmentGroup>(GetEnrollmentGroupUri(enrollmentGroupId), errorMappingOverrides, null, false, cancellationToken);
        }
        
        public override IProvisioningQuery CreateEnrollmentsQuery()
        {
            return this.CreateEnrollmentsQuery(null);
        }

        public override IProvisioningQuery CreateEnrollmentsQuery(int? pageSize)
        {
            return new ProvisioningQuery(token => this.ExecuteQueryAsync(GetQueryEnrollmentUri(), pageSize, token, CancellationToken.None));
        }

        public override IProvisioningQuery CreateEnrollmentGroupsQuery()
        {
            return this.CreateEnrollmentGroupsQuery(null);
        }

        public override IProvisioningQuery CreateEnrollmentGroupsQuery(int? pageSize)
        {
            return new ProvisioningQuery(token => this.ExecuteQueryAsync(GetQueryEnrollmentGroupUri(), pageSize, token, CancellationToken.None));
        }

        public override Task RemoveEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup)
        {
            return this.RemoveEnrollmentGroupAsync(enrollmentGroup, CancellationToken.None);
        }

        public override Task RemoveEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken)
        {
            return this.RemoveEnrollmentGroupAsync(enrollmentGroup.EnrollmentGroupId, enrollmentGroup, CancellationToken.None);
        }

        public override Task RemoveEnrollmentGroupAsync(string enrollmentGroupId)
        {
            return this.RemoveEnrollmentGroupAsync(enrollmentGroupId, CancellationToken.None);
        }

        public override Task RemoveEnrollmentGroupAsync(string enrollmentGroupId, CancellationToken cancellationToken)
        {
            var eTag = new ETagHolder { ETag = "*" };
            return this.RemoveEnrollmentGroupAsync(enrollmentGroupId, eTag, cancellationToken);
        }

        Task RemoveEnrollmentGroupAsync(string enrollmentGroupId, IETagHolder eTagHolder, CancellationToken cancellationToken)
        {
            ThrowIfClosed();

            if (string.IsNullOrWhiteSpace(eTagHolder.ETag))
            {
                throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingEnrollmentGroup);
            }

            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.NotFound,
                    responseMessage => Task.FromResult((Exception) new EnrollmentGroupNotFoundException(enrollmentGroupId))
                }
            };

            return this.httpClientHelper.DeleteAsync<IETagHolder>(GetEnrollmentGroupUri(enrollmentGroupId), eTagHolder, errorMappingOverrides, null, cancellationToken);
        }

        public override Task<EnrollmentGroup> UpdateEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup)
        {
            return this.UpdateEnrollmentGroupAsync(enrollmentGroup, CancellationToken.None);
        }

        public override Task<EnrollmentGroup> UpdateEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken)
        {
            return this.UpdateEnrollmentGroupAsync(enrollmentGroup, false, cancellationToken);
        }

        public override Task<EnrollmentGroup> UpdateEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, bool forceUpdate)
        {
            return this.UpdateEnrollmentGroupAsync(enrollmentGroup, forceUpdate, CancellationToken.None);
        }

        public override Task<EnrollmentGroup> UpdateEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, bool forceUpdate, CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            ValidateEnrollmentGroup(enrollmentGroup);

            if (string.IsNullOrWhiteSpace(enrollmentGroup.ETag) && !forceUpdate)
            {
                throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingEnrollmentGroup);
            }

            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.Conflict,
                    responseMessage => Task.FromResult((Exception) new EnrollmentGroupAlreadyExistsException(enrollmentGroup.EnrollmentGroupId))
                },
                {
                    HttpStatusCode.PreconditionFailed,
                    async (responseMessage) => new PreconditionFailedException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                }
            };

            PutOperationType operationType = forceUpdate ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity;

            return this.httpClientHelper.PutAsync(GetEnrollmentGroupUri(enrollmentGroup.EnrollmentGroupId), enrollmentGroup, operationType, errorMappingOverrides, cancellationToken);
        }

        public override Task<RegistrationStatus> GetDeviceRegistrationAsync(string registrationId)
        {
            return this.GetDeviceRegistrationAsync(registrationId, CancellationToken.None);
        }

        public override Task<RegistrationStatus> GetDeviceRegistrationAsync(string registrationId, CancellationToken cancellationToken)
        {
            ThrowIfClosed();

            if (string.IsNullOrWhiteSpace(registrationId))
            {
                throw new ArgumentException(ApiResources.RegistrationIdIsNull);
            }

            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.NotFound,
                    responseMessage => Task.FromResult((Exception) new DeviceNotFoundException(registrationId))
                }
            };

            return this.httpClientHelper.GetAsync<RegistrationStatus>(GetDeviceRegistrationUri(registrationId), errorMappingOverrides, null, false, cancellationToken);
        }

        public override IProvisioningQuery CreateDeviceRegistrationsQuery(string enrollmentGroupId)
        {
            return this.CreateDeviceRegistrationsQuery(enrollmentGroupId, null);
        }

        public override IProvisioningQuery CreateDeviceRegistrationsQuery(string enrollmentGroupId, int? pageSize)
        {
            if (string.IsNullOrWhiteSpace(enrollmentGroupId))
            {
                throw new ArgumentException(ApiResources.EnrollmentGroupIdIsNull);
            }

            return new ProvisioningQuery(token => this.ExecuteQueryAsync(GetDeviceRegistrationsUri(enrollmentGroupId), pageSize, token, CancellationToken.None));
        }

        public override Task RemoveDeviceRegistrationAsync(RegistrationStatus registrationStatus)
        {
            return this.RemoveDeviceRegistrationAsync(registrationStatus, CancellationToken.None);
        }

        public override Task RemoveDeviceRegistrationAsync(RegistrationStatus registrationStatus, CancellationToken cancellationToken)
        {
            return this.RemoveDeviceRegistrationAsync(registrationStatus.RegistrationId, registrationStatus, cancellationToken);
        }

        public override Task RemoveDeviceRegistrationAsync(string registrationId)
        {
            return this.RemoveDeviceRegistrationAsync(registrationId, CancellationToken.None);
        }

        public override Task RemoveDeviceRegistrationAsync(string registrationId, CancellationToken cancellationToken)
        {
            var eTag = new ETagHolder { ETag = "*" };
            return this.RemoveDeviceRegistrationAsync(registrationId, eTag, cancellationToken);
        }

        Task RemoveDeviceRegistrationAsync(string registrationId, IETagHolder eTagHolder, CancellationToken cancellationToken)
        {
            ThrowIfClosed();

            if (string.IsNullOrWhiteSpace(eTagHolder.ETag))
            {
                throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingDeviceRegistration);
            }

            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.NotFound,
                    responseMessage => Task.FromResult((Exception) new RegistrationNotFoundException(registrationId))
                }
            };

            return this.httpClientHelper.DeleteAsync<IETagHolder>(GetDeviceRegistrationUri(registrationId), eTagHolder, errorMappingOverrides, null, cancellationToken);
        }

        private static Uri GetEnrollmentUri(string registrationId)
        {
            registrationId = WebUtility.UrlEncode(registrationId);
            return new Uri(EnrollmentUriFormat.FormatInvariant(registrationId, ApiVersionQueryString), UriKind.Relative);
        }

        public static Uri GetBulkEnrollmentUri()
        {
            return new Uri(BulkEnrollmentUriFormat.FormatInvariant(ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetQueryEnrollmentUri()
        {
            return new Uri(QueryEnrollmentUriFormat.FormatInvariant(ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetQueryEnrollmentGroupUri()
        {
            return new Uri(QueryEnrollmentGroupUriFormat.FormatInvariant(ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetEnrollmentGroupUri(string enrollmentGroupId)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(EnrollmentGroupUriFormat.FormatInvariant(enrollmentGroupId, ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetDeviceRegistrationUri(string registrationId)
        {
            registrationId = WebUtility.UrlEncode(registrationId);
            return new Uri(DeviceProvisioningUriFormat.FormatInvariant(registrationId, ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetDeviceRegistrationsUri(string enrollmentGroupId)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(DeviceProvisioningsUriFormat.FormatInvariant(enrollmentGroupId, ApiVersionQueryString), UriKind.Relative);
        }

        static void ValidateRegistrationAndDeviceId(Enrollment enrollment)
        {
            if (enrollment == null)
            {
                throw new ArgumentNullException(nameof(enrollment));
            }

            if (string.IsNullOrWhiteSpace(enrollment.RegistrationId))
            {
                throw new ArgumentException("enrollment.RegistrationId");
            }

            if (!IDRegex.IsMatch(enrollment.RegistrationId))
            {
                throw new ArgumentException(ApiResources.EnrollmentIdNotValid.FormatInvariant(enrollment.RegistrationId));
            }

            if(!string.IsNullOrWhiteSpace(enrollment.DeviceId) &&
               !IDRegex.IsMatch(enrollment.DeviceId))
            {
                throw new ArgumentException(ApiResources.EnrollmentIdNotValid.FormatInvariant(enrollment.DeviceId));
            }
        }

        static void ValidateRegistrationAndDeviceIdForEnrollmentList(IEnumerable<Enrollment> enrollments)
        {
            foreach (Enrollment enrollment in enrollments)
            {
                ValidateRegistrationAndDeviceId(enrollment);
            }
        }

        internal static void ValidateEnrollmentGroup(EnrollmentGroup enrollmentGroup)
        {
            if (enrollmentGroup == null)
            {
                throw new ArgumentNullException(nameof(enrollmentGroup));
            }

            if (string.IsNullOrWhiteSpace(enrollmentGroup.EnrollmentGroupId))
            {
                throw new ArgumentException("enrollmentGroup.EnrollmentGroupId");
            }
        }

        async Task<ProvisioningQueryResult> ExecuteQueryAsync(Uri queryUri, int? pageSize, string continuationToken, CancellationToken cancellationToken)
        {
            ThrowIfClosed();

            //TODO: support sqlQueryStrings

            var customHeaders = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                customHeaders.Add(ContinuationTokenHeader, continuationToken);
            }

            if (pageSize != null)
            {
                customHeaders.Add(PageSizeHeader, pageSize.ToString());
            }

            HttpResponseMessage response = await this.httpClientHelper.PostAsync<QuerySpecification>(
                queryUri,
                new QuerySpecification
                {
                    Sql = string.Empty
                },
                null,
                customHeaders,
                new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" },
                null,
                cancellationToken).ConfigureAwait(false);

            return await ProvisioningQueryResult.FromHttpResponseAsync(response).ConfigureAwait(false);
        }
    }
}
