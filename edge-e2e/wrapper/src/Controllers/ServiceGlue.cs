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
using Newtonsoft.Json.Linq;
using System.Threading;

namespace IO.Swagger.Controllers
{
    /// <summary>
    /// Object which glues the swagger generated wrappers to the various IoTHub SDKs
    /// </summary>
    internal class ServiceGlue
    {
        private static Dictionary<string, ServiceClient> objectMap = new Dictionary<string, ServiceClient>();
        private static int objectCount = 0;
        private const string serviceClientPrefix = "serviceClient_";

        public ServiceGlue()
        {
        }

        public async Task<ConnectResponse> ConnectAsync(string connectionString)
        {
            var client = ServiceClient.CreateFromConnectionString(connectionString);
            await client.OpenAsync().ConfigureAwait(false);
            var connectionId = serviceClientPrefix + Convert.ToString(++objectCount);
            objectMap[connectionId] = client;
            return new ConnectResponse
            {
                ConnectionId = connectionId
            };
        }

        public async Task DisconnectAsync(string connectionId)
        {
            if (objectMap.ContainsKey(connectionId))
            {
                var client = objectMap[connectionId] as ServiceClient;
                objectMap.Remove(connectionId);
                await client.CloseAsync().ConfigureAwait(false);
            }
        }

        private static TimeSpan _jtokenToTimeSpan(JToken t)
        {
            if (t == null)
            {
                return TimeSpan.Zero;
            }
            else
            {
                return TimeSpan.FromSeconds((double)t);
            }
        }

        private CloudToDeviceMethod _jobjectToMethod(JObject jobj)
        {
            string methodName = (string)jobj["methodName"];
            var responseTimeout = _jtokenToTimeSpan(jobj["responseTimeoutInSeconds"]);
            var connectionTimeout = _jtokenToTimeSpan(jobj["connectionTimeoutInSeconds"]);
            var payload = JsonConvert.SerializeObject(jobj["payload"]);

            var method = new CloudToDeviceMethod(methodName, responseTimeout, connectionTimeout);
            method.SetPayloadJson(payload);
            return method;
        }

        public async Task<object> InvokeModuleMethodAsync(string connectionId, string deviceId, string moduleId, object methodInvokeParameters)
        {
            Debug.WriteLine("InvokeModuleMethodAsync received for {0} with deviceId {1} and moduleId {2}", connectionId, deviceId, moduleId);
            Debug.WriteLine(methodInvokeParameters.ToString());
            var client = objectMap[connectionId];
            var request = _jobjectToMethod(methodInvokeParameters as JObject);
            Debug.WriteLine("Invoking");
            var response = await client.InvokeDeviceMethodAsync(deviceId, moduleId, request, CancellationToken.None).ConfigureAwait(false);
            Debug.WriteLine("Response received:");
            Debug.WriteLine(JsonConvert.SerializeObject(response));
            return new JObject(
                new JProperty("status", response.Status),
                new JProperty("payload", response.GetPayloadAsJson())
            );
        }

        public async Task<object> InvokeDeviceMethodAsync(string connectionId, string deviceId, object methodInvokeParameters)
        {
            Debug.WriteLine("InvokeDeviceMethodAsync received for {0} with deviceId {1} ", connectionId, deviceId);
            Debug.WriteLine(methodInvokeParameters.ToString());
            var client = objectMap[connectionId];
            var request = _jobjectToMethod(methodInvokeParameters as JObject);
            Debug.WriteLine("Invoking");
            var response = await client.InvokeDeviceMethodAsync(deviceId, request, CancellationToken.None).ConfigureAwait(false);
            Debug.WriteLine("Response received:");
            Debug.WriteLine(JsonConvert.SerializeObject(response));
            return new JObject(
                new JProperty("status", response.Status),
                new JProperty("payload", response.GetPayloadAsJson())
            );
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
