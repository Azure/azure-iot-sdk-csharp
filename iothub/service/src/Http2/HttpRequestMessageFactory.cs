// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;

namespace Microsoft.Azure.Devices.Http2
{
    /// <summary>
    /// Factory for creating HTTP requests to be sent by a service client. The requests created by
    /// this client contain all the common headers and attributes
    /// </summary>
    internal class HttpRequestMessageFactory
    {
        private const string ApplicationJson = "application/json";

        private Uri _baseUri;
        private string _apiVersionQueryString;

        /// <summary>
        /// Constructor for mocking purposes only.
        /// </summary>
        protected HttpRequestMessageFactory()
        {
        }

        public HttpRequestMessageFactory(Uri baseUri, string apiVersion)
        {
            _baseUri = baseUri;
            _apiVersionQueryString = "?api-version=" + apiVersion;
        }

        /// <summary>
        /// This helper method constructs the minimal HTTP request used by all HTTP service clients. It adds
        /// the mandatory and optional headers that all requests should use.
        /// </summary>
        /// <param name="method">The HTTP method of the request to build.</param>
        /// <param name="relativeUri">The URI that the request will be made to.</param>
        /// <param name="authorizationProvider">The provider of authorization tokens.</param>
        /// <param name="payload">The payload for the request to be serialized. If null, no payload will be in the request.</param>
        /// <param name="queryStringParameters">Additional query string parameters to be added to request URI.</param>
        /// <returns>The created HTTP request.</returns>
        internal HttpRequestMessage CreateRequest(HttpMethod method, Uri relativeUri, IotHubConnectionProperties authorizationProvider, object payload = null, string queryStringParameters = null)
        {
            var message = new HttpRequestMessage();
            message.Method = method;
            message.RequestUri = new Uri(_baseUri, relativeUri.ToString() + _apiVersionQueryString + (string.IsNullOrWhiteSpace(queryStringParameters) ? "" : queryStringParameters));
            message.Headers.Add(HttpRequestHeader.Accept.ToString(), ApplicationJson);
            message.Headers.Add(HttpRequestHeader.Authorization.ToString(), authorizationProvider.GetAuthorizationHeader());
            message.Headers.Add(HttpRequestHeader.UserAgent.ToString(), Utils.GetClientVersion());

            if (payload != null)
            {
                message.Headers.Add(HttpRequestHeader.ContentType.ToString(), ApplicationJson);

                if (payload != null)
                {
                    if (payload is string)
                    {
                        // This is a special case reserved for the digital twins subclient where
                        // users are expected to pass in an already serialized string. For example,
                        // invoking a digital twin command or updating a digital twin both use
                        // this functionality.
                        message.Content = new StringContent((string)payload);
                    }
                    else
                    {
                        message.Content = HttpMessageHelper2.SerializePayload(payload);
                    }
                }
            }

            return message;
        }
    }
}
