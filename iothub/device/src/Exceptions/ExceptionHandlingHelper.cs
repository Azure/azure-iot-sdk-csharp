// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class ExceptionHandlingHelper
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
                    async (response) =>
                        new IotHubClientException(
                            await GetExceptionMessageAsync(response).ConfigureAwait(false),
                            IotHubClientErrorCode.BadRequest)
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

        private static string CreateMessageWhenDeviceNotFound(string deviceId)
        {
            return "Device {0} not registered".FormatInvariant(deviceId);
        }
    }
}
