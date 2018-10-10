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
using Microsoft.Azure.Devices.Shared;

namespace IO.Swagger.Controllers
{
    /// <summary>
    /// Object which glues the swagger generated wrappers to the various IoTHub SDKs
    /// </summary>
    internal class ModuleGlue
    {
        private static Dictionary<string, ModuleClient> objectMap = new Dictionary<string, ModuleClient>();
        private static int objectCount = 0;
        private const string modulePrefix = "module_";

        public ModuleGlue()
        {
        }

        private static Microsoft.Azure.Devices.Client.TransportType transportNameToType(string transport)
        {
            switch (transport.ToLower())
            {
                case "mqtt":
                    return Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only;
                case "mqttws":
                    return Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only;
                case "amqp":
                    return Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only;
                case "amqpws":
                    return Microsoft.Azure.Devices.Client.TransportType.Amqp_WebSocket_Only;
                case "http":
                    return Microsoft.Azure.Devices.Client.TransportType.Http1;
                default:
                    throw new ArgumentException("unknown transport " + transport);
            }
        }

        public async Task<ConnectResponse> ConnectAsync(string transport, string connectionString, Certificate caCertificate)
        {
            Debug.WriteLine("ConnectAsync for " + transport);
            var client = ModuleClient.CreateFromConnectionString(connectionString, transportNameToType(transport));
            await client.OpenAsync().ConfigureAwait(false);
            var connectionId = modulePrefix + Convert.ToString(++objectCount);
            Debug.WriteLine("Connected successfully.  Connection Id = " + connectionId);
            objectMap[connectionId] = client;
            return new ConnectResponse
            {
                ConnectionId = connectionId
            };
        }

        public async Task<ConnectResponse> ConnectFromEnvironmentAsync(string transport)
        {
            var client = await ModuleClient.CreateFromEnvironmentAsync(transportNameToType(transport)).ConfigureAwait(false);
            await client.OpenAsync().ConfigureAwait(false);
            var connectionId = modulePrefix + Convert.ToString(++objectCount);
            objectMap[connectionId] = client;
            return new ConnectResponse
            {
                ConnectionId = connectionId
            };
        }

        public async Task DisconnectAsync(string connectionId)
        {
            Debug.WriteLine("DisconnectAsync received for " + connectionId);
            if (objectMap.ContainsKey(connectionId))
            {
                var client = objectMap[connectionId];
                objectMap.Remove(connectionId);
                await client.CloseAsync().ConfigureAwait(false);
                Debug.WriteLine("Disconnected successfully");
            }
            else
            {
                Debug.WriteLine("Client already disconnected.  Nothing to to do.");
            }
        }

        public async Task EnableInputMessagesAsync(string connectionId)
        {
            Debug.WriteLine("EnableInputMessageAsync received for " + connectionId);
        }

        public async Task EnableMethodsAsync(string connectionId)
        {
            Debug.WriteLine("EnableMethodsAsync received for " + connectionId);
        }

        private TwinCollection lastDesiredProps = null;
        private SemaphoreSlim desiredPropMutex = null;

        public async Task EnableTwinAsync(string connectionId)
        {
            Debug.WriteLine("EnableTwinAsync received for " + connectionId);
            var client = objectMap[connectionId];

            DesiredPropertyUpdateCallback handler = async (props, context) =>
            {
                Debug.WriteLine("patch received");
                lastDesiredProps = props;
                if (desiredPropMutex == null)
                {
                    Debug.WriteLine("No mutex to release.  nobody is listening for this patch.");
                }
                else
                {
                    Debug.WriteLine("releasing patch mutex");
                    desiredPropMutex.Release();
                    desiredPropMutex = null;
                }
            };

            Debug.WriteLine("setting patch handler");
            await client.SetDesiredPropertyUpdateCallbackAsync(handler, null).ConfigureAwait(false);
            Debug.WriteLine("Done enabling twin");

        }

        public async Task SendEventAsync(string connectionId, string eventBody)
        {
            Debug.WriteLine("sendEventAsync received for {0} with body {1}", connectionId, eventBody);
            var client = objectMap[connectionId];
            await client.SendEventAsync(new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(eventBody))).ConfigureAwait(false);
            Debug.WriteLine("sendEventAsync complete");
        }

