// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    internal class ExceptionHandlingHelper
    {
        /// <summary>
        /// Mapper to map IoT hub service returned 6 digit error codes to <see cref="IotHubClientException.ErrorCode"/>.
        /// </summary>
        /// <param name="errorCode">The 6 digit error code returned by IoT hub service.</param>
        /// <returns>The IotHubClientErrorCode that is returned as a part of IotHubClientException.</returns>
        internal static IotHubClientErrorCode GetIotHubClientErrorCode(string errorCode)
        {
            return errorCode switch
            {
                "400004" => IotHubClientErrorCode.ArgumentInvalid,
                _ => IotHubClientErrorCode.Unknown,
            };
        }

        internal static IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> GetDefaultErrorMapping()
        {
            var mappings = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                { 
                    HttpStatusCode.NoContent,
                    async (response) =>
                        new IotHubClientException(
                            CreateMessageWhenDeviceNotFound(await GetExceptionMessageAsync(response).ConfigureAwait(false)),
                            errorCode:IotHubClientErrorCode.DeviceNotFound)
                },
                { 
                    HttpStatusCode.NotFound,
                    async (response) =>
                        new IotHubClientException(
                            CreateMessageWhenDeviceNotFound(await GetExceptionMessageAsync(response).ConfigureAwait(false)),
                            errorCode:IotHubClientErrorCode.DeviceNotFound)
                },
                { 
                    HttpStatusCode.BadRequest,
                    async (response) =>
                        new ArgumentException(await GetExceptionMessageAsync(response).ConfigureAwait(false))
                },
                { 
                    HttpStatusCode.Unauthorized,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            errorCode:IotHubClientErrorCode.Unauthorized)
                },
                { 
                    HttpStatusCode.Forbidden,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            errorCode:IotHubClientErrorCode.QuotaExceeded)
                },
                { 
                    HttpStatusCode.PreconditionFailed,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            errorCode:IotHubClientErrorCode.DeviceMessageLockLost)
                },
                { 
                    HttpStatusCode.RequestEntityTooLarge,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            errorCode:IotHubClientErrorCode.MessageTooLarge)
                },
                { 
                    HttpStatusCode.InternalServerError,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            errorCode:IotHubClientErrorCode.ServerError)
                },
                { 
                    HttpStatusCode.ServiceUnavailable,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            errorCode:IotHubClientErrorCode.ServerBusy)
                },
                { 
                    (HttpStatusCode)429,
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            errorCode:IotHubClientErrorCode.Throttled)
                }
            };
            return mappings;
        }

        internal static Task<string> GetExceptionMessageAsync(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync();
        }

        private static string CreateMessageWhenDeviceNotFound(string deviceId)
        {
            return "Device {0} not registered".FormatInvariant(deviceId);
        }
    }
}
