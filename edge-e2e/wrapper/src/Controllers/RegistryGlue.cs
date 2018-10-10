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
using System.Diagnostics;

namespace IO.Swagger.Controllers
{
    /// <summary>
    /// Object which glues the swagger generated wrappers to the various IoTHub SDKs
    /// </summary>
    internal class RegistryGlue
    {
        private static Dictionary<string, object> objectMap = new Dictionary<string, object>();
        private static int objectCount = 0;
        private const string registryPrefix = "registry_";

        public RegistryGlue()
        {
        }

        public async Task<ConnectResponse> ConnectAsync(string connectionString)
        {
            Debug.WriteLine("RegistryConnectAsync called");
            var client = RegistryManager.CreateFromConnectionString(connectionString);
            Debug.WriteLine("Connecting registry manager object");
            await client.OpenAsync().ConfigureAwait(false);
            var connectionId = registryPrefix + Convert.ToString(++objectCount);
            Debug.WriteLine("Registry manager connection complete.  ConnectionId = " + connectionId);
            objectMap[connectionId] = client;
            return new ConnectResponse
            {
                ConnectionId = connectionId
            };
        }

        public async Task DisconnectAsync(string connectionId)
        {
            Debug.WriteLine("RegistryDisconnectAsync called for " + connectionId);
            if (objectMap.ContainsKey(connectionId))
            {
                var client = objectMap[connectionId] as RegistryManager;
                objectMap.Remove(connectionId);
                Debug.WriteLine("Calling CloseAsync on the RegistryManager object");
                await client.CloseAsync().ConfigureAwait(false);
                Debug.WriteLine("RegistryManager.CloseAsync complete");
            }
            else
            {
                Debug.WriteLine("registry object is already closed.");
            }
        }

        public async Task<object> GetModuleTwin(string connectionId, string deviceId, string moduleId)
        {
            Debug.WriteLine("RegistryModuleTwinGet received for {0} with deviceId {1} and moduleId {2}", connectionId, deviceId, moduleId);
            var client = objectMap[connectionId] as RegistryManager;
            Debug.WriteLine("Getting twin");
            var twin = await client.GetTwinAsync(deviceId, moduleId).ConfigureAwait(false);
            Debug.WriteLine("Twin received.");
            Debug.WriteLine(JsonConvert.SerializeObject(twin));
            return twin;
        }

        public async Task PatchModuleTwin(string connectionId, string deviceId, string moduleId, object props)
        {
            Debug.WriteLine("RegistryTwinPatchPutAsync received for {0} with deviceId {1} and moduleId {2}", connectionId, deviceId, moduleId);
            Debug.WriteLine(JsonConvert.SerializeObject(props));
            var client = objectMap[connectionId] as RegistryManager;
            Debug.WriteLine("Patching twin");
            await client.UpdateTwinAsync(deviceId, moduleId, JsonConvert.SerializeObject(props), "*").ConfigureAwait(false);
            Debug.WriteLine("patch complete");
        }

        public async Task CleanupResourcesAsync()
        {
            if (objectMap.Count > 0)
            {
                string[] keys = new string[objectMap.Count];
                objectMap.Keys.CopyTo(keys, 0);
                foreach (var key in keys)
                {
                    await DisconnectAsync(key).ConfigureAwait(false);
                }
            }
        }


    }
}
