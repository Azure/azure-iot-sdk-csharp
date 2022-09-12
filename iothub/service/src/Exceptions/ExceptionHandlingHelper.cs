// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    internal class ExceptionHandlingHelper
    {
        private const string MessageFieldErrorCode = "errorCode";
        private const string HttpErrorCodeName = "iothub-errorcode";

        internal static Task<string> GetExceptionMessageAsync(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync();
        }

        // There are two things to consider when surfacing service errors to the user, the 6-digit error code and
        // the error description. Ideally, when a backend service returns an error, both of these fields are set
        // in the same place. However, IoT hub is returning the 6-digit code in the response content, while
        // the error description in the response header. Therefore, there is a chance that the 6-digit error code
        // does not match the error description. For that reason, the SDK will do its best to decide what to surface to the user.
        // The SDK will attempt to retrieve the integer error code from the response content and the error description
        // from the response header. Through a 'description' to 'error code' enum mapping, the SDK will check if
        // both values are a match. If so, the SDK will populate the exception with the proper Code. In the case where
        // there is a mismatch between the error code and the description, the SDK returns
        // IotHubStatusCode.Unknown and log a warning.
        internal static async Task<IotHubErrorCode> GetIotHubErrorCodeAsync(HttpResponseMessage response)
        {
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            IoTHubExceptionResult responseContent = null;
            try
            {
                responseContent = JsonConvert.DeserializeObject<IoTHubExceptionResult>(responseBody);
            }
            catch (JsonReaderException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Info(
                        nameof(GetIotHubErrorCodeAsync),
                        $"Failed to parse response content JSON: {ex.Message}. Message body: '{responseBody}.'");
            }

            if (responseContent != null)
            {
                try
                {
                    var structuredMessageFields = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent.Message);

                    if (structuredMessageFields != null
                        && structuredMessageFields.TryGetValue(MessageFieldErrorCode, out string errorCodeObj))
                    {
                        if (int.TryParse(errorCodeObj, NumberStyles.Any, CultureInfo.InvariantCulture, out int errorCodeInt))
                        {
                            return CompareToResponseHeader(response, (IotHubErrorCode)errorCodeInt);
                        }
                    }
                }
                catch (JsonReaderException ex)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(
                            nameof(GetIotHubErrorCodeAsync),
                            $"Failed to deserialize error message into a dictionary: {ex.Message}. Message body: '{responseBody}.'");
                }
            }

            // In some scenarios, the error response string is a semicolon delimited string with the service-returned error code
            // embedded in a string response.
            const char errorFieldsDelimiter = ';';
            string[] messageFields = responseContent.Message?.Split(errorFieldsDelimiter);

            if (messageFields == null && Logging.IsEnabled)
            {
                Logging.Error(
                    nameof(GetIotHubErrorCodeAsync),
                    $"Failed to find expected semicolon in error message to find error code." +
                    $" Message body: '{responseBody}.'");
            }
            else
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

                            // When the returned error code is numeric, only take the first 6 characters as it contains 6 digits.
                            if (int.TryParse(returnedErrorCode.Substring(0, 6), out int code))
                            {
                                return CompareToResponseHeader(response, (IotHubErrorCode)code);
                            }

                            // Otherwise the error code might be a string (e.g., PreconditionFailed) in which case we'll try to
                            // find the matching IotHubErrorCode enum with that same name.
                            if (Enum.TryParse(returnedErrorCode, out IotHubErrorCode errorCode))
                            {
                                return CompareToResponseHeader(response, errorCode);
                            }
                        }
                    }
                }
            }

            if (Logging.IsEnabled)
                Logging.Error(
                    nameof(GetIotHubErrorCodeAsync),
                    $"Failed to derive any error code from the response message: {responseBody}");

            return IotHubErrorCode.Unknown;
        }

        internal static IotHubErrorCode CompareToResponseHeader(HttpResponseMessage response, IotHubErrorCode contentErrorCode)
        {
            string headerErrorCodeString = response.Headers.GetFirstValueOrNull(HttpErrorCodeName);

            if (headerErrorCodeString != null
                && Enum.TryParse(headerErrorCodeString, out IotHubErrorCode headerErrorCode))
            {
                if ((int)headerErrorCode == (int)contentErrorCode)
                {
                    // We have a match. Therefore, return the proper error code.
                    return headerErrorCode;
                }

                if (Logging.IsEnabled)
                    Logging.Error(null, $"There is a mismatch between the error code retrieved from the response content and the response header." +
                        $"Content error code: {(int)contentErrorCode}. Header error code description: {(int)headerErrorCode}.");
            }

            return IotHubErrorCode.Unknown;
        }
    }
}
