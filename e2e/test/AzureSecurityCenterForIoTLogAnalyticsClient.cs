// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
#if !NET451
using Microsoft.Azure.OperationalInsights;
using Microsoft.Rest;
#endif
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class AzureSecurityCenterForIoTLogAnalyticsClient : IDisposable
    {
        //OperationalInsights sdk is not supported on .NET 451
        //In .NET451 this class uses LogAnaltics REST api 

        //Azure Active Directory authentication authority for public cloud
        private const string AuthenticationAuthorityTemplate = "https://login.windows.net/{0}";
        //Azure Log Analytics Authentication token audience
        private const string Audience =  "https://api.loganalytics.io/";
        //Azure Log Analytics query URL
        private const string QueryUriTemplate = "https://api.loganalytics.io/{0}/workspaces/{1}/query";
        //Azure Log Analytics API version
        private const string LogAnalyticsApiVersion = "v1";
        //Query template for querying a SecurityIoTRawEvent by device id and raw event id
        private const string RawEventQueryTemplate =
            @"SecurityIoTRawEvent
    | where DeviceId == ""{0}""
    | where IoTRawEventId == ""{1}""";
        
        private readonly string _workspaceId = Configuration.AzureSecurityCenterForIoTLogAnalytics.WorkspacedId;
        private readonly string _aadTenant = Configuration.AzureSecurityCenterForIoTLogAnalytics.AadTenant;
        private readonly string _appId = Configuration.AzureSecurityCenterForIoTLogAnalytics.AadAppId;
        private readonly string _appCertificate = Configuration.AzureSecurityCenterForIoTLogAnalytics.AadAppCertificate;

        private readonly TimeSpan _polingInterval = TimeSpan.FromSeconds(20);

        private readonly AuthenticationContext _authenticationContext;
        private readonly IClientAssertionCertificate _certificateAssertion;

#if NET451 //http client and a REST Log Analytics api query URI
        private readonly HttpClient _client;
        private readonly string _queryUri;
#endif

        public static AzureSecurityCenterForIoTLogAnalyticsClient CreateClient()
        {
            return new AzureSecurityCenterForIoTLogAnalyticsClient();
        }

        private AzureSecurityCenterForIoTLogAnalyticsClient()
        {
#if NET451
            _client = new HttpClient();
            _queryUri = string.Format(CultureInfo.InvariantCulture, QueryUriTemplate, LogAnalyticsApiVersion, _workspaceId);
#endif
            string authority = string.Format(CultureInfo.InvariantCulture, AuthenticationAuthorityTemplate, _aadTenant);
            _authenticationContext = new AuthenticationContext(authority);
            var cert = new X509Certificate2(Convert.FromBase64String(_appCertificate));
            _certificateAssertion = new ClientAssertionCertificate(_appId, cert);

        }

        public async Task<bool> IsRawEventExist(string deviceId, string eventId)
        {
            bool isEventExist = false;
            string query = string.Format(CultureInfo.InvariantCulture, RawEventQueryTemplate, deviceId, eventId);
            var sw = new Stopwatch();
            sw.Start();
            while (!isEventExist && sw.Elapsed.TotalMinutes < 30)
            {
                isEventExist = await DoQuery(query).ConfigureAwait(false);
                await Task.Delay(_polingInterval).ConfigureAwait(false);
            }

            sw.Stop();
            return isEventExist;
        }

        private async Task<bool> DoQuery(string query)
        {
            string accessToken = await GetAccessToken().ConfigureAwait(false);
#if NET451
            return await DoQueryHttpClient(query).ConfigureAwait(false);
#else
            return await DoQueryLogAnalyticsClient(query).ConfigureAwait(false);
#endif
        }

#if NET451
        private async Task<bool> DoQueryHttpClient(string query)
        {
            string accessToken = await GetAccessToken().ConfigureAwait(false);
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _queryUri))
            {
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

        private StringContent CreateRequestContent(string query)
        {
            JObject body = new JObject();
            body["query"] = query;
            body["timespan"] = TimeSpan.FromHours(2);

            StringContent content = new StringContent(JsonConvert.SerializeObject(body));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return content;
        }
#else 
        private async Task<bool> DoQueryLogAnalyticsClient(string query)
        {
            string accessToken = await GetAccessToken().ConfigureAwait(false);
            TokenCredentials creds = new TokenCredentials(accessToken);
            using (OperationalInsightsDataClient client = new OperationalInsightsDataClient(creds))
            {
                client.WorkspaceId = _workspaceId;
                var result = client.Query(query);
                return result.Results.Any();
            }
        }
#endif

        private async Task<string> GetAccessToken()
        {
            AuthenticationResult result = await _authenticationContext.AcquireTokenAsync(Audience, _certificateAssertion).ConfigureAwait(false);
            return result.AccessToken;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
#if NET451
                _client.Dispose();
#endif
            }
        }
    }
}
