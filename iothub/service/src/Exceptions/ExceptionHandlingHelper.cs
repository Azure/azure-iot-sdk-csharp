// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    internal class ExceptionHandlingHelper
    {
        private const string MessageFieldErrorCode = "errorCode";
        private const string HttpErrorCodeName = "iothub-errorcode";

        private static readonly IReadOnlyDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> s_mappings =
            new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
        {
            {
                HttpStatusCode.NoContent,
                async (response) => new IotHubServiceException(
                    code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                    message: await GetExceptionMessageAsync(response).ConfigureAwait(false))
            },
            {
                HttpStatusCode.NotFound,
                async (response) => new IotHubServiceException(
                    code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                    message: await GetExceptionMessageAsync(response).ConfigureAwait(false))
            },
            {
                HttpStatusCode.Conflict,
                async (response) => new IotHubServiceException(
                    code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                    message: await GetExceptionMessageAsync(response).ConfigureAwait(false))
            },
            {
                HttpStatusCode.BadRequest, async (response) => new ArgumentException(
                message: await GetExceptionMessageAsync(response).ConfigureAwait(false))
            },
            {
                HttpStatusCode.Unauthorized,
                async (response) => new IotHubServiceException(
                    code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                    message: await GetExceptionMessageAsync(response).ConfigureAwait(false))
            },
            {
                HttpStatusCode.Forbidden,
                async (response) => new QuotaExceededException(
                    code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                    message: await GetExceptionMessageAsync(response).ConfigureAwait(false))
            },
            {
                HttpStatusCode.PreconditionFailed,
                async (response) => new IotHubServiceException(
                    code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                    message: await GetExceptionMessageAsync(response).ConfigureAwait(false))
            },
            {
                HttpStatusCode.RequestEntityTooLarge,
                async (response) => new MessageTooLargeException(
                    code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                    message: await GetExceptionMessageAsync(response).ConfigureAwait(false))
            },
            {
                HttpStatusCode.InternalServerError,
                async (response) => new IotHubServiceException(
                    code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                    message: await GetExceptionMessageAsync(response).ConfigureAwait(false))
            },
            {
                HttpStatusCode.ServiceUnavailable,
                async (response) => new IotHubServiceException(
                    code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                    message: await GetExceptionMessageAsync(response).ConfigureAwait(false))
            },
            {
                (HttpStatusCode)429,
                async (response) => new ThrottlingException(
                    code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                    message: await GetExceptionMessageAsync(response).ConfigureAwait(false))
            }
        };

        public static IReadOnlyDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> GetDefaultErrorMapping() =>
            s_mappings;

        public static Task<string> GetExceptionMessageAsync(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Get the fully-qualified error code from the HTTP response message, if exists.
        /// </summary>
        /// <param name="response">The HTTP response message</param>
        /// <returns>The fully-qualified error code, or the response status code, if no error code was provided.</returns>
        public static async Task<IotHubStatusCode> GetExceptionCodeAsync(HttpResponseMessage response)
        {
            // First we will attempt to retrieve the error code from the response content.
            string responseContentStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // There are two things to consider when surfacing service errors to the user, the 6-digit error code and the code description. Ideally, when a backend service
            // returns an error, both of these fields are set in the same place. However, IoT hub is returning the 6-digit code in the response content, while
            // the error description in the response header. Therefore, there is a chance that the 6-digit error code does not match the error description. For that reason,
            // the SDK will do its best to decide what to surface to the user.
            // The SDK will attempt to retrieve the integer error code from the response content and the error description from the response header. Through a 'description'
            // to 'error code' enum mapping, the SDK will check if both values are a match. If so, the SDK will populate the exception with the proper Code. In the case where
            // there is a mismatch between the error code and the description, the SDK returns IotHubStatusCode.InvalidErrorCode and log a warning.

            int errorCodeValue = (int)IotHubStatusCode.InvalidErrorCode;
            try
            {
                IoTHubExceptionResult responseContent = JsonConvert.DeserializeObject<IoTHubExceptionResult>(responseContentStr);

                try
                {
                    Dictionary<string, string> messageFields = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent.Message);

                    if (messageFields != null
                        && messageFields.TryGetValue(MessageFieldErrorCode, out string errorCodeObj))
                    {
                        // The result of TryParse is not being tracked since errorCodeValue has already been initialized to a default value of InvalidErrorCode.
                        _ = int.TryParse(errorCodeObj, NumberStyles.Any, CultureInfo.InvariantCulture, out errorCodeValue);
                    }
                }
                catch (JsonReaderException ex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(null, $"Failed to deserialize error message into a dictionary: {ex}. Message body: '{responseContentStr}.'");

                    // In some scenarios, the error response string is a ';' delimited string with the service-returned error code.
                    const char errorFieldsDelimiter = ';';
                    string[] messageFields = responseContent.Message?.Split(errorFieldsDelimiter);

                    if (messageFields != null)
                    {
                        foreach (string messageField in messageFields)
                        {
                            if (messageField.IndexOf(MessageFieldErrorCode, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                const char errorCodeDelimiter = ':';

                                if (messageField.IndexOf(errorCodeDelimiter) >= 0)
                                {
                                    string[] errorCodeFields = messageField.Split(errorCodeDelimiter);

                                    string returnedErrorCode = errorCodeFields[1];

                                    // When the returned error code is numeric, only taing the first 6 characters as the numeric code contains 6 digits.
                                    if (int.TryParse(returnedErrorCode.Substring(0, 6), out int code))
                                    {
                                        returnedErrorCode = code.ToString();
                                    }

                                    if (Enum.TryParse(returnedErrorCode, out IotHubStatusCode errorCode))
                                    {
                                        errorCodeValue = (int)errorCode;
                                    }
                                }
                            }
                            break;
                        }
                    }
                    else
                    {
                        if (Logging.IsEnabled)
                            Logging.Error(null, $"Failed to deserialize error message into a dictionary and could not parse ';' delimited string either: {ex}." +
                                $" Message body: '{responseContentStr}.'");

                        return IotHubStatusCode.InvalidErrorCode;
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(null, $"Failed to parse response content JSON: {ex}. Message body: '{responseContentStr}.'");

                return IotHubStatusCode.InvalidErrorCode;
            }

            // Now that we retrieved the integer error code from the response content, we will retrieve the error description from the header.
            string headerErrorCodeString = response.Headers.GetFirstValueOrNull(HttpErrorCodeName);
            if (headerErrorCodeString != null
                && Enum.TryParse(headerErrorCodeString, out IotHubStatusCode headerErrorCode))
            {
                if ((int)headerErrorCode == errorCodeValue)
                {
                    // We have a match. Therefore, return the proper error code.
                    return headerErrorCode;
                }

                if (Logging.IsEnabled)
                    Logging.Error(null, $"There is a mismatch between the error code retrieved from the response content and the response header." +
                        $"Content error code: {errorCodeValue}. Header error code description: {(int)headerErrorCode}.");
            }

            return IotHubStatusCode.InvalidErrorCode;
        }
    }
}
