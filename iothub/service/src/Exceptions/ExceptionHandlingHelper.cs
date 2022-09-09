// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
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

        public static Task<string> GetExceptionMessageAsync(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Get the fully-qualified error iotHubStatusCode from the HTTP response message, if exists.
        /// </summary>
        /// <param name="response">The HTTP response message</param>
        /// <returns>The fully-qualified error iotHubStatusCode, or the response status iotHubStatusCode, if no error iotHubStatusCode was provided.</returns>
        public static async Task<IotHubStatusCode> GetExceptionCodeAsync(HttpResponseMessage response)
        {
            // First we will attempt to retrieve the error iotHubStatusCode from the response content.
            string responseContentStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // There are two things to consider when surfacing service errors to the user, the 6-digit error iotHubStatusCode and the iotHubStatusCode description. Ideally, when a backend service
            // returns an error, both of these fields are set in the same place. However, IoT hub is returning the 6-digit iotHubStatusCode in the response content, while
            // the error description in the response header. Therefore, there is a chance that the 6-digit error iotHubStatusCode does not match the error description. For that reason,
            // the SDK will do its best to decide what to surface to the user.
            // The SDK will attempt to retrieve the integer error iotHubStatusCode from the response content and the error description from the response header. Through a 'description'
            // to 'error iotHubStatusCode' enum mapping, the SDK will check if both values are a match. If so, the SDK will populate the exception with the proper Code. In the case where
            // there is a mismatch between the error iotHubStatusCode and the description, the SDK returns IotHubStatusCode.Unknown and log a warning.

            int errorCodeValue = (int)IotHubStatusCode.Unknown;
            try
            {
                IoTHubExceptionResult responseContent = JsonConvert.DeserializeObject<IoTHubExceptionResult>(responseContentStr);

                try
                {
                    Dictionary<string, string> messageFields = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent.Message);

                    if (messageFields != null
                        && messageFields.TryGetValue(MessageFieldErrorCode, out string errorCodeObj))
                    {
                        // The result of TryParse is not being tracked since errorCodeValue has already been initialized to a default value of Unknown.
                        _ = int.TryParse(errorCodeObj, NumberStyles.Any, CultureInfo.InvariantCulture, out errorCodeValue);
                    }
                }
                catch (JsonReaderException ex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(null, $"Failed to deserialize error message into a dictionary: {ex}. Message body: '{responseContentStr}.'");

                    // In some scenarios, the error response string is a ';' delimited string with the service-returned error iotHubStatusCode.
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

                                    // When the returned error iotHubStatusCode is numeric, only taing the first 6 characters as the numeric iotHubStatusCode contains 6 digits.
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

                        return IotHubStatusCode.Unknown;
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(null, $"Failed to parse response content JSON: {ex}. Message body: '{responseContentStr}.'");

                return IotHubStatusCode.Unknown;
            }

            // Now that we retrieved the integer error iotHubStatusCode from the response content, we will retrieve the error description from the header.
            string headerErrorCodeString = response.Headers.GetFirstValueOrNull(HttpErrorCodeName);
            if (headerErrorCodeString != null
                && Enum.TryParse(headerErrorCodeString, out IotHubStatusCode headerErrorCode))
            {
                if ((int)headerErrorCode == errorCodeValue)
                {
                    // We have a match. Therefore, return the proper error iotHubStatusCode.
                    return headerErrorCode;
                }

                if (Logging.IsEnabled)
                    Logging.Error(null, $"There is a mismatch between the error iotHubStatusCode retrieved from the response content and the response header." +
                        $"Content error iotHubStatusCode: {errorCodeValue}. Header error iotHubStatusCode description: {(int)headerErrorCode}.");
            }

            return IotHubStatusCode.Unknown;
        }
    }
}