        public async Task SendOutputEventAsync(string connectionId, string outputName, string eventBody)
        {
            Debug.WriteLine("sendEventAsync received for {0} with output {1} and body {2}", connectionId, outputName, eventBody);
            var client = objectMap[connectionId];
            await client.SendEventAsync(outputName, new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(eventBody))).ConfigureAwait(false);
            Debug.WriteLine("sendOutputEventAsync complete");
        }

        public async Task<object> WaitForInputMessageAsync(string connectionId, string inputName)
        {
            Debug.WriteLine("WaitForInputMessageAsync received for {0} with inputName {1}", connectionId, inputName);
            var mutex = new System.Threading.SemaphoreSlim(1);
            await mutex.WaitAsync().ConfigureAwait(false);  // Grab the mutex. The handler will release it later
            Microsoft.Azure.Devices.Client.Message message = null;
            var client = objectMap[connectionId];
            MessageHandler handler = async (msg, context) =>
            {
                Debug.WriteLine("message received");
                message = msg;
                await client.SetInputMessageHandlerAsync(inputName, null, null).ConfigureAwait(false);
                Debug.WriteLine("releasing inputMessage mutex");
                mutex.Release();
                return MessageResponse.Completed;
            };
            Debug.WriteLine("Setting input handler");
            await client.SetInputMessageHandlerAsync(inputName, handler, null).ConfigureAwait(false);

            Debug.WriteLine("Waiting for inputMessage mutex to release");
            await mutex.WaitAsync().ConfigureAwait(false);
            Debug.WriteLine("mutex triggered.");

            byte[] bytes = message.GetBytes();
            string s = Encoding.UTF8.GetString(bytes);
            Debug.WriteLine("message = {0}", s as object);
            object result;
            try
            {
                result = JsonConvert.DeserializeObject(s);
            }
            catch(JsonReaderException)
            {
                result = s;
            }
            return result;
        }

        private static TimeSpan? _jtokenToTimeSpan(JToken t)
        {
            if (t == null)
            {
                return null;
            }
            else
            {
                return TimeSpan.FromSeconds((double)t);
            }
        }

        private MethodRequest _jobjectToMethodRequest(JObject jobj)
        {
            string methodName = (string)jobj["methodName"];
            var responseTimeout = _jtokenToTimeSpan(jobj["responseTimeoutInSeconds"]);
            var connectionTimeout = _jtokenToTimeSpan(jobj["connectionTimeoutInSeconds"]);
            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jobj["payload"]));

            return new MethodRequest(methodName, payload, responseTimeout, connectionTimeout);
        }

        public async Task<object> InvokeModuleMethodAsync(string connectionId, string deviceId, string moduleId, object methodInvokeParameters)
        {
            Debug.WriteLine("InvokeModuleMethodAsync received for {0} with deviceId {1} and moduleId {2}", connectionId, deviceId, moduleId);
            Debug.WriteLine(methodInvokeParameters.ToString());
            var client = objectMap[connectionId];
            var request = _jobjectToMethodRequest(methodInvokeParameters as JObject);
            Debug.WriteLine("Invoking");
            var response = await client.InvokeMethodAsync(deviceId, moduleId, request, CancellationToken.None).ConfigureAwait(false);
            Debug.WriteLine("Response received:");
            Debug.WriteLine(JsonConvert.SerializeObject(response));
            return new JObject(
                new JProperty("status", response.Status),
                new JProperty("payload", response.ResultAsJson)
            );
        }

        public async Task<object> InvokeDeviceMethodAsync(string connectionId, string deviceId, object methodInvokeParameters)
        {
            Debug.WriteLine("InvokeDeviceMethodAsync received for {0} with deviceId {1} ", connectionId, deviceId);
            Debug.WriteLine(methodInvokeParameters.ToString());
            var client = objectMap[connectionId];
            var request = _jobjectToMethodRequest(methodInvokeParameters as JObject);
            Debug.WriteLine("Invoking");
            var response = await client.InvokeMethodAsync(deviceId, request, CancellationToken.None).ConfigureAwait(false);
            Debug.WriteLine("Response received:");
            Debug.WriteLine(JsonConvert.SerializeObject(response));
            return new JObject(
                new JProperty("status", response.Status),
                new JProperty("payload", response.ResultAsJson)
            );
        }

        public async Task<object> WaitForDesiredPropertyPatchAsync(string connectionId)
        {
            // Since there's no way to un-register for a patch, we have a global patch handler.  We keep the
            // "last desired props received" in a member varaible along with a mutex to trigger when this changes.
            // Not very cool and not very thread safe :(
            Debug.WriteLine("WaitForDesiredPropertyPatchAsync received for " + connectionId);
            var client = objectMap[connectionId];
            var mutex = new System.Threading.SemaphoreSlim(1);
            await mutex.WaitAsync().ConfigureAwait(false);  // Grab the mutex. The handler will release it later
            desiredPropMutex = mutex;

            Debug.WriteLine("Waiting for patch");
            await mutex.WaitAsync().ConfigureAwait(false);
            Debug.WriteLine("mutex triggered.");

            Debug.WriteLine("Returning patch:");
            Debug.WriteLine(JsonConvert.SerializeObject(lastDesiredProps));
            return lastDesiredProps;
        }

        public async Task<object> GetTwinAsync(string connectionId)
        {
            Debug.WriteLine("GetTwinAsync received for " + connectionId);
            var client = objectMap[connectionId];
            Twin t = await client.GetTwinAsync().ConfigureAwait(false);
            Debug.WriteLine("Twin Received");
            Debug.WriteLine(JsonConvert.SerializeObject(t));
            return t;
        }

        public async Task SendTwinPatchAsync(string connectionId, object props)
        {
            Debug.WriteLine("SendTwinPatchAsync received for " + connectionId);
            Debug.WriteLine(JsonConvert.SerializeObject(props));
            var client = objectMap[connectionId];
            TwinCollection reportedProps = new TwinCollection(props as JObject, null);
            await client.UpdateReportedPropertiesAsync(reportedProps).ConfigureAwait(false);
        }

        public async Task<object> RoundtripMethodCallAsync(string connectionId, string methodName, RoundtripMethodCallBody requestAndResponse)
        {
            Debug.WriteLine("RoundtripMethodCallAsync received for {0} and methodName {1}", connectionId, methodName);
            Debug.WriteLine(JsonConvert.SerializeObject(requestAndResponse));
            var client = objectMap[connectionId];
            var mutex = new System.Threading.SemaphoreSlim(1);
            await mutex.WaitAsync().ConfigureAwait(false);  // Grab the mutex. The handler will release it later

            MethodCallback callback = async (methodRequest, userContext) =>
            {
                Debug.WriteLine("Method invocation received");

                object request = JsonConvert.DeserializeObject(methodRequest.DataAsJson);
                string received = JsonConvert.SerializeObject(new JRaw(request));
                string expected = ((Newtonsoft.Json.Linq.JToken)requestAndResponse.RequestPayload)["payload"].ToString();
                Debug.WriteLine("request expected: " + expected);
                Debug.WriteLine("request received: " + received);
                if (expected != received)
                {
                    Debug.WriteLine("request did not match expectations");
                    Debug.WriteLine("Releasing the method mutex");
                    mutex.Release();
                    return new MethodResponse(500);
                }
                else
                {
                    int status = 200;
                    if (requestAndResponse.StatusCode != null)
                    {
                        status = (int)requestAndResponse.StatusCode;
                    }

                    byte[] responseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestAndResponse.ResponsePayload));

                    Debug.WriteLine("Releasing the method mutex");
                    mutex.Release();

                    Debug.WriteLine("Returning the result");
                    return new MethodResponse(responseBytes, status);
                }
            };

            Debug.WriteLine("Setting the handler");
            await client.SetMethodHandlerAsync(methodName, callback, null).ConfigureAwait(false);

            Debug.WriteLine("Waiting on the method mutex");
            await mutex.WaitAsync().ConfigureAwait(false);

            Debug.WriteLine("Method mutex released.  Waiting for a tiny bit.");  // Otherwise, the connection might close before the response is actually sent
            await Task.Delay(100).ConfigureAwait(false);

            Debug.WriteLine("Nulling the handler");
            await client.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);

            Debug.WriteLine("RoundtripMethodCallAsync is complete");
            return new object();
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
