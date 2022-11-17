// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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
        internal static async Task<Tuple<string, IotHubServiceErrorCode>> GetErrorCodeAndTrackingIdAsync(HttpResponseMessage response)
        {
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            ResponseMessage responseMessage = null;

            try
            {
                IotHubExceptionResult result = JsonConvert.DeserializeObject<IotHubExceptionResult>(responseBody);
                responseMessage = result.Message;
            }
            catch (JsonException ex) when (ex is JsonSerializationException or JsonReaderException)
            {
                if (Logging.IsEnabled)
                    Logging.Error(
                        nameof(GetErrorCodeAndTrackingIdAsync),
                        $"Failed to parse response content JSON: {ex.Message}. Message body: '{responseBody}.'");
            }

            if (responseMessage == null)
            {
                try
                {
                    // sometimes the message is escaped JSON :(
                    ResponseMessageWrapper wrapped = JsonConvert.DeserializeObject<ResponseMessageWrapper>(responseBody);
                    responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(wrapped.Message);
                }
                catch (JsonException ex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(
                            nameof(GetErrorCodeAndTrackingIdAsync),
                            $"Failed to parse response content JSON: {ex.Message}. Message body: '{responseBody}.'");
                }
            }

            if (responseMessage != null)
            {
                string trackingId = string.Empty;
                if (responseMessage.TrackingId != null)
                {
                    trackingId = responseMessage.TrackingId;
                }

                if (responseMessage.ErrorCode != null)
                {
                    if (int.TryParse(responseMessage.ErrorCode, NumberStyles.Any, CultureInfo.InvariantCulture, out int errorCodeInt))
                    {
                        return Tuple.Create(trackingId, (IotHubServiceErrorCode)errorCodeInt);
                    }
                }
            }

            try
            {
                ResponseMessage2 rs2 = JsonConvert.DeserializeObject<ResponseMessage2>(responseBody);
                if (rs2.TryParse())
                {
                    return Tuple.Create(rs2.TrackingId, rs2.ErrorCode);
                }
            }
            catch (JsonReaderException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(
                        nameof(GetErrorCodeAndTrackingIdAsync),
                        $"Failed to deserialize error message into ResponseMessage2: '{ex.Message}'. Message body: '{responseBody}'.");
            }

            IotHubExceptionResult2 exResult = null;
            try
            {
                exResult = JsonConvert.DeserializeObject<IotHubExceptionResult2>(responseBody);
            }
            catch (JsonReaderException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(
                        nameof(GetErrorCodeAndTrackingIdAsync),
                        $"Failed to parse response content JSON: {ex.Message}. Message body: '{responseBody}.'");
            }

            if (exResult != null)
            {
                // In some scenarios, the error response string is a semicolon delimited string with the service-returned error code
                // embedded in a string response.
                const char errorFieldsDelimiter = ';';
                string[] messageFields = exResult.Message?.Split(errorFieldsDelimiter);

                if (messageFields == null || messageFields.Length < 2)
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
                                return Tuple.Create(string.Empty, (IotHubServiceErrorCode)code);
                            }

                            // Otherwise the error code might be a string (e.g., PreconditionFailed) in which case we'll try to
                            // find the matching IotHubServiceErrorCode enum with that same name.
                            if (Enum.TryParse(returnedErrorCode, out IotHubServiceErrorCode errorCode))
                            {
                                return Tuple.Create(string.Empty, errorCode);
                            }
                        }
                    }
                }
            }

            if (Logging.IsEnabled)
                Logging.Error(
                    nameof(GetErrorCodeAndTrackingIdAsync),
                    $"Failed to derive any error code from the response message: {responseBody}");

            return Tuple.Create(string.Empty, IotHubServiceErrorCode.Unknown);
        }
    }
}
