// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> for executing queries using a SQL-like syntax.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-query-language"/>
    public class QueryClient
    {
        private const string JobTypeFormat = "&jobType={0}";
        private const string JobStatusFormat = "&jobStatus={0}";
        private const string ContinuationTokenHeader = "x-ms-continuation";
        private const string PageSizeHeader = "x-ms-max-item-count";
        private const string DevicesQueryUriFormat = "/devices/query";
        private const string JobsQueryFormat = "/jobs/v2/query";

        private string _hostName;
        private IotHubConnectionProperties _credentialProvider;
        private HttpClient _httpClient;
        private HttpRequestMessageFactory _httpRequestMessageFactory;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected QueryClient()
        {
        }

        internal QueryClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            HttpClient httpClient,
            HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _credentialProvider = credentialProvider;
            _hostName = hostName;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Execute a query on your IoT hub and get an iterable set of the queried items. The kind of iterable items returned
        /// by this query will depend on the query provided.
        /// </summary>
        /// <param name="query">The query. See <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-query-language">this document</see> for more details on how to build this query.</param>
        /// <param name="options">The optional parameters to execute the query with.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <typeparam name="T">
        /// The type to deserialize the set of items into. For example, when running a query like "SELECT * FROM devices",
        /// this type should be <see cref="Twin"/>. When running a query like "SELECT * FROM devices.jobs", this type should be
        /// <see cref="ScheduledJob"/>.
        /// </typeparam>
        /// <returns>An iterable set of the queried items.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided <paramref name="query"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-query-language"/>
        /// <example>
        /// <c>
        /// QueryResponse&lt;Twin&gt; queriedTwins = await iotHubServiceClient.Query.CreateAsync&lt;Twin&gt;("SELECT * FROM devices");
        /// while (await queriedTwins.MoveNextAsync())
        /// {
        ///     Twin queriedTwin = queriedTwins.Current;
        ///     Console.WriteLine(queriedTwin);
        /// }
        /// </c>
        /// </example>
        /// <example>
        /// <c>
        /// QueryResponse&lt;ScheduledJob&gt; queriedJobs = await iotHubServiceClient.Query.CreateAsync&lt;ScheduledJob&gt;("SELECT * FROM devices.jobs");
        /// while (await queriedJobs.MoveNextAsync())
        /// {
        ///     ScheduledJob queriedJob = queriedJobs.Current;
        ///     Console.WriteLine(queriedJob);
        /// }
        /// </c>
        /// </example>
        public virtual async Task<QueryResponse<T>> CreateAsync<T>(string query, QueryOptions options = default, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Creating query", nameof(CreateAsync));
            try
            {
                Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, QueryDevicesRequestUri(), _credentialProvider, new QuerySpecification { Sql = query });
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                if (!string.IsNullOrWhiteSpace(options?.ContinuationToken))
                {
                    request.Headers.Add(ContinuationTokenHeader, options?.ContinuationToken);
                }

                if (options?.PageSize != null)
                {
                    request.Headers.Add(PageSizeHeader, options.PageSize.ToString());
                }

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                string responsePayload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var page = new QueriedPage<T>(response, responsePayload);
                return new QueryResponse<T>(this, query, page.Items, page.ContinuationToken, options?.PageSize);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(CreateAsync)} threw an exception: {ex}", nameof(CreateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Creating query", nameof(CreateAsync));
            }
        }

        /// <summary>
        /// Query all jobs or query jobs by type and/or status.
        /// </summary>
        /// <param name="jobType">The type of the jobs to return in the query. If null, jobs of all types will be returned</param>
        /// <param name="jobStatus">The status of the jobs to return in the query. If null, jobs of all states will be returned</param>
        /// <param name="options">The optional parameters to run the query with.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>An iterable set of the queried jobs.</returns>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <example>
        /// <c>
        /// QueryResponse&lt;ScheduledJob&gt; queriedJobs = await iotHubServiceClient.Query.CreateAsync();
        /// while (await queriedJobs.MoveNextAsync())
        /// {
        ///     ScheduledJob queriedJob = queriedJobs.Current;
        ///     Console.WriteLine(queriedJob);
        /// }
        /// </c>
        /// </example>

        public virtual async Task<QueryResponse<ScheduledJob>> CreateAsync(JobType? jobType = null, JobStatus? jobStatus = null, QueryOptions options = default, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"jobType=[{jobType}], jobStatus=[{jobStatus}], pageSize=[{options?.PageSize}]", nameof(CreateAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, new Uri(JobsQueryFormat, UriKind.Relative), _credentialProvider, null, BuildQueryJobUri(jobType, jobStatus));

                var customHeaders = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(options?.ContinuationToken))
                {
                    request.Headers.Add(ContinuationTokenHeader, options?.ContinuationToken);
                }

                if (options?.PageSize != null)
                {
                    request.Headers.Add(PageSizeHeader, options.PageSize.ToString());
                }

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                string responsePayload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                QueriedPage<ScheduledJob> page = new QueriedPage<ScheduledJob>(response, responsePayload);
                return new QueryResponse<ScheduledJob>(this, jobType, jobStatus, page.Items, page.ContinuationToken, options?.PageSize);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(CreateAsync)} threw an exception: {ex}", nameof(CreateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"jobType=[{jobType}], jobStatus=[{jobStatus}], pageSize=[{options?.PageSize}]", nameof(CreateAsync));
            }
        }

        private static Uri QueryDevicesRequestUri()
        {
            return new Uri(DevicesQueryUriFormat, UriKind.Relative);
        }

        private static string BuildQueryJobUri(JobType? jobType, JobStatus? jobStatus)
        {
            var stringBuilder = new StringBuilder();

            if (jobType != null)
            {
                stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, JobTypeFormat, jobType.ToString()));
            }

            if (jobStatus != null)
            {
                stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, JobStatusFormat, jobStatus.ToString()));
            }

            return stringBuilder.ToString();
        }
    }
}
