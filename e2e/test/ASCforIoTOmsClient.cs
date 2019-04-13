using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class ASCforIoTOmsClient : IDisposable
    {
        private const string Audience =  "https://api.loganalytics.io/";
        private const string QueryUriTemplate = "https://api.loganalytics.io/{0}/workspaces/{1}/query";
        private const string LogAnalyticsApiVersion = "v1";
        private const string RawEventQueryTemplate =
            @"SecurityIoTRawEvent
    | where DeviceId == ""{0}""
    | where IoTRawEventId == ""{1}""";
        
        private readonly string _workspaceId = Configuration.Oms.WorkspacedId;
        private readonly string _aadTenant = Configuration.Oms.AadTenant;
        private readonly string _appId = Configuration.Oms.AadAppId;
        private readonly string _appKey = Configuration.Oms.AadAppKey;

        private readonly HttpClient _client;
        private readonly AuthenticationContext _authenticationContext;
        private readonly string _queryUri;

        public static ASCforIoTOmsClient CreateClient()
        {
            return new ASCforIoTOmsClient();
        }

        private ASCforIoTOmsClient()
        {
            _client = new HttpClient();
            _authenticationContext = new AuthenticationContext("https://login.windows.net/" + _aadTenant);
            _queryUri = string.Format(CultureInfo.InvariantCulture, QueryUriTemplate, LogAnalyticsApiVersion, _workspaceId);
        }

        public async Task<bool> IsRawEventExist(string deviceId, string eventId)
        {
            bool isEventExist = false;
            string query = string.Format(CultureInfo.InvariantCulture, RawEventQueryTemplate, deviceId, eventId);
            var sw = new Stopwatch();
            sw.Start();
            while (!isEventExist && sw.Elapsed.TotalMinutes < 30)
            {
                isEventExist = await QueryMessage(query).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            }

            sw.Stop();
            return isEventExist;
        }

        private async Task<bool> QueryMessage(string query)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _queryUri))
            {
                string accessToken = await GetAccessToken().ConfigureAwait(false);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = CreateRequestContent(query);
                using (HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false))
                {
                    string responseAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    JObject responseBody = JObject.Parse(responseAsString);

                    return responseBody["tables"][0]["rows"].HasValues;
                }
            }
        }

        private async Task<string> GetAccessToken()
        {
            ClientCredential cc = new ClientCredential(_appId, _appKey);
            AuthenticationResult result = await _authenticationContext.AcquireTokenAsync(Audience, cc).ConfigureAwait(false);

            return result.AccessToken;
        }

        private StringContent CreateRequestContent(string query)
        {
            JObject body = new JObject();
            body["query"] = query;
            body["timespan"] = TimeSpan.FromHours(2);

            StringContent content = new StringContent(JsonConvert.SerializeObject(body));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return content;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
