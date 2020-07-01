// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Common;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using Microsoft.Azure.Devices.Common.Extensions;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;

    internal class ExceptionHandlingHelper
    {
        public static IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> GetDefaultErrorMapping()
        {
            var mappings = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>();

            mappings.Add(HttpStatusCode.NoContent, async (response) => new DeviceNotFoundException(code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                                                                                                   message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));

            mappings.Add(HttpStatusCode.NotFound, async (response) => new DeviceNotFoundException(code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                                                                                                  message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));

            mappings.Add(HttpStatusCode.Conflict, async (response) => new DeviceAlreadyExistsException(code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                                                                                                       message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));

            mappings.Add(HttpStatusCode.BadRequest, async (response) => new ArgumentException(message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));

            mappings.Add(HttpStatusCode.Unauthorized, async (response) => new UnauthorizedException(code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                                                                                                    message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));

            mappings.Add(HttpStatusCode.Forbidden, async (response) => new QuotaExceededException(code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                                                                                                  message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));

            mappings.Add(HttpStatusCode.PreconditionFailed, async (response) => new DeviceMessageLockLostException(code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                                                                                                                   message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));

            mappings.Add(HttpStatusCode.RequestEntityTooLarge, async (response) => new MessageTooLargeException(code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                                                                                                                message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));

            mappings.Add(HttpStatusCode.InternalServerError, async (response) => new ServerErrorException(code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                                                                                                          message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));

            mappings.Add(HttpStatusCode.ServiceUnavailable, async (response) => new ServerBusyException(code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                                                                                                        message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));

            mappings.Add((HttpStatusCode)429, async (response) => new ThrottlingException(code: await GetExceptionCodeAsync(response).ConfigureAwait(false),
                                                                                          message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));

            return mappings;
        }

        public static Task<string> GetExceptionMessageAsync(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Get the fully qualified error code from the http response message, if exists
        /// </summary>
        /// <param name="response">The http response message</param>
        /// <returns>The fully qualified error code, or the response status code if no error code was provided.</returns>
        public static async Task<ErrorCode> GetExceptionCodeAsync(HttpResponseMessage response)
        {
            // First we will attempt to retrieve the error code from the response content.
            string responseContentStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // There are two things to consider when surfacing service errors to the user, the 6-digit error code and the code description. Ideally, when a backend service
            // returns an error, both of these fields are set in the same place. However, IoT Hub is returning the 6-digit code in the response content, while 
            // the error description in the response header. Therefore, there is a chance that the 6-digit error code does not match the error description. For that reason,
            // the SDK will do its best to decide what to surface to the user.
            // The SDK will attempt to retrieve the integer error code from the response content and the error description from the response header. Through a 'description'
            // to 'error code' enum mapping, the SDK will check if both values are a match. If so, the SDK will populate the exception with the proper Code. In the case where
            // there is a mismatch between the error code and the description, the SDK returns ErrorCode.InvalidErrorCode and log a warning. 

            int errorCode = -1;
            try
            {
                IoTHubExceptionResult responseContent = JsonConvert.DeserializeObject<IoTHubExceptionResult>(responseContentStr);
                Dictionary<string, string> messageFields = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent._message);
                if (messageFields != null && messageFields.TryGetValue(CommonConstants.ErrorCode, out string errorCodeObj))
                {
                    errorCode = Convert.ToInt32(errorCodeObj, CultureInfo.InvariantCulture);
                }
                else
                {
                    return ErrorCode.InvalidErrorCode;
                }
            }
            catch (JsonReaderException ex)
            {
                if (Logging.IsEnabled)
                {
                    Logging.Error(null, $"Failed to parse response content JSON: {ex}. Message body: '{responseContentStr}.'");
                }

                return ErrorCode.InvalidErrorCode;
            }

            // Now that we retrieved the integer error code from the response content, we will retrieve the error description from the header.
            string headerErrorCodeString = response.Headers.GetFirstValueOrNull(CommonConstants.HttpErrorCodeName);
            if (headerErrorCodeString != null &&
                Enum.TryParse(headerErrorCodeString, out ErrorCode headerErrorCode))
            {
                if ((int)headerErrorCode == errorCode)
                {
                    // We have a match. Therefore, return the proper error code.
                    return headerErrorCode;
                }
                else
                {
                    if (Logging.IsEnabled)
                    {
                        Logging.Error(null, $"There is a mismatch between the error code retrieved from the response content and the response header." +
                            $"Content error code: {errorCode}. Header error code description: {(int)headerErrorCode}.");
                    }
                }
            }

            return ErrorCode.InvalidErrorCode;
        }
    }
}
