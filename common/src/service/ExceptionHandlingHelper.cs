﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Azure.Devices.Common.Exceptions;

    class ExceptionHandlingHelper
    {
        public static IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> GetDefaultErrorMapping()
        {
            var mappings = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>();

            mappings.Add(HttpStatusCode.NoContent, async (response) => new DeviceNotFoundException(message: await GetExceptionMessageAsync(response).ConfigureAwait(false), innerException: null));
            mappings.Add(HttpStatusCode.NotFound, async (response) => new DeviceNotFoundException(message: await GetExceptionMessageAsync(response).ConfigureAwait(false), innerException: null));
            mappings.Add(HttpStatusCode.Conflict, async (response) => new DeviceAlreadyExistsException(message: await GetExceptionMessageAsync(response).ConfigureAwait(false), innerException: null));
            mappings.Add(HttpStatusCode.BadRequest, async (response) => new ArgumentException(message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));
            mappings.Add(HttpStatusCode.Unauthorized, async (response) => new UnauthorizedException(message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));
            mappings.Add(HttpStatusCode.Forbidden, async (response) => new QuotaExceededException(message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));
            mappings.Add(HttpStatusCode.PreconditionFailed, async (response) => new DeviceMessageLockLostException(message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));
            mappings.Add(HttpStatusCode.RequestEntityTooLarge, async (response) => new MessageTooLargeException(message: await GetExceptionMessageAsync(response).ConfigureAwait(false))); ;
            mappings.Add(HttpStatusCode.InternalServerError, async (response) => new ServerErrorException(message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));
            mappings.Add(HttpStatusCode.ServiceUnavailable, async (response) => new ServerBusyException(message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));
            mappings.Add((HttpStatusCode)429, async (response) => new ThrottlingException(message: await GetExceptionMessageAsync(response).ConfigureAwait(false)));

            return mappings;
        }

        public static Task<string> GetExceptionMessageAsync(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync();
        }
    }
}
