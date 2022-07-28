using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Http2;

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
        private const string DevicesQueryUriFormat = "/devices/query";

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

        internal QueryClient(string hostName, IotHubConnectionProperties credentialProvider, HttpClient httpClient, HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _credentialProvider = credentialProvider;
            _hostName = hostName;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Retrieves a handle through which a result for a given query can be fetched.
        /// </summary>
        /// <param name="sqlQueryString">The SQL query.</param>
        /// <param name="pageSize">The maximum number of items per page.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A handle used to fetch results for a SQL query.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided SQL query string is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided SQL query string is empty or whitespace.</exception>
        /// <exception cref="IotHubException">
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
        public virtual IQuery CreateAsync(string sqlQueryString, int? pageSize = null, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Creating query", nameof(CreateAsync));
            try
            {
                return new Query(async (token) => await ExecuteAsync(
                    sqlQueryString,
                    pageSize,
                    token,
                    cancellationToken));
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(CreateAsync)} threw an exception: {ex}", nameof(CreateAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Creating query", nameof(CreateAsync));
            }
        }

        private async Task<QueryResult> ExecuteAsync(string sqlQueryString, int? pageSize, string continuationToken, CancellationToken cancellationToken)
        {
            Argument.RequireNotNullOrEmpty(sqlQueryString, nameof(sqlQueryString));

            var customHeaders = new Dictionary<string, string>();
            MediaTypeHeaderValue contentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                customHeaders.Add(ContinuationTokenHeader, continuationToken);
            }

            if (pageSize != null)
            {
                customHeaders.Add(PageSizeHeader, pageSize.ToString());
            }

            using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, QueryDevicesRequestUri(), _credentialProvider, new QuerySpecification { Sql = sqlQueryString });
            AddCustomHeaders(request, customHeaders, contentType);
            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
            return await QueryResult.FromHttpResponseAsync(response).ConfigureAwait(false);
        }

        private static Uri QueryDevicesRequestUri()
        {
            return new Uri(DevicesQueryUriFormat, UriKind.Relative);
        }

        private static void AddCustomHeaders(HttpRequestMessage requestMessage, IDictionary<string, string> customHeaders, MediaTypeHeaderValue contentType)
        {
            if (customHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in customHeaders)
                {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }
            }
            requestMessage.Content.Headers.ContentType = contentType;
        }
    }
}