// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Http2
{
    internal class HttpMessageHelper2
    {
        private const string ApplicationJson = "application/json";

        public const string ETagForce = "*";

        /// <summary>
        /// Helper method for serializing payload objects.
        /// </summary>
        /// <param name="payload">The payload object to serialize.</param>
        /// <returns>The serialized HttpContent.</returns>
        internal static HttpContent GetPayload(object payload)
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

        public static void InsertEtag(HttpRequestMessage requestMessage, string eTag)
        {
            if (string.IsNullOrWhiteSpace(eTag))
            {
                throw new ArgumentException("The entity does not have its ETag set.");
            }

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
