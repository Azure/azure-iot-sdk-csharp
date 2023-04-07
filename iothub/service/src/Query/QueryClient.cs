// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Azure;

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
        /// Iterate over twins:
        /// <code language="csharp">
        /// AsyncPageable&lt;Twin&gt; twinsQuery = iotHubServiceClient.Query.Create&lt;ClientTwin&gt;("SELECT * FROM devices");
        /// await foreach (Twin queriedTwin in twinsQuery)
        /// {
        ///     Console.WriteLine(queriedTwin.DeviceId);
        /// }
        /// </code>
        /// Or scheduled jobs:
        /// <code language="csharp">
        /// AsyncPageable&lt;ScheduledJob&gt; jobsQuery = iotHubServiceClient.Query.Create&lt;ScheduledJob&gt;("SELECT * FROM devices.jobs");
        /// await foreach (ScheduledJob queriedJob in jobsQuery)
        /// {
        ///     Console.WriteLine(queriedJob);
        /// }
        /// </code>
        /// Iterate over pages of twins:
        /// <code language="csharp">
        /// IAsyncEnumerable&lt;Page&lt;ClientTwin&gt;&gt; twinsQuery = iotHubServiceClient.Query.Create&lt;ClientTwin&gt;("SELECT * FROM devices").AsPages();
        /// await foreach (Page&lt;ClientTwin&gt; queriedTwinsPage in twinsQuery)
        /// {
        ///     foreach (ClientTwin queriedTwin in queriedTwinsPage.Values)
        ///     {
        ///         Console.WriteLine(queriedTwin.DeviceId);
        ///     }
        ///     
        ///     // Note that this is disposed for you while iterating item-by-item, but not when
        ///     // iterating page-by-page. That is why this sample has to manually call dispose
        ///     // on the response object here.
        ///     queriedTwinsPage.GetRawResponse().Dispose();
        /// }
        /// </code>
        /// </example>
        public virtual AsyncPageable<T> Create<T>(string query, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Creating query.", nameof(Create));

            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                async Task<Page<T>> nextPageFunc(string continuationToken, int? pageSizeHint)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                        HttpMethod.Post,
                        s_queryUri,
                        _credentialProvider,
                        new QuerySpecification { Sql = query });

                    return await BuildAndSendRequestAsync<T>(request, continuationToken, pageSizeHint, cancellationToken).ConfigureAwait(false);
                }

                async Task<Page<T>> firstPageFunc(int? pageSizeHint)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                        HttpMethod.Post,
                        s_queryUri,
                        _credentialProvider,
                        new QuerySpecification { Sql = query });

                    return await BuildAndSendRequestAsync<T>(request, null, pageSizeHint, cancellationToken).ConfigureAwait(false);
                }

                return PageableHelpers.CreateAsyncEnumerable(firstPageFunc, nextPageFunc, null);
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, $"Creating query threw an exception: {ex}", nameof(Create));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Creating query.", nameof(Create));
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
        /// Iterate over jobs:
        /// <code language="csharp">
        /// AsyncPageable&lt;ScheduledJob&gt; jobsQuery = iotHubServiceClient.Query.CreateJobsQuery();
        /// await foreach (ScheduledJob scheduledJob in jobsQuery)
        /// {
        ///     Console.WriteLine(scheduledJob.JobId);
        /// }
        /// </code>
        /// Iterate over pages of twins:
        /// <code language="csharp">
        /// IAsyncEnumerable&lt;Page&lt;ScheduledJob&gt;&gt; jobsQuery = iotHubServiceClient.Query.CreateJobsQuery().AsPages();
        /// await foreach (Page&lt;ScheduledJob&gt; scheduledJobsPage in jobsQuery)
        /// {
        ///     foreach (ScheduledJob scheduledJob in scheduledJobsPage.Values)
        ///     {
        ///         Console.WriteLine(scheduledJob.JobId);
        ///     }
        ///     
        ///     // Note that this is disposed for you while iterating item-by-item, but not when
        ///     // iterating page-by-page. That is why this sample has to manually call dispose
        ///     // on the response object here.
        ///     scheduledJobsPage.GetRawResponse().Dispose();
        /// }
        /// </code>
        /// </example>
        public virtual AsyncPageable<ScheduledJob> CreateJobsQuery(JobQueryOptions options = default, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Creating query with jobType: {options?.JobType}, jobStatus: {options?.JobStatus}", nameof(Create));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                async Task<Page<ScheduledJob>> nextPageFunc(string continuationToken, int? pageSizeHint)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                        HttpMethod.Get,
                        s_jobsQueryFormat,
                        _credentialProvider,
                        null,
                        BuildQueryJobQueryString(options));

                    return await BuildAndSendRequestAsync<ScheduledJob>(request, continuationToken, pageSizeHint, cancellationToken).ConfigureAwait(false);
                }

                async Task<Page<ScheduledJob>> firstPageFunc(int? pageSizeHint)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                        HttpMethod.Get,
                        s_jobsQueryFormat,
                        _credentialProvider,
                        null,
                        BuildQueryJobQueryString(options));

                    return await BuildAndSendRequestAsync<ScheduledJob>(request, null, pageSizeHint, cancellationToken).ConfigureAwait(false);
                }

                return PageableHelpers.CreateAsyncEnumerable(firstPageFunc, nextPageFunc);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.RequestTimeout, null, ex);
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, $"Creating query with jobType: {options?.JobType}, jobStatus: {options?.JobStatus} threw an exception: {ex}", nameof(Create));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Creating query with jobType: {options?.JobType}, jobStatus: {options?.JobStatus}", nameof(Create));
            }
        }

        private async Task<Page<T>> BuildAndSendRequestAsync<T>(HttpRequestMessage request, string continuationToken, int? pageSizeHint, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (!string.IsNullOrWhiteSpace(continuationToken))
            { 
                request.Headers.Add(ContinuationTokenHeader, continuationToken);
            }

            if (pageSizeHint != null)
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
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.RequestTimeout, null, ex);
            }

            await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
            Stream bodyStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using StreamReader bodyStreamReader = new StreamReader(bodyStream);
            string responsePayload = await bodyStreamReader.ReadToEndAsync().ConfigureAwait(false);
            QueriedPage<T> page = new QueriedPage<T>(response, responsePayload);
#pragma warning disable CA2000 // Dispose objects before losing scope
            // The disposable QueryResponse object is the user's responsibility, not the SDK's
            return Page<T>.FromValues(page.Items, page.ContinuationToken, new QueryResponse(response, bodyStream));
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
