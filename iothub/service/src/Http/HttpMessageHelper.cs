// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A helper class for constructing HTTP requests and parsing HTTP responses.
    /// </summary>
    internal class HttpMessageHelper
    {
        private const string ApplicationJson = "application/json";

        /// <summary>
        /// The If-Match header value for forcing the operation regardless of ETag.
        /// </summary>
        internal const string ETagForce = "\"*\"";

        /// <summary>
        /// Helper method for serializing payload objects.
        /// </summary>
        /// <param name="payload">The payload object to serialize.</param>
        /// <returns>The serialized HttpContent.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="payload"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="payload"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        internal static HttpContent SerializePayload(object payload)
        {
            Argument.AssertNotNull(payload, nameof(payload));

            string str = JsonConvert.SerializeObject(payload);
            return new StringContent(str, Encoding.UTF8, ApplicationJson);
        }

        /// <summary>
        /// Throws the appropriate exception if the received HTTP status code differs from the expected status code.
        /// </summary>
        /// <param name="expectedHttpStatusCode">The HTTP status code that indicates that the operation was a success and no exception should be thrown.</param>
        /// <param name="responseMessage">The HTTP response that contains the actual status code as well as the payload that contains error details if an error occurred.</param>
        internal static async Task ValidateHttpResponseStatusAsync(HttpStatusCode expectedHttpStatusCode, HttpResponseMessage responseMessage)
        {
            if (expectedHttpStatusCode != responseMessage.StatusCode)
            {
                IReadOnlyDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> defaultErrorMapping =
                    ExceptionHandlingHelper.GetDefaultErrorMapping();
                if (defaultErrorMapping.TryGetValue(responseMessage.StatusCode, out Func<HttpResponseMessage, Task<Exception>> mappedException))
                {
                    throw await mappedException.Invoke(responseMessage);
                }

                // Default case for when the mapping of this error code to an exception does not exist yet
                ErrorCode errorCode = await ExceptionHandlingHelper.GetExceptionCodeAsync(responseMessage);
                string errorMessage = await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage);
                throw new IotHubException(errorCode, errorMessage);
            }
        }

        /// <summary>
        /// Deserializes the payload of the HTTP response into the provided object type.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize the response payload into.</typeparam>
        /// <param name="response">The HTTP response containing the payload to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        internal static async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage response)
        {
            string str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(str);
        }

        /// <summary>
        /// Adds the appropriate If-Match header value to the provided HTTP request.
        /// </summary>
        /// <param name="requestMessage">The request to add the If-Match header to.</param>
        /// <param name="eTag">The If-Match header value to sanitize before adding.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="requestMessage"/> or <paramref name="requestMessage"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="eTag"/> is empty or white space.</exception>
        internal static void ConditionallyInsertETag(HttpRequestMessage requestMessage, string eTag)
        {
            ConditionallyInsertETag(requestMessage, new ETag(eTag), false);
        }

        /// <summary>
        /// Adds the appropriate If-Match header value to the provided HTTP request.
        /// </summary>
        /// <param name="requestMessage">The request to add the If-Match header to.</param>
        /// <param name="eTag">The If-Match header value to sanitize before adding.</param>
        /// <param name="onlyIfUnchanged">
        /// If true, the inserted IfMatch header value will be "*". If false, the IfMatch header value will be equal to the provided eTag.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="requestMessage"/> or <paramref name="requestMessage"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="eTag"/> is empty or white space.</exception>
        internal static void ConditionallyInsertETag(HttpRequestMessage requestMessage, ETag eTag, bool onlyIfUnchanged = false)
        {
            Argument.AssertNotNullOrWhiteSpace(eTag, nameof(eTag));
            Argument.AssertNotNull(requestMessage, nameof(requestMessage));

            if (!onlyIfUnchanged)
            {
                requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue(ETagForce));
            }
            else
            {
                StringBuilder escapedETagBuilder = new StringBuilder();
                if (!eTag.ToString().StartsWith("\"", StringComparison.OrdinalIgnoreCase))
                {
                    escapedETagBuilder.Append('"');
                }

                escapedETagBuilder.Append(eTag.ToString());

                if (!eTag.ToString().EndsWith("\"", StringComparison.OrdinalIgnoreCase))
                {
                    escapedETagBuilder.Append('"');
                }

                requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue(escapedETagBuilder.ToString()));
            }
        }
    }
}
