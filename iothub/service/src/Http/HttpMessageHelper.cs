// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
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
        internal static HttpContent SerializePayload(object payload)
        {
            if (payload == null)
            {
                return null;
            }

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
        public static void InsertETag(HttpRequestMessage requestMessage, string eTag)
        {
            if (string.IsNullOrWhiteSpace(eTag))
            {
                throw new ArgumentException("The entity does not have its ETag set.");
            }

            // All ETag values need to be wrapped in escaped quotes, but the "forced" value
            // is hardcoded with quotes so it can be skipped here
            if (!ETagForce.Equals(eTag))
            {
                if (!eTag.StartsWith("\"", StringComparison.OrdinalIgnoreCase))
                {
                    eTag = "\"" + eTag;
                }

                if (!eTag.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
                {
                    eTag += "\"";
                }
            }

            requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue(eTag));
        }
    }
}
