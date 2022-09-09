﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    internal class ExceptionHandlingHelper
    {
        public static IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> GetDefaultErrorMapping()
        {
            var mappings = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                { HttpStatusCode.NotFound, async (response) => new IotHubClientException(CreateMessageWhenDeviceNotFound(await GetExceptionMessageAsync(response).ConfigureAwait(false)), innerException: null, isTransient: false, trackingId: null, IotHubStatusCode.DeviceNotFound) },
                { HttpStatusCode.BadRequest, async (response) => new ArgumentException(await GetExceptionMessageAsync(response).ConfigureAwait(false)) },
                { HttpStatusCode.Unauthorized, async (response) => new IotHubClientException(await GetExceptionMessageAsync(response).ConfigureAwait(false), innerException: null, isTransient: false, IotHubStatusCode.Unauthorized) },
                { HttpStatusCode.Forbidden, async (response) => new IotHubClientException(await GetExceptionMessageAsync(response).ConfigureAwait(false), innerException: null, isTransient: true, IotHubStatusCode.QuotaExceeded) },
                { HttpStatusCode.PreconditionFailed, async (response) => new IotHubClientException(await GetExceptionMessageAsync(response).ConfigureAwait(false), isTransient: false, IotHubStatusCode.DeviceMessageLockLost) },
                { HttpStatusCode.RequestEntityTooLarge, async (response) => new IotHubClientException(await GetExceptionMessageAsync(response).ConfigureAwait(false), isTransient: false, IotHubStatusCode.MessageTooLarge) },
                { HttpStatusCode.InternalServerError, async (response) => new IotHubClientException(await GetExceptionMessageAsync(response).ConfigureAwait(false), isTransient: true, IotHubStatusCode.ServerError) },
                { HttpStatusCode.ServiceUnavailable, async (response) => new IotHubClientException(await GetExceptionMessageAsync(response).ConfigureAwait(false), innerException: null, isTransient: true, IotHubStatusCode.ServerBusy) },
                { (HttpStatusCode)429, async (response) => new IotHubClientException(await GetExceptionMessageAsync(response).ConfigureAwait(false), innerException: null, isTransient: true, IotHubStatusCode.Throttled) }
            };
            return mappings;
        }

        public static Task<string> GetExceptionMessageAsync(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync();
        }

        private static string CreateMessageWhenDeviceNotFound(string deviceId)
        {
            return "Device {0} not registered".FormatInvariant(deviceId);
        }
    }
}
