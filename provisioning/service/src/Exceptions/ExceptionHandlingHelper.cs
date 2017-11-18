// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class ExceptionHandlingHelper
    {
        public static IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> GetDefaultErrorMapping()
        {
            var mappings = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>();

            mappings.Add(HttpStatusCode.BadRequest, async (response) => new ProvisioningServiceClientBadFormatException(await GetExceptionMessageAsync(response)));
            mappings.Add(HttpStatusCode.Unauthorized, async (response) => new ProvisioningServiceClientUnathorizedException(await GetExceptionMessageAsync(response)));
            mappings.Add(HttpStatusCode.NotFound, async (response) => new ProvisioningServiceClientNotFoundException(await GetExceptionMessageAsync(response)));
            mappings.Add(HttpStatusCode.PreconditionFailed, async (response) => new ProvisioningServiceClientPreconditionFailedException(await GetExceptionMessageAsync(response)));
            //mappings.Add(HttpStatusCode.RequestEntityTooLarge, async (response) => new ProvisioningServiceClientTooManyRequestsException(await GetExceptionMessageAsync(response))); ;
            mappings.Add(HttpStatusCode.InternalServerError, async (response) => new ProvisioningServiceClientInternalServerErrorException(await GetExceptionMessageAsync(response)));

            return mappings;
        }

        public static Task<string> GetExceptionMessageAsync(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync();
        }
    }
}
