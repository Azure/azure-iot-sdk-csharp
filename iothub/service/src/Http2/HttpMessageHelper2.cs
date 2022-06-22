// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Http2
{
    internal class HttpMessageHelper2
    {
        private const string ApplicationJson = "application/json";

        internal static HttpRequestMessage CreateRequest(HttpMethod method, Uri requestUri, IotHubConnectionProperties authorizationProvider, object payload = null)
        {
            var message = new HttpRequestMessage();
            message.Method = method;
            message.RequestUri = requestUri;
            message.Headers.Add(HttpRequestHeader.Accept.ToString(), ApplicationJson);
            message.Headers.Add(HttpRequestHeader.Authorization.ToString(), authorizationProvider.GetAuthorizationHeader());
            message.Headers.Add(HttpRequestHeader.UserAgent.ToString(), Utils.GetClientVersion());

            if (payload != null)
            {
                message.Headers.Add(HttpRequestHeader.ContentType.ToString(), ApplicationJson);
                message.Content = GetPayload(payload);
            }

            return message;
        }

        internal static HttpContent GetPayload(object payload)
        {
            string str = JsonConvert.SerializeObject(payload);
            return new StringContent(str, Encoding.UTF8, ApplicationJson);
        }

        internal static async Task<T> DeserializeResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            //TODO do the error code parsing here?
            string str = await response.Content.ReadHttpContentAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(str);
        }
    }
}
