// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        // the error description in the response header. The SDK will attempt to retrieve the integer error code
        // in the field of ErrorCode from the response content. If it works, the SDK will populate the exception
        // with the proper Code. Otherwise the SDK returns IotHubStatusCode.Unknown and log an error.
        internal static async Task<KeyValuePair<string, IotHubErrorCode>> GetErrorCodeAndTrackingIdAsync(HttpResponseMessage response)
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
                    Logging.Error(
                        nameof(GetErrorCodeAndTrackingIdAsync),
                        $"Failed to parse response content JSON: {ex.Message}. Message body: '{responseBody}.'");
            }

            if (responseContent != null)
            {
                string trackingId = string.Empty;
                try
                {
                    var structuredMessageFields = JsonConvert.DeserializeObject<ResponseMessage>(responseContent.Message);

                    if (structuredMessageFields != null)
                    {
                        if (structuredMessageFields.TrackingId != null)
                        {
                            trackingId = structuredMessageFields.TrackingId;
                        }

                        if (structuredMessageFields.ErrorCode != null)
                        {
                            if (int.TryParse(structuredMessageFields.ErrorCode, NumberStyles.Any, CultureInfo.InvariantCulture, out int errorCodeInt))
                            {
                                return new KeyValuePair<string, IotHubErrorCode>(trackingId, (IotHubErrorCode)errorCodeInt);
                            }
                        }
                    }
                }
                catch (JsonReaderException ex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(
                            nameof(GetErrorCodeAndTrackingIdAsync),
                            $"Failed to deserialize error message into a dictionary: {ex.Message}. Message body: '{responseBody}.'");
                }
            }

            // In some scenarios, the error response string is a semicolon delimited string with the service-returned error code
            // embedded in a string response.
            const char errorFieldsDelimiter = ';';
            string[] messageFields = responseContent.Message?.Split(errorFieldsDelimiter);

            if (messageFields == null || messageFields.Count() < 2)
            {
                if (Logging.IsEnabled)
                    Logging.Error(
                        nameof(GetErrorCodeAndTrackingIdAsync),
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

                        if (messageField.IndexOf(errorCodeDelimiter) < 0)
                        {
                            continue;
                        }

                        string[] errorCodeFields = messageField.Split(errorCodeDelimiter);

                        string returnedErrorCode = errorCodeFields[1];

                        // When the returned error code is numeric, only take the first 6 characters as it contains 6 digits.
                        if (int.TryParse(returnedErrorCode.Substring(0, 6), out int code))
                        {
                            return new KeyValuePair<string, IotHubErrorCode>(string.Empty, (IotHubErrorCode)code);
                        }

                        // Otherwise the error code might be a string (e.g., PreconditionFailed) in which case we'll try to
                        // find the matching IotHubErrorCode enum with that same name.
                        if (Enum.TryParse(returnedErrorCode, out IotHubErrorCode errorCode))
                        {
                            return new KeyValuePair<string, IotHubErrorCode>(string.Empty, errorCode);
                        }
                    }
                }
            }

            if (Logging.IsEnabled)
                Logging.Error(
                    nameof(GetErrorCodeAndTrackingIdAsync),
                    $"Failed to derive any error code from the response message: {responseBody}");

            return new KeyValuePair<string, IotHubErrorCode>(string.Empty, IotHubErrorCode.Unknown);
        }
    }
}
