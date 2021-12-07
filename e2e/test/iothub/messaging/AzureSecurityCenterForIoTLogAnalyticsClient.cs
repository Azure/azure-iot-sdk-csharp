// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FluentAssertions;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    public class AzureSecurityCenterForIoTLogAnalyticsClient : IDisposable
    {
        //OperationalInsights sdk is not supported on .NET 451
        //In .NET451 this class uses LogAnaltics REST api

        //Azure Active Directory authentication authority for public cloud
        private const string AuthenticationAuthorityTemplate = "https://login.windows.net/{0}";

        //Azure Log Analytics Authentication token audience
        private const string Audience = "https://api.loganalytics.io/";

        //Azure Log Analytics query URL
        private const string QueryUriTemplate = "https://api.loganalytics.io/{0}/workspaces/{1}/query";

        //Azure Log Analytics API version
        private const string LogAnalyticsApiVersion = "v1";

        //Query template for querying a SecurityIoTRawEvent by device id and raw event id
        private const string RawEventQueryTemplate =
            @"SecurityIoTRawEvent
    | where DeviceId == ""{0}""
    | where IoTRawEventId == ""{1}""";

        private readonly string _workspaceId = TestConfiguration.AzureSecurityCenterForIoTLogAnalytics.WorkspacedId;
        private readonly string _aadTenant = TestConfiguration.AzureSecurityCenterForIoTLogAnalytics.AadTenant;
        private readonly string _appId = TestConfiguration.AzureSecurityCenterForIoTLogAnalytics.AadAppId;
        private readonly string _appCertificate = TestConfiguration.AzureSecurityCenterForIoTLogAnalytics.AadAppCertificate;

        private readonly TimeSpan _polingInterval = TimeSpan.FromSeconds(20);
        private readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);

        private readonly AuthenticationContext _authenticationContext;
        private readonly IClientAssertionCertificate _certificateAssertion;

        private readonly X509Certificate2 _authCertificate;

        //These are used in NET451 instead of OperationalInsights SDK
        private readonly HttpClient _client;

        private readonly string _queryUri;

        public static AzureSecurityCenterForIoTLogAnalyticsClient CreateClient()
        {
            return new AzureSecurityCenterForIoTLogAnalyticsClient();
        }

        private AzureSecurityCenterForIoTLogAnalyticsClient()
        {
            _client = new HttpClient();
            _queryUri = string.Format(CultureInfo.InvariantCulture, QueryUriTemplate, LogAnalyticsApiVersion, _workspaceId);
            string authority = string.Format(CultureInfo.InvariantCulture, AuthenticationAuthorityTemplate, _aadTenant);
            _authenticationContext = new AuthenticationContext(authority);
            _authCertificate = new X509Certificate2(Convert.FromBase64String(_appCertificate));
            _certificateAssertion = new ClientAssertionCertificate(_appId, _authCertificate);
        }

        public async Task<bool> IsRawEventExist(string deviceId, string eventId)
        {
            string query = string.Format(CultureInfo.InvariantCulture, RawEventQueryTemplate, deviceId, eventId);
            return await QueryEventHttpClient(query).ConfigureAwait(false);
        }

        private async Task<bool> QueryEventHttpClient(string query)
        {
            bool isEventExist = false;
            var sw = new Stopwatch();
            sw.Start();
            while (!isEventExist && sw.Elapsed < _timeout)
            {
                isEventExist = await DoQueryHttpClient(query).ConfigureAwait(false);
                await Task.Delay(_polingInterval).ConfigureAwait(false);
            }

            sw.Stop();
            return isEventExist;
        }

        private async Task<bool> DoQueryHttpClient(string query)
        {
            string accessToken = await GetAccessToken().ConfigureAwait(false);
            using (var request = new HttpRequestMessage(HttpMethod.Post, _queryUri))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = CreateRequestContent(query);
                using (HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false))
                {
                    response.IsSuccessStatusCode.Should().BeTrue();
                    string responseAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var responseBody = JObject.Parse(responseAsString);
                    return responseBody["tables"][0]["rows"].HasValues;
                }
            }
        }

        private StringContent CreateRequestContent(string query)
        {
            var body = new JObject();
            body["query"] = query;
            body["timespan"] = TimeSpan.FromHours(2);

            var content = new StringContent(JsonConvert.SerializeObject(body));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return content;
        }

        private async Task<string> GetAccessToken()
        {
            AuthenticationResult result = await _authenticationContext
                .AcquireTokenAsync(Audience, _certificateAssertion).ConfigureAwait(false);
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
                _client?.Dispose();

                // X509Certificate needs to be disposed for implementations !NET451 (NET451 doesn't implement X509Certificates as IDisposable).
                if (_authCertificate is IDisposable disposableCert)
                {
                    disposableCert?.Dispose();
                }
            }
        }
    }
}
