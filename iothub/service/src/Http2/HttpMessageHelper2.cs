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
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Http2
{
    internal class HttpMessageHelper2
    {
        private const string ApplicationJson = "application/json";

        /// <summary>
        /// This helper method constructs the minimal HTTP request used by all HTTP service clients. It adds
        /// the mandatory and optional headers that all requests should use.
        /// </summary>
        /// <param name="method">The HTTP method of the request to build.</param>
        /// <param name="requestUri">The URI that the request will be made to.</param>
        /// <param name="authorizationProvider">The provider of authorization tokens.</param>
        /// <param name="payload">The payload for the request to be serialized. If null, no payload will be in the request.</param>
        /// <returns>The created HTTP request.</returns>
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

        /// <summary>
        /// Helper method for serializing payload objects.
        /// </summary>
        /// <param name="payload">The payload object to serialize.</param>
        /// <returns>The serialized HttpContent.</returns>
        internal static HttpContent GetPayload(object payload)
        {
            string str = JsonConvert.SerializeObject(payload);
            return new StringContent(str, Encoding.UTF8, ApplicationJson);
        }

        /// <summary>
        /// Throws the appropriate exception if the received HTTP status code differs from the expected status code.
        /// </summary>
        /// <param name="expectedHttpStatusCode">The HTTP status code that indicates that the operation was a success and no exception should be thrown.</param>
        /// <param name="responseMessage">The HTTP response that contains the actual status code as well as the payload that contains error details if an error occurred.</param>
        /// <returns>Task.</returns>
        internal static async Task ValidateHttpResponseStatus(HttpStatusCode expectedHttpStatusCode, HttpResponseMessage responseMessage)
        {
            if (expectedHttpStatusCode != responseMessage.StatusCode)
            {
                throw await ExceptionHandlingHelper.GetDefaultErrorMapping()[responseMessage.StatusCode].Invoke(responseMessage);
            }
        }

        /// <summary>
        /// Deserializes the payload of the HTTP response into the provided object type.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize the response payload into.</typeparam>
        /// <param name="response">The HTTP response containing the payload to deserialize.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The deserialized object.</returns>
        internal static async Task<T> DeserializeResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            string str = await response.Content.ReadHttpContentAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(str);
        }
    }
}
