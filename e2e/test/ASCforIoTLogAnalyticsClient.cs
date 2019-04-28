// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public class ASCforIoTLogAnalyticsClient : IDisposable
    {
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
        
        private readonly string _workspaceId = Configuration.ASCforIoTLogAnalytics.WorkspacedId;
        private readonly string _aadTenant = Configuration.ASCforIoTLogAnalytics.AadTenant;
        private readonly string _appId = Configuration.ASCforIoTLogAnalytics.AadAppId;
        private readonly string _appKey = Configuration.ASCforIoTLogAnalytics.AadAppKey;

        private readonly TimeSpan _polingInterval = TimeSpan.FromSeconds(20);

        private readonly HttpClient _client;
        private readonly AuthenticationContext _authenticationContext;
        private readonly string _queryUri;

        public static ASCforIoTLogAnalyticsClient CreateClient()
        {
            return new ASCforIoTLogAnalyticsClient();
        }

        private ASCforIoTLogAnalyticsClient()
        {
            _client = new HttpClient();
            string authority = string.Format(CultureInfo.InvariantCulture, AuthenticationAuthorityTemplate, _aadTenant);
            _authenticationContext = new AuthenticationContext(authority);
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
                isEventExist = await DoQuery(query).ConfigureAwait(false);
                await Task.Delay(_polingInterval).ConfigureAwait(false);
            }

            sw.Stop();
            return isEventExist;
        }

        private async Task<bool> DoQuery(string query)
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.Dispose();
            }
        }
    }
}
