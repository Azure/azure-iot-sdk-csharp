// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    internal class RegistryManagerSample
    {
        private readonly Parameters _parameters;
        private readonly List<string> _deviceIdsAdded = new();

        public RegistryManagerSample(Parameters parameters)
        {
            _parameters = parameters;
        }

        public async Task RunSampleAsync()
        {
            using RegistryManager registryManager = RegistryManager
                .CreateFromConnectionString(_parameters.IoTHubConnectionString);

            try
            {
                await CreateDeviceHierarchyAsync(registryManager);

                await AddDeviceWithSelfSignedCertificateAsync(registryManager);

                await AddDeviceWithCertificateAuthorityAuthenticationAsync(registryManager);

                await EnumerateTwinsAsync(registryManager);
            }
            finally
            {
                // clean up
                Console.WriteLine("\n=== Removing devices ===\n");

                foreach (string deviceId in _deviceIdsAdded)
                {
                    await RemoveDeviceAsync(registryManager, deviceId);
                }
            }
        }

        /// <summary>
        /// Creates some edge devices with a parent and child, and a leaf device as a child.
        /// </summary>
        private async Task CreateDeviceHierarchyAsync(RegistryManager registryManager)
        {
            Console.WriteLine("=== Creating a hierarchy of devices using default (symmetric key) authentication ===\n");

            string edgeParentId = GenerateDeviceId();
            var edgeParent = new Device(edgeParentId)
            {
                Capabilities = new DeviceCapabilities
                {
                    // To create an edge device, this must be set to true
                    IotEdge = true,
                },
            };

            // Add the device and capture the output which includes system-assigned properties like ETag and Scope.
            edgeParent = await registryManager.AddDeviceAsync(edgeParent);
            Console.WriteLine($"Added edge {edgeParent.Id} with device scope {edgeParent.Scope}.");

            string nestedEdgeId = GenerateDeviceId();
            var nestedEdge = new Device(nestedEdgeId)
            {
                Capabilities = new DeviceCapabilities
                {
                    IotEdge = true,
                },
                // To make this edge device a child of another edge device, add the parent's device scope to the parent scopes property.
                // The scope property is immutable for an edge device, and should not be set by the client.
                ParentScopes = { edgeParent.Scope },
            };

            nestedEdge = await registryManager.AddDeviceAsync(nestedEdge);
            Console.WriteLine($"Added edge {nestedEdge.Id} with device scope {nestedEdge.Scope} and parent scope {nestedEdge.ParentScopes.First()}.");

            // Create a device with default (shared key) authentication
            string basicDeviceId = GenerateDeviceId();
            var basicDevice = new Device(basicDeviceId)
            {
                // To make this device a child of an edge device, set the scope property to the parent's scope property value.
                // Note, this is different to how hierarchy is specified on edge devices.
                // The parent scopes property can be set to the same value, or left alone and the service will set it for you.
                Scope = nestedEdge.Scope,
            };

            basicDevice = await registryManager.AddDeviceAsync(basicDevice);
            Console.WriteLine($"Added device '{basicDevice.Id}' with device scope of {basicDevice.Scope} and parent scope of {basicDevice.ParentScopes.First()}.");
        }

        private async Task AddDeviceWithSelfSignedCertificateAsync(RegistryManager registryManager)
        {
            Console.WriteLine("\n=== Creating a device using self-signed certificate authentication ===\n");

            string selfSignedCertDeviceId = GenerateDeviceId();

            var device = new Device(selfSignedCertDeviceId)
            {
                Authentication = new AuthenticationMechanism
                {
                    Type = AuthenticationType.SelfSigned,
                    X509Thumbprint = new X509Thumbprint
                    {
                        PrimaryThumbprint = _parameters.PrimaryThumbprint,
                        SecondaryThumbprint = _parameters.SecondaryThumbprint,
                    },
                },
            };

            await registryManager.AddDeviceAsync(device);
            Console.WriteLine($"Added device {selfSignedCertDeviceId} with self-signed certificate auth. ");
        }

        private async Task AddDeviceWithCertificateAuthorityAuthenticationAsync(RegistryManager registryManager)
        {
            Console.WriteLine("\n=== Creating a device using CA-signed certificate authentication ===\n");

            string caCertDeviceId = GenerateDeviceId();
            var device = new Device(caCertDeviceId)
            {
                Authentication = new AuthenticationMechanism
                {
                    Type = AuthenticationType.CertificateAuthority,
                },
            };

            await registryManager.AddDeviceAsync(device);
            Console.WriteLine($"Added device {caCertDeviceId} with CA authentication.");

            // Demonstrate updating a twin's desired property
            await UpdateDesiredPropertiesAsync(registryManager, caCertDeviceId);
        }

        private static async Task RemoveDeviceAsync(RegistryManager registryManager, string deviceId)
        {
            try
            {
                await registryManager.RemoveDeviceAsync(deviceId);
                Console.WriteLine($"Removed device {deviceId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to remove device {deviceId} due to {ex.Message}.");
            }
        }

        private async Task EnumerateTwinsAsync(RegistryManager registryManager)
        {
            Console.WriteLine("\n=== Querying twins ===\n");
            await Task.Delay(5000); // give a little time for the twin store to reflect all our recent additions

            string queryText = $"SELECT * FROM devices WHERE STARTSWITH(id, '{_parameters.DevicePrefix}')";
            Console.WriteLine($"Using query text of: {queryText}");

            IQuery query = registryManager.CreateQuery(queryText);

            while (query.HasMoreResults)
            {
                IEnumerable<Twin> twins = await query.GetNextAsTwinAsync();

                foreach (Twin twin in twins)
                {
                    Console.WriteLine($"{twin.DeviceId}");
                    Console.WriteLine($"\tIs edge: {twin.Capabilities.IotEdge}");
                    if (!string.IsNullOrWhiteSpace(twin.DeviceScope))
                    {
                        Console.WriteLine($"\tDevice scope: {twin.DeviceScope}");
                    }
                    if (twin.ParentScopes?.Any() ?? false)
                    {
                        Console.WriteLine($"\tParent scope: {twin.ParentScopes?.FirstOrDefault()}");
                    }
                }
            }
        }

        private static async Task UpdateDesiredPropertiesAsync(RegistryManager registryManager, string deviceId)
        {
            Console.WriteLine("\n=== Updating a desired property value ===\n");

            Twin twin = await registryManager.GetTwinAsync(deviceId);

            // Set a desired value for a property the device supports, with the corresponding data type
            string patch =
                @"{
                ""properties"": {
                ""desired"": {
                    ""customKey"": ""customValue""
                }
                }
            }";
            Console.WriteLine($"Using property patch of:\n{patch}");

            await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);
        }

        private string GenerateDeviceId()
        {
            string deviceId = _parameters.DevicePrefix + Guid.NewGuid().ToString().Substring(0, 8);
            _deviceIdsAdded.Add(deviceId);
            return deviceId;
        }
    }
}
