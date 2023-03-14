// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A helper class for constructing HTTP requests and parsing HTTP responses.
    /// </summary>
    internal sealed class HttpMessageHelper
    {
        private const string ApplicationJson = "application/json";

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
                string errorMessage = await ServiceExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false);
                Tuple<string, IotHubServiceErrorCode> pair = await ServiceExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(responseMessage).ConfigureAwait(false);
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
        /// If true, the IfMatch header value will be equal to the provided eTag. If false, the inserted IfMatch header value will be "*".
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="requestMessage"/> or <paramref name="requestMessage"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="eTag"/> is empty or white space.</exception>
        internal static void ConditionallyInsertETag(HttpRequestMessage requestMessage, ETag eTag, bool onlyIfUnchanged)
        {
            Debug.Assert(requestMessage != null, "Request message should not have been null");

            if (!onlyIfUnchanged)
            {
                eTag = ETag.All;
            }

            if (!string.IsNullOrWhiteSpace(eTag.ToString()))
            {
                // Azure.Core.ETag expects the format "H" for serializing ETags that go into the header.
                // https://github.com/Azure/azure-sdk-for-net/blob/9c6238e0f0dd403d6583b56ec7902c77c64a2e37/sdk/core/Azure.Core/src/ETag.cs#L87-L114
                // Also, System.Net.Http.Headers does not allow ETag.All (*) as a valid value even though RFC allows it.
                // For this reason, we'll add the ETag value without additional validation.
                // System.Net.Http.Headers validation: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Http/tests/UnitTests/Headers/EntityTagHeaderValueTest.cs#L214,
                // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Http/src/System/Net/Http/Headers/GenericHeaderParser.cs#L98
                // RFC specification: https://www.rfc-editor.org/rfc/rfc7232#section-3.1
                _ = requestMessage.Headers.TryAddWithoutValidation("If-Match", eTag.ToString("H"));
            }
        }
    }
}
