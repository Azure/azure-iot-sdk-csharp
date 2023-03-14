// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Microsoft.Azure.Devices.Query;
using static Microsoft.Azure.Devices.Amqp.AmqpClientHelper;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> for executing queries using a SQL-like syntax.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-query-language"/>
    public class QueryClient
    {
        private const string ContinuationTokenHeader = "x-ms-continuation";
        private const string PageSizeHeader = "x-ms-max-item-count";

        private static readonly Uri s_jobsQueryFormat = new("/jobs/v2/query", UriKind.Relative);
        private static readonly Uri s_queryUri = new("/devices/query", UriKind.Relative);

        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;
        private readonly RetryHandler _internalRetryHandler;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected QueryClient()
        { }

        internal QueryClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            HttpClient httpClient,
            HttpRequestMessageFactory httpRequestMessageFactory,
            RetryHandler retryHandler)
        {
            _credentialProvider = credentialProvider;
            _hostName = hostName;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
            _internalRetryHandler = retryHandler;
        }

        /// <summary>
        /// Execute a query on your IoT hub and get an iterable set of the queried items.
        /// </summary>
        /// <remarks>
        /// The kind of iterable items returned by this query will depend on the query provided.
        /// </remarks>
        /// <param name="query">The query. See <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-query-language">this document</see>
        /// for more details on how to build this query.</param>
        /// <param name="options">The optional parameters to execute the query with.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <typeparam name="T">
        /// The type to deserialize the set of items into. For example, when running a query like "SELECT * FROM devices",
        /// this type should be <see cref="ClientTwin"/>. When running a query like "SELECT * FROM devices.jobs", this type should be
        /// <see cref="ScheduledJob"/>.
        /// </typeparam>
        /// <returns>An iterable set of the queried items.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-query-language"/>
        /// <example>
        /// Iterate twins:
        /// <code language="csharp">
        /// AsyncPageable&lt;Twin&gt; twinQuery = iotHubServiceClient.Query.CreateAsync&lt;Twin&gt;("SELECT * FROM devices");
        /// await foreach (Twin queriedTwin in twinQuery)
        /// {
        ///     Console.WriteLine(queriedTwin);
        /// }
        /// </code>
        /// Or scheduled jobs:
        /// <code language="csharp">
        /// AsyncPageable&lt;ScheduledJob&gt; jobsQuery = await iotHubServiceClient.Query.CreateAsync&lt;ScheduledJob&gt;("SELECT * FROM devices.jobs");
        /// await foreach (ScheduledJob queriedJob in jobsQuery)
        /// {
        ///     Console.WriteLine(queriedJob);
        /// }
        /// </code>
        /// </example>
        public virtual AsyncPageable<T> CreateAsync<T>(string query, QueryOptions options = default, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Creating query.", nameof(CreateAsync));

            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                async Task<Page<T>> nextPageFunc(string continuationToken, int? pageSizeHint)
                {
                    using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                        HttpMethod.Post,
                        s_queryUri,
                        _credentialProvider,
                        new QuerySpecification { Sql = query });

                    return await BuildAndSendRequest<T>(request, options, continuationToken, pageSizeHint, cancellationToken);
                }

                async Task<Page<T>> firstPageFunc(int? pageSizeHint)
                {
                    using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                        HttpMethod.Post,
                        s_queryUri,
                        _credentialProvider,
                        new QuerySpecification { Sql = query });

                    return await BuildAndSendRequest<T>(request, options, null, pageSizeHint, cancellationToken);
                }

                return PageableHelpers.CreateAsyncEnumerable(firstPageFunc, nextPageFunc, options?.PageSize);
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, $"Creating query threw an exception: {ex}", nameof(CreateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Creating query.", nameof(CreateAsync));
            }
        }

        /// <summary>
        /// Query all jobs or query jobs by type and/or status.
        /// </summary>
        /// <param name="options">The optional parameters to run the query with.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>An iterable set of the queried jobs.</returns>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <example>
        /// <code language="csharp">
        /// AsyncPageable&lt;ScheduledJob&gt; jobsQuery = await iotHubServiceClient.Query.CreateJobsQueryAsync();
        /// await foreach (ScheduledJob scheduledJob in jobsQuery)
        /// {
        ///     Console.WriteLine(scheduledJob.JobId);
        /// }
        /// </code>
        /// </example>
        public virtual AsyncPageable<ScheduledJob> CreateJobsQueryAsync(JobQueryOptions options = default, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Creating query with jobType: {options?.JobType}, jobStatus: {options?.JobStatus}, pageSize: {options?.PageSize}", nameof(CreateAsync));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                async Task<Page<ScheduledJob>> nextPageFunc(string continuationToken, int? pageSizeHint)
                {
                    using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                                        HttpMethod.Get,
                                        s_jobsQueryFormat,
                                        _credentialProvider,
                                        null,
                                        BuildQueryJobQueryString(options));

                    return await BuildAndSendRequest<ScheduledJob>(request, options, continuationToken, pageSizeHint, cancellationToken);
                }

                async Task<Page<ScheduledJob>> firstPageFunc(int? pageSizeHint)
                {
                    using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                                        HttpMethod.Get,
                                        s_jobsQueryFormat,
                                        _credentialProvider,
                                        null,
                                        BuildQueryJobQueryString(options));

                    return await BuildAndSendRequest<ScheduledJob>(request, options, null, pageSizeHint, cancellationToken);
                }

                return PageableHelpers.CreateAsyncEnumerable(firstPageFunc, nextPageFunc, options?.PageSize);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, $"Creating query with jobType: {options?.JobType}, jobStatus: {options?.JobStatus}, pageSize: {options?.PageSize} threw an exception: {ex}", nameof(CreateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Creating query with jobType: {options?.JobType}, jobStatus: {options?.JobStatus}, pageSize: {options?.PageSize}", nameof(CreateAsync));
            }
        }

        private async Task<Page<T>> BuildAndSendRequest<T>(HttpRequestMessage request, QueryOptions options, string continuationToken, int? pageSizeHint, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(options?.ContinuationToken))
            {
                request.Headers.Add(ContinuationTokenHeader, options?.ContinuationToken);
            }
            else if (!string.IsNullOrWhiteSpace(continuationToken))
            { 
                request.Headers.Add(ContinuationTokenHeader, continuationToken);
            }

            if (options?.PageSize != null)
            {
                request.Headers.Add(PageSizeHeader, options.PageSize.ToString());
            }
            else if (pageSizeHint != null)
            {
                request.Headers.Add(PageSizeHeader, pageSizeHint.ToString());
            }

            if (request.Content != null)
            { 
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") 
                { 
                    CharSet = "utf-8" 
                };
            }

            HttpResponseMessage response = null;

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
            }

            await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
            string responsePayload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            QueriedPage<T> page = new QueriedPage<T>(response, responsePayload);
#pragma warning disable CA2000 // Dispose objects before losing scope
            // The disposable QueryResponse object is the user's responsibility, not the SDK's
            return Page<T>.FromValues(page.Items, page.ContinuationToken, new QueryResponse(response));
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        private static string BuildQueryJobQueryString(JobQueryOptions options)
        {
            string queryString = "";

            if (options?.JobType != null)
            {
                queryString += $"&jobType={options.JobType.Value}";
            }

            if (options?.JobStatus != null)
            {
                queryString += $"&jobStatus={options.JobStatus.Value}";
            }

            return queryString;
        }
    }
}
