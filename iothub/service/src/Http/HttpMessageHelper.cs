// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled.
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
                string errorMessage = await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false);
                IotHubErrorCode errorCode = ExceptionHandlingHelper.GetIotHubErrorCode(errorMessage);
                throw new IotHubServiceException(responseMessage.StatusCode, errorCode, errorMessage);
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
        /// <param name="onlyIfUnchanged">
        /// If true, the inserted IfMatch header value will be "*". If false, the IfMatch header value will be equal to the provided eTag.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="requestMessage"/> or <paramref name="requestMessage"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="eTag"/> is empty or white space.</exception>
        internal static void ConditionallyInsertETag(HttpRequestMessage requestMessage, ETag eTag, bool onlyIfUnchanged)
        {
            Argument.AssertNotNull(requestMessage, nameof(requestMessage));

            if (onlyIfUnchanged && eTag != null)
            {
                // "Perform this operation only if the entity is unchanged"
                // Sends the If-Match header with a value of the ETag.
                string escapedETag = EscapeETag(eTag.ToString());
                requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue(escapedETag));
            }
            else
            {
                // "Perform this operation even if the entity has changed"
                // Sends the If-Match header with a value of "*"
                requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue(ETagForce));
            }
        }

        // ETag values other than "*" need to be wrapped in escaped quotes if they are not
        // already.
        private static string EscapeETag(string eTag)
        {
            var escapedETagBuilder = new StringBuilder();

            if (!eTag.ToString().StartsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                escapedETagBuilder.Append('"');
            }

            escapedETagBuilder.Append(eTag.ToString());

            if (!eTag.ToString().EndsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                escapedETagBuilder.Append('"');
            }

            return escapedETagBuilder.ToString();
        }
    }
}
