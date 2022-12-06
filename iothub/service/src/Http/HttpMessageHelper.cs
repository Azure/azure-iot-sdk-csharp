// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Azure;
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
        private static readonly string s_eTagForce = $"\"{ETag.All}\"";

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
        internal static HttpContent SerializePayload(object payload)
        {
            Debug.Assert(payload != null, "Upstream caller should have validated the payload.");

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
                Tuple<string, IotHubServiceErrorCode> pair = await ExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(responseMessage);
                string trackingId = pair.Item1;
                IotHubServiceErrorCode errorCode = pair.Item2;

                throw new IotHubServiceException(errorMessage, responseMessage.StatusCode, errorCode, trackingId);
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
            Debug.Assert(requestMessage != null, "Request message should not have been null");

            if (onlyIfUnchanged && !string.IsNullOrWhiteSpace(eTag.ToString()))
            {
                // "Perform this operation only if the entity is unchanged"
                // Sends the If-Match header with a value of the ETag.
                string escapedETag = EscapeETag(eTag.ToString());
                requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue(escapedETag, true));
            }
            else
            {
                // "Perform this operation even if the entity has changed"
                // Sends the If-Match header with a value of "*"
                requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue(s_eTagForce, true));
            }
        }

        // ETag values other than "*" need to be wrapped in escaped quotes if they are not
        // already.
        private static string EscapeETag(string eTag)
        {
            var escapedETagBuilder = new StringBuilder();

            if (!eTag.StartsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                escapedETagBuilder.Append('"');
            }

            escapedETagBuilder.Append(eTag);

            if (!eTag.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                escapedETagBuilder.Append('"');
            }

            return escapedETagBuilder.ToString();
        }
    }
}
