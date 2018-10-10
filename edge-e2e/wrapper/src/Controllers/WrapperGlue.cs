// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Swashbuckle.AspNetCore.SwaggerGen;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using IO.Swagger.Attributes;
using IO.Swagger.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices;
using System.Text;

namespace IO.Swagger.Controllers
{
    /// <summary>
    /// Object which glues the swagger generated wrappers to the various IoTHub SDKs
    /// </summary>
    internal class WrapperGlue
    {
        public WrapperGlue()
        {
        }


        public async Task CleanupResourcesAsync()
        {
            await ModuleApiController.module_glue.CleanupResourcesAsync().ConfigureAwait(false);
            await RegistryApiController.registry_glue.CleanupResourcesAsync().ConfigureAwait(false);
            await ServiceApiController.service_glue.CleanupResourcesAsync().ConfigureAwait(false);
        }

        public async Task PrintMessageAsync(String message)
        {
            Console.WriteLine(message);
        }
    }
}