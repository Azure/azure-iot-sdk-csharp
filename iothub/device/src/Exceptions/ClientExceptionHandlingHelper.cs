// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class ClientExceptionHandlingHelper
    {
        internal static IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> GetDefaultErrorMapping()
        {
            var mappings = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.NoContent,
                    async (response) =>
                        new IotHubClientException(
                            CreateMessageWhenDeviceNotFound(await GetExceptionMessageAsync(response).ConfigureAwait(false)),
                            IotHubClientErrorCode.DeviceNotFound)
                },
                {
                    HttpStatusCode.NotFound,
                    async (response) =>
                        new IotHubClientException(
                            CreateMessageWhenDeviceNotFound(await GetExceptionMessageAsync(response).ConfigureAwait(false)),
                            IotHubClientErrorCode.DeviceNotFound)
                },
                {
                    HttpStatusCode.BadRequest,
                    async (response) => await GenerateIotHubClientExceptionAsync(response).ConfigureAwait(false)
                },
                {
                    HttpStatusCode.Unauthorized,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            IotHubClientErrorCode.Unauthorized)
                },
                {
                    HttpStatusCode.Forbidden,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            IotHubClientErrorCode.QuotaExceeded)
                },
                {
                    HttpStatusCode.PreconditionFailed,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            IotHubClientErrorCode.DeviceMessageLockLost)
                },
                {
                    HttpStatusCode.RequestEntityTooLarge,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            IotHubClientErrorCode.MessageTooLarge)
                },
                {
                    HttpStatusCode.InternalServerError,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            IotHubClientErrorCode.ServerError)
                },
                {
                    HttpStatusCode.ServiceUnavailable,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            IotHubClientErrorCode.ServerBusy)
                },
                {
                    (HttpStatusCode)429,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            IotHubClientErrorCode.Throttled)
                }
            };
            return mappings;
        }

        internal static Task<string> GetExceptionMessageAsync(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync();
        }

        internal static async Task<Tuple<string, IotHubClientErrorCode>> GetErrorCodeAndTrackingIdAsync(HttpResponseMessage response)
        {
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            ErrorPayload result = GetErrorCodeAndTrackingId(responseBody);
            return Tuple.Create(result.TrackingId, result.IotHubClientErrorCode);
        }

        internal static ErrorPayload GetErrorCodeAndTrackingId(string responseBody)
        {
            ErrorPayload responseMessage = null;

            try
            {
                // Sometimes we get the full error payload at the root
                responseMessage = JsonConvert.DeserializeObject<ErrorPayload>(responseBody);
                if (responseMessage.ErrorCode == null
                    && responseMessage.TrackingId == null)
                {
                    // And sometimes it is a nested object with a single 'Message' property at the root
                    responseMessage = JsonConvert.DeserializeObject<ErrorPayload>(responseMessage.Message);
                }
            }
            catch (JsonException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(
                        nameof(GetErrorCodeAndTrackingIdAsync),
                        $"Failed to parse response content JSON: {ex.Message}. Message body: '{responseBody}.'");
            }

            if (responseMessage != null)
            {
                if (Enum.TryParse<IotHubClientErrorCode>(responseMessage.ErrorCode?.ToString(), out IotHubClientErrorCode errorCode))
                {
                    responseMessage.IotHubClientErrorCode = errorCode;
                }
                else if (int.TryParse(responseMessage.ErrorCode?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int errorCodeInt))
                {
                    responseMessage.IotHubClientErrorCode = (IotHubClientErrorCode)errorCodeInt;
                }
                else
                {
                    responseMessage.IotHubClientErrorCode = IotHubClientErrorCode.Unknown;
                }

                return responseMessage;
            }

            if (Logging.IsEnabled)
                Logging.Error(
                    nameof(GetErrorCodeAndTrackingIdAsync),
                    $"Failed to derive any error code from the response message: '{responseBody}'");

            return new ErrorPayload
            {
                IotHubClientErrorCode = IotHubClientErrorCode.Unknown,
                Message = responseBody,
                TrackingId = "",
            };
        }

        private static string CreateMessageWhenDeviceNotFound(string deviceId)
        {
            return "Device {0} not registered".FormatInvariant(deviceId);
        }

        private static async Task<IotHubClientException> GenerateIotHubClientExceptionAsync(HttpResponseMessage response)
        {
            string message = await GetExceptionMessageAsync(response).ConfigureAwait(false);
            Tuple<string, IotHubClientErrorCode> pair = await GetErrorCodeAndTrackingIdAsync(response).ConfigureAwait(false);

            return new IotHubClientException(message, pair.Item2)
            {
                TrackingId = pair.Item1,
            };
        }
    }
}
