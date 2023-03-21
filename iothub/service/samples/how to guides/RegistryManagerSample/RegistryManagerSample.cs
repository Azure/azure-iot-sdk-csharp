// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;

namespace Microsoft.Azure.Devices.Samples
{
    internal class RegistryManagerSample
    {
        private readonly List<string> _deviceIdsAdded = new();
        private readonly IotHubServiceClient _client;
        private readonly Parameters _parameters;

        public RegistryManagerSample(IotHubServiceClient client, Parameters parameters)
        {
            _client = client;
            _parameters = parameters;
        }

        public async Task RunSampleAsync()
        {
            try
            {
                await CreateDeviceHierarchyAsync();

                await AddDeviceWithSelfSignedCertificateAsync();

                await AddDeviceWithCertificateAuthorityAuthenticationAsync();

                await EnumerateTwinsAsync();
            }
            finally
            {
                // clean up
                Console.WriteLine("\n=== Removing devices ===\n");

                foreach (string deviceId in _deviceIdsAdded)
                {
                    await RemoveDeviceAsync(deviceId);
                }
            }
        }

        /// <summary>
        /// Creates some edge devices with a parent and child, and a leaf device as a child.
        /// </summary>
        private async Task CreateDeviceHierarchyAsync()
        {
            Console.WriteLine("=== Creating a hierarchy of devices using default (symmetric key) authentication ===\n");

            string edgeParentId = GenerateDeviceId();
            var edgeParent = new Device(edgeParentId)
            {
                Capabilities = new ClientCapabilities
                {
                    // To create an edge device, this must be set to true
                    IsIotEdge = true,
                },
            };

            // Add the device and capture the output which includes system-assigned properties like ETag and Scope.
            edgeParent = await _client.Devices.CreateAsync(edgeParent);
            Console.WriteLine($"Added edge {edgeParent.Id} with device scope {edgeParent.Scope}.");

            string nestedEdgeId = GenerateDeviceId();
            var nestedEdge = new Device(nestedEdgeId)
            {
                Capabilities = new ClientCapabilities
                {
                    IsIotEdge = true,
                },
                // To make this edge device a child of another edge device, add the parent's device scope to the parent scopes property.
                // The scope property is immutable for an edge device, and should not be set by the client.
                ParentScopes = { edgeParent.Scope },
            };

            nestedEdge = await _client.Devices.CreateAsync(nestedEdge);
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

            basicDevice = await _client.Devices.CreateAsync(basicDevice);
            Console.WriteLine($"Added device '{basicDevice.Id}' with device scope of {basicDevice.Scope} and parent scope of {basicDevice.ParentScopes.First()}.");
        }

        private async Task AddDeviceWithSelfSignedCertificateAsync()
        {
            Console.WriteLine("\n=== Creating a device using self-signed certificate authentication ===\n");

            string selfSignedCertDeviceId = GenerateDeviceId();

            var device = new Device(selfSignedCertDeviceId)
            {
                Authentication = new AuthenticationMechanism
                {
                    Type = ClientAuthenticationType.SelfSigned,
                    X509Thumbprint = new X509Thumbprint
                    {
                        PrimaryThumbprint = _parameters.PrimaryThumbprint,
                        SecondaryThumbprint = _parameters.SecondaryThumbprint,
                    },
                },
            };

            await _client.Devices.CreateAsync(device);
            Console.WriteLine($"Added device {selfSignedCertDeviceId} with self-signed certificate auth. ");
        }

        private async Task AddDeviceWithCertificateAuthorityAuthenticationAsync()
        {
            Console.WriteLine("\n=== Creating a device using CA-signed certificate authentication ===\n");

            string caCertDeviceId = GenerateDeviceId();
            var device = new Device(caCertDeviceId)
            {
                Authentication = new AuthenticationMechanism
                {
                    Type = ClientAuthenticationType.CertificateAuthority,
                },
            };

            await _client.Devices.CreateAsync(device);
            Console.WriteLine($"Added device {caCertDeviceId} with CA authentication.");

            // Demonstrate updating a twin's desired property
            await UpdateDesiredPropertiesAsync(caCertDeviceId);
        }

        private async Task RemoveDeviceAsync(string deviceId)
        {
            try
            {
                await _client.Devices.DeleteAsync(deviceId);
                Console.WriteLine($"Removed device {deviceId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to remove device {deviceId} due to {ex.Message}.");
            }
        }

        private async Task EnumerateTwinsAsync()
        {
            Console.WriteLine("\n=== Querying twins ===\n");
            await Task.Delay(5000); // give a little time for the twin store to reflect all our recent additions

            string queryText = $"SELECT * FROM devices WHERE STARTSWITH(id, '{_parameters.DevicePrefix}')";
            Console.WriteLine($"Using query text of: {queryText}");

            AsyncPageable<ClientTwin> query = _client.Query.CreateAsync<ClientTwin>(queryText);

            await foreach (ClientTwin queriedTwin in query)
            {
                Console.WriteLine($"{queriedTwin.DeviceId}");
                Console.WriteLine($"\tIs edge: {queriedTwin.Capabilities.IsIotEdge}");
                if (!string.IsNullOrWhiteSpace(queriedTwin.DeviceScope))
                {
                    Console.WriteLine($"\tDevice scope: {queriedTwin.DeviceScope}");
                }
                if (queriedTwin.ParentScopes?.Any() ?? false)
                {
                    Console.WriteLine($"\tParent scope: {queriedTwin.ParentScopes[0]}");
                }
            }
        }

        private async Task UpdateDesiredPropertiesAsync(string deviceId)
        {
            Console.WriteLine("\n=== Updating a desired property value ===\n");

            ClientTwin twin = await _client.Twins.GetAsync(deviceId);

            // Set a desired value for a property the device supports, with the corresponding data type
            var patch = new ClientTwin(twin.DeviceId)
            {
                ETag = twin.ETag,
            };
            patch.Properties.Desired["customKey"] = "customValue";
            Console.WriteLine($"Using property patch of:\n{patch}");

            await _client.Twins.UpdateAsync(twin.DeviceId, patch, true);
        }

        private string GenerateDeviceId()
        {
            string deviceId = string.Concat(_parameters.DevicePrefix, Guid.NewGuid().ToString().AsSpan(0, 8));
            _deviceIdsAdded.Add(deviceId);
            return deviceId;
        }
    }
}
