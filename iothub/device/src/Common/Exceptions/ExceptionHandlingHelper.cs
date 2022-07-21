// Copyright (c) Microsoft. All rights reserved.
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
                { HttpStatusCode.NotFound, async (response) => new DeviceNotFoundException(await GetExceptionMessageAsync(response).ConfigureAwait(false)) },
                { HttpStatusCode.BadRequest, async (response) => new BadFormatException(await GetExceptionMessageAsync(response).ConfigureAwait(false)) },
                { HttpStatusCode.Unauthorized, async (response) => new UnauthorizedException(await GetExceptionMessageAsync(response).ConfigureAwait(false)) },
                { HttpStatusCode.Forbidden, async (response) => new QuotaExceededException(await GetExceptionMessageAsync(response).ConfigureAwait(false)) },
                { HttpStatusCode.PreconditionFailed, async (response) => new DeviceMessageLockLostException(await GetExceptionMessageAsync(response).ConfigureAwait(false)) },
                { HttpStatusCode.RequestEntityTooLarge, async (response) => new MessageTooLargeException(await GetExceptionMessageAsync(response).ConfigureAwait(false)) },
                { HttpStatusCode.InternalServerError, async (response) => new ServerErrorException(await GetExceptionMessageAsync(response).ConfigureAwait(false)) },
                { HttpStatusCode.ServiceUnavailable, async (response) => new ServerBusyException(await GetExceptionMessageAsync(response).ConfigureAwait(false)) },
                { (HttpStatusCode)429, async (response) => new IotHubThrottledException(await GetExceptionMessageAsync(response).ConfigureAwait(false), null) }
            };
            return mappings;
        }

        private static Dictionary<int, IotHubException> mappings = new Dictionary<int, IotHubException>
            {
                { 404, new DeviceNotFoundException() },
                { 400, new BadFormatException() },
                { 401, new UnauthorizedException() },
                { 403, new QuotaExceededException() },
                { 412, new DeviceMessageLockLostException() },
                { 413, new MessageTooLargeException() },
                { 500, new ServerErrorException() },
                { 503, new ServerBusyException() },
                { 429, new IotHubThrottledException() }
            };

        public static IotHubException GetExceptionFromStatusCode(int status, string message = "")
        {
            switch (status)
            {
                case 400:
                    return new BadFormatException(message);

                case 401:
                    return new UnauthorizedException(message);

                case 403:
                    return new QuotaExceededException(message);

                case 404:
                    return new DeviceNotFoundException(message);

                case 412:
                    return new DeviceMessageLockLostException(message);

                case 413:
                    return new MessageTooLargeException(message);

                case 429:
                    return new IotHubThrottledException(message);

                case 500:
                    return new ServerErrorException(message);

                case 503:
                    return new ServerBusyException(message);

                default:
                    return new IotHubException(message);
            }
        }

        public static Task<string> GetExceptionMessageAsync(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync();
        }
    }
}
