// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.ResourceManager.EdgeOrder;
using Azure.ResourceManager;
using Azure;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Discovery.Client;
using Microsoft.Azure.Devices.Discovery.Client.Transport;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Azure.Core;
using Microsoft.Azure.Devices.Provisioning.Security;
using Azure.ResourceManager.Resources.Models;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Runtime.ConstrainedExecution;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using System.IO;

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("DPS")]
    public class DiscoveryE2ETests : E2EMsTestBase
    {
        private const int PassingTimeoutMiliseconds = 10 * 60 * 1000;
        private const int FailingTimeoutMiliseconds = 10 * 1000;
        private const int MaxTryCount = 10;
        private const string InvalidGlobalAddress = "httpbin.org";
        private static readonly string s_globalDiscoveryEndpoint = TestConfiguration.Discovery.GlobalDeviceEndpoint;
        private static readonly string s_subscriptionId = TestConfiguration.Discovery.SubscriptionId;
        private static readonly string s_resourceGroup1 = TestConfiguration.Discovery.ResourceGroup1;
        private static readonly string s_resourceGroup2 = TestConfiguration.Discovery.ResourceGroup2;
        private static readonly string s_resourceOwner = TestConfiguration.Discovery.ResourceOwner;
        private static readonly string s_registrationId = TestConfiguration.Discovery.RegistrationId;
        private static readonly string s_skipArtifacts = TestConfiguration.Discovery.SkipArtifacts;
        private static readonly string s_proxyServerAddress = "";
        private static readonly string provResourceApiVersion = "?api-version=2023-12-01-preview";

        private static readonly HashSet<Type> s_retryableExceptions = new() { typeof(ProvisioningServiceClientHttpException) };
        private static readonly IRetryPolicy s_provisioningServiceRetryPolicy = new ProvisioningServiceRetryPolicy();

        private readonly string _idPrefix = $"e2e-{nameof(DiscoveryE2ETests).ToLower()}-";

        private List<string> azureResources;
        private HttpClient client;

        [ClassInitialize]
        public static void TestClassSetup(TestContext _)
        {
        }

        [TestInitialize]
        public void TestSetup()
        {
            azureResources = new List<string>();
            client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestConfiguration.Discovery.AzureBearerToken.Trim());
        }

        [TestMethod]
        [Timeout(LongerRunningTestTimeoutMilliseconds)]
        public async Task DPS_Onboard_Ok()
        {
            await UploadDevice();

            await ClientValidOnboardingAsyncOk(false).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongerRunningTestTimeoutMilliseconds)]
        public async Task DPS_Onboard_Discovery_Service_Ok()
        {
            await UploadDevice(uploadWithOrderingService: false);

            await ClientValidOnboardingAsyncOk(false).ConfigureAwait(false);
        }

        #region InvalidGlobalAddress

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Onboard_InvalidProvisioningAddress()
        {
            await UploadDevice();

            await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(() => ClientValidOnboardingAsyncOk(false, invalidProvisioningEndpoint: true));
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Onboard_InvalidDiscoveryAddress()
        {
            await UploadDevice();

            await Assert.ThrowsExceptionAsync<DiscoveryTransportException>(() => ClientValidOnboardingAsyncOk(false, discoveryEndpoint: InvalidGlobalAddress));
        }

        #endregion InvalidGlobalAddress

        #region InvalidArtifacts

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Onboard_InvalidSrk()
        {
            await UploadDevice(invalidSrk: true);

            await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(() => ClientValidOnboardingAsyncOk(false, invalidProvisioningEndpoint: true));
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Onboard_InvalidEk()
        {
            await UploadDevice(invalidEk: true);

            await Assert.ThrowsExceptionAsync<DiscoveryTransportException>(() => ClientValidOnboardingAsyncOk(false, discoveryEndpoint: InvalidGlobalAddress));
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Onboard_InvalidSrkEk()
        {
            await UploadDevice(invalidEk: true, invalidSrk: true);

            await Assert.ThrowsExceptionAsync<DiscoveryTransportException>(() => ClientValidOnboardingAsyncOk(false, discoveryEndpoint: InvalidGlobalAddress));
        }

        #endregion InvalidGlobalAddress

        public async Task ClientValidOnboardingAsyncOk(
            bool setCustomProxy,
            string proxyServerAddress = null,
            string discoveryEndpoint = null,
            bool invalidProvisioningEndpoint = false)
        {
            // Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            await ClientValidOnboardingAsyncOk(
                    setCustomProxy,
                    TimeSpan.MaxValue,
                    proxyServerAddress,
                    discoveryEndpoint,
                    invalidProvisioningEndpoint)
                .ConfigureAwait(false);
        }

        private async Task ClientValidOnboardingAsyncOk(
            bool setCustomProxy,
            TimeSpan timeout,
            string proxyServerAddress = null,
            string discoveryEndpoint = null,
            bool invalidProvisioningEndpoint = false)
        {
            // The range of valid combinations of configuration is very limited, there are fewer cases for us to test

            string registrationId = s_registrationId;

            using DiscoveryTransportHandler transport = new DiscoveryTransportHandlerHttp();
            using SecurityProvider security = new SecurityProviderTpmHsm(registrationId);

            if (setCustomProxy)
            {
                transport.Proxy = proxyServerAddress == null
                    ? null
                    : new WebProxy(s_proxyServerAddress);
            }

            var client = DiscoveryDeviceClient.Create(
                discoveryEndpoint ?? s_globalDiscoveryEndpoint,
                security,
                transport);

            using var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

            Console.WriteLine("Getting nonce for challenge... ");
            byte[] nonce = await client.IssueChallengeAsync(cts.Token);

            Console.WriteLine($"Received nonce {Convert.ToBase64String(nonce, 0, 10)}");

            OnboardingInfo onboardingInfo = await client.GetOnboardingInfoAsync(nonce, cts.Token);

            Console.WriteLine($"Received endpoint: {onboardingInfo.EdgeProvisioningEndpoint}");

            using ProvisioningTransportHandler provTransport = new ProvisioningTransportHandlerHttp();
            using SecurityProvider provSecurity = new SecurityProviderX509Certificate(onboardingInfo.ProvisioningCertificate[0], onboardingInfo.ProvisioningCertificate);

            if (setCustomProxy)
            {
                provTransport.Proxy = proxyServerAddress == null
                    ? null
                    : new WebProxy(s_proxyServerAddress);
            }

            string provisioningEndpoint = onboardingInfo.EdgeProvisioningEndpoint;

            if (invalidProvisioningEndpoint)
            {
                provisioningEndpoint = InvalidGlobalAddress;
            }

            var provClient = ProvisioningDeviceClient.Create(
                provisioningEndpoint,
                provSecurity,
                provTransport);

            DeviceOnboardingResult onboardingResult = await provClient.OnboardAsync("random string", cts.Token);

            Console.WriteLine($"Successfully onboarded {onboardingResult.Id} {onboardingResult.Result.RegistrationId}");
        }

        private async Task UploadDevice(bool uploadWithOrderingService = true, bool invalidSrk = false, bool invalidEk = false)
        {
            if (string.Equals(s_skipArtifacts.Trim(), "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (uploadWithOrderingService)
            {
                await UploadDeviceWithOrderingService(invalidSrk, invalidEk);
            }
            else
            {
                await UploadDeviceWithDiscoveryService(invalidSrk, invalidEk);
            }
        }

        private async Task UploadDeviceWithOrderingService(bool invalidSrk, bool invalidEk)
        {
            string subscriptionId = s_subscriptionId;
            string resourceGroupName = s_resourceGroup1;
            string resourceGroupName2 = s_resourceGroup2;
            string siteResourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/";
            string serialNumber = s_registrationId;
            using SecurityProviderTpm security = new SecurityProviderTpmHsm(serialNumber);

            try
            {
                var azureTags = new AzureResourceTags()
                {
                    owner = s_resourceOwner,
                    purpose = "e2etesting"
                };

                string bootstrapResourceName = TestConfiguration.Discovery.BootstrapResourceName;

                bool createBootstrapResource = string.IsNullOrEmpty(bootstrapResourceName);

                if (createBootstrapResource)
                {
                    bootstrapResourceName = $"{Guid.NewGuid()}";
                }

                string apiVersion = "?api-version=2023-05-01-preview";
                string baseUri = $"management.azure.com{siteResourceId}providers/Microsoft.EdgeOrder/bootstrapConfigurations/{bootstrapResourceName}";
                string bootstrapResourceUri = $"{baseUri}{apiVersion}";

                if (createBootstrapResource)
                {
                    await MakeBootstrapResource(siteResourceId, azureTags, bootstrapResourceUri);
                }

                // get token (site key) for bootstrap resource
                string tokenUri = $"{baseUri}/listToken{apiVersion}";
                var content = new StringContent("");
                var response = await client.PostAsync($"https://{tokenUri}", content);

                content.Dispose();

                Console.WriteLine($"Sent token request: {response.StatusCode}");
                var siteKeyString = await response.Content.ReadAsStringAsync();
                var siteKeyObj = JsonConvert.DeserializeObject<BootstrapSiteKey>(siteKeyString);
                Console.WriteLine($"Got token: {siteKeyObj.Token.Substring(0,32)}");

                // upload artifacts
                
                var armClientOptions = new ArmClientOptions()
                {
                    Diagnostics =
                    {
                        IsLoggingContentEnabled = true,
                    },
                };

                string srk = Convert.ToBase64String(security.GetStorageRootKey());
                string ek = Convert.ToBase64String(security.GetEndorsementKey());

                if (invalidSrk)
                {
                    srk = "invalid";
                }

                if (invalidEk)
                {
                    ek = "invalid";
                }

                var artifacts = new AzureHardwareCenterArtifacts()
                {
                    Metadata = new AzureHardwareCenterMetadataWrapper(new AzureHardwareCenterMetadata()
                    {
                        ProductFamily = "azurestackhci",
                        SerialNumber = serialNumber,
                        ApplianceID = new ApplianceID(srk, "V12"),
                        EndorsementKeyPublic = ek,
                        Manufacturer = "Manufacturer",
                        Model = "Model",
                        Version = "V1",
                        Kind = "AzureStackHCIMetadata",
                        ConfigurationId = "sampleConfigId",
                        CreateTimeUTC = DateTime.Now,
                    }),
                    PartnerAdditionalMetadata = new AzureHardwareCenterAdditionalMetadata()
                    {
                        Kind = "AzureStackHCIAdditionalMetadata",
                        Version = "V1",
                        Properties = new AdditionalProperties(new List<string>())
                    }
                };

                var artifactResponse = EdgeOrderExtensions.UploadDeviceArtifacts(siteKeyObj.Token, serialNumber, JsonConvert.SerializeObject(artifacts), WaitUntil.Completed, armClientOptions: armClientOptions);

                Console.WriteLine($"Artifacts uploaded: {artifactResponse.HasCompleted}");

                // check for or create provisioning resource

                string provResourceName = TestConfiguration.Discovery.ProvisioningResourceName;

                bool shouldMakeProvResource = string.IsNullOrEmpty(provResourceName);

                if (shouldMakeProvResource)
                {
                    provResourceName = GetRandomResourceName(serialNumber);
                }

                string provisioningResourceFullyQualifiedName = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName2}/providers/Microsoft.FairfieldGardens/provisioningResources/{provResourceName}";
                string provisioningResourceBaseUri = $"eastus2euap.management.azure.com{provisioningResourceFullyQualifiedName}";
                string provisioningResourceUri = $"{provisioningResourceBaseUri}{provResourceApiVersion}";

                if (shouldMakeProvResource)
                {
                    await MakeProvisioningResource(azureTags, provisioningResourceUri);
                }

                // check for or create provisioning resource policy

                string provResourcePolicyName = TestConfiguration.Discovery.ProvisioningPolicyResourceName;

                bool shouldMakeProvResourcePolicy = string.IsNullOrEmpty(provResourcePolicyName);

                if (shouldMakeProvResourcePolicy)
                {
                    provResourcePolicyName = GetRandomResourceName(serialNumber);
                }

                string provisioningResourcePolicyFullyQualifiedName = $"{provisioningResourceFullyQualifiedName}/provisioningPolicies/{provResourcePolicyName}";
                string provisioningResourcePolicyUri = $"{provisioningResourceBaseUri}/provisioningPolicies/{provResourcePolicyName}{provResourceApiVersion}";

                if (shouldMakeProvResourcePolicy)
                {
                    await MakeProvisioningResourcePolicy(azureTags, provisioningResourcePolicyUri);
                }

                // add the resources in order of how they should be deleted
                if (shouldMakeProvResourcePolicy)
                {
                    azureResources.Add(provisioningResourcePolicyUri);
                }
                if (shouldMakeProvResource)
                {
                    azureResources.Add(provisioningResourceUri);
                }

                // create arc device and extension
                await CreateArcDeviceAndExtension(azureTags, provisioningResourcePolicyFullyQualifiedName);

                // wait a bit
                Console.WriteLine($"Sleeping 180s");
                Thread.Sleep(180 * 1000);

                // patch the order
                string edgeOrderPatchUri = $"management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EdgeOrder/orderItems/{serialNumber}?api-version=2023-05-01-preview";

                var patch = new EdgeOrderPatch()
                {
                    Properties = new EdgeOrderPatch.EdgeOrderPatchProperties()
                    {
                        OrderItemDetails = new EdgeOrderPatch.EdgeOrderPatchProperties.EdgeOrderPatchOrderItemDetails()
                        {
                            ProductDetails = new EdgeOrderPatch.EdgeOrderPatchProperties.EdgeOrderPatchOrderItemDetails.EdgeOrderPatchProductDetails()
                            {
                                ProvisioningDetails = new EdgeOrderPatch.EdgeOrderPatchProperties.EdgeOrderPatchOrderItemDetails.EdgeOrderPatchProductDetails.EdgeOrderPatchProvisioningDetails()
                                {
                                    SerialNumber = serialNumber,
                                    ReadyToConnectArmId = provisioningResourcePolicyFullyQualifiedName,
                                    ProvisioningArmId = provisioningResourceFullyQualifiedName,
                                    ProvisioningEndpoint = $"{provResourceName}.eastus2euap.ffg.azure.net"
                                }
                            }
                        }
                    }
                };

                content = new StringContent(JsonConvert.SerializeObject(patch), Encoding.UTF8, "application/json");
                response = await client.PatchAsync($"https://{edgeOrderPatchUri}", content);
                content.Dispose();

                Console.WriteLine($"Patch order: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());

                response = await client.GetAsync($"https://{edgeOrderPatchUri}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());

                Console.WriteLine($"Sleeping 60s");
                Thread.Sleep(60000);
            }
            finally
            {

            }
        }

        private async Task UploadDeviceWithDiscoveryService(bool invalidSrk, bool invalidEk)
        {
            using SecurityProviderTpm security = new SecurityProviderTpmHsm(s_registrationId);

            try
            {
                var azureTags = new AzureResourceTags()
                {
                    owner = s_resourceOwner,
                    purpose = "e2etesting"
                };

                // check for or create provisioning resource

                string provResourceName = TestConfiguration.Discovery.ProvisioningResourceName;

                bool shouldMakeProvResource = string.IsNullOrEmpty(provResourceName);

                if (shouldMakeProvResource)
                {
                    provResourceName = GetRandomResourceName(s_registrationId);
                }

                string provResourceApiVersion = "?api-version=2023-12-01-preview";

                string provisioningResourceFullyQualifiedName = $"/subscriptions/{s_subscriptionId}/resourceGroups/{s_resourceGroup2}/providers/Microsoft.FairfieldGardens/provisioningResources/{provResourceName}";
                string provisioningResourceBaseUri = $"eastus2euap.management.azure.com{provisioningResourceFullyQualifiedName}";
                string provisioningResourceUri = $"{provisioningResourceBaseUri}{provResourceApiVersion}";

                if (shouldMakeProvResource)
                {
                    await MakeProvisioningResource(azureTags, provisioningResourceUri);
                }

                // check for or create provisioning resource policy

                string provResourcePolicyName = TestConfiguration.Discovery.ProvisioningPolicyResourceName;

                bool shouldMakeProvResourcePolicy = string.IsNullOrEmpty(provResourcePolicyName);

                if (shouldMakeProvResourcePolicy)
                {
                    provResourcePolicyName = GetRandomResourceName(s_registrationId);
                }

                string provisioningResourcePolicyFullyQualifiedName = $"{provisioningResourceFullyQualifiedName}/provisioningPolicies/{provResourcePolicyName}";
                string provisioningResourcePolicyUri = $"{provisioningResourceBaseUri}/provisioningPolicies/{provResourcePolicyName}{provResourceApiVersion}";

                if (shouldMakeProvResourcePolicy)
                {
                    await MakeProvisioningResourcePolicy(azureTags, provisioningResourcePolicyUri);
                }

                // add the resources in order of how they should be deleted
                if (shouldMakeProvResourcePolicy)
                {
                    azureResources.Add(provisioningResourcePolicyUri);
                }
                if (shouldMakeProvResource)
                {
                    azureResources.Add(provisioningResourceUri);
                }

                // create arc device and extension
                await CreateArcDeviceAndExtension(azureTags, provisioningResourcePolicyFullyQualifiedName);

                // patch discovery device
                string discoveryPatchUri = $"prod.eastus2euap.service.discovery.ffg.azure.net/discovery/devices/{s_registrationId}";

                var patch = new DiscoveryDevicePatch()
                {
                    TpmDetails = new DiscoveryDevicePatch.DiscoveryDeviceTpmDetails()
                    {
                        EndorsementKey = Convert.ToBase64String(security.GetEndorsementKey()),
                        SigningKey = Convert.ToBase64String(security.GetStorageRootKey())
                    },
                    EdgeProvisioningEndpoint = $"{provResourceName}.eastus2euap.ffg.azure.net",
                    RegistrationId = s_registrationId
                };

                var handler = new HttpClientHandler();
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.SslProtocols = SslProtocols.Tls12;

                var cert = new X509Certificate2(TestConfiguration.Discovery.DiscoveryServiceCertificatePath);

                handler.ClientCertificates.Add(cert);
                var discoveryServiceClient = new HttpClient(handler);

                var content = new StringContent(JsonConvert.SerializeObject(patch), Encoding.UTF8, "application/json");
                var response = await discoveryServiceClient.PutAsync($"https://{discoveryPatchUri}?api-version=2023-12-01-preview", content);
                content.Dispose();

                discoveryServiceClient.Dispose();
                handler.Dispose();

                Console.WriteLine($"Patch order: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());

                Console.WriteLine($"Sleeping 60s");
                Thread.Sleep(60000);
            }
            finally
            {

            }
        }

        private string GetRandomResourceName(string baseName)
        {
            return $"{baseName}-{Guid.NewGuid().ToString("N").Substring(0, 5)}t";
        }

        private async Task MakeBootstrapResource(string siteResourceId, AzureResourceTags azureTags, string bootstrapResourceUri)
        {
            // create bootstrap resource
            var bootstrapResourceRequest = new AzureBootstrapResource()
            {
                Properties = new AzureBootstrapResource.BootstrapResourceProperties()
                {
                    SiteResourceId = siteResourceId,
                    MaximumNumberOfDevicesToOnboard = 10,
                    TokenExpiryDate = DateTime.Now.AddDays(5).ToString("o", CultureInfo.InvariantCulture)
                },
                Identity = new AzureBootstrapResource.BootstrapResourceIdentity()
                {
                    Type = "SystemAssigned"
                },
                Tags = azureTags,
                Location = "eastus"
            };

            await MakeAzurePutCall(bootstrapResourceUri, bootstrapResourceRequest, "Bootstrap resource");

            azureResources.Add(bootstrapResourceUri);
        }

        private async Task MakeProvisioningResource(AzureResourceTags azureTags, string provisioningResourceUri)
        {
            // make provisioning resource
            var provisioningResource = new ProvisioningResource()
            {
                Location = "eastus2euap",
                Tags = azureTags,
                Properties = new ProvisioningResource.ProvisioningProperties()
                {
                    enableOperationalCertificates = true
                }
            };

            await PollForAzurePut(provisioningResourceUri, provisioningResource, "Provisioning resource");
        }

        private async Task MakeProvisioningResourcePolicy(AzureResourceTags azureTags, string provisioningResourcePolicyUri)
        {
            var provisioningResourcePolicy = new ProvisioningPolicyResource()
            {
                Location = "eastus2euap",
                Tags = azureTags,
                Properties = new ProvisioningPolicyResource.ProvisioningPolicyProperties()
                {
                    BootstrapAuthentication = new ProvisioningPolicyResource.ProvisioningPolicyProperties.ProvisioningPolicyPropertyAuth() { Type = "Discovery" },
                    ResourceDetails = new ProvisioningPolicyResource.ProvisioningPolicyProperties.ProvisioningPolicyResourceDetails() { ResourceType = "Microsoft.HybridCompute/machines" },
                    Status = true
                },
            };

            await PollForAzurePut(provisioningResourcePolicyUri, provisioningResourcePolicy, "Provisioning policy resource");
        }

        private async Task CreateArcDeviceAndExtension(AzureResourceTags azureTags, string provisioningResourcePolicyFullyQualifiedName)
        {
            // make arc device resource

            string arcDeviceApiVersion = "?api-version=2023-03-15-preview";

            string arcDeviceUri = $"management.azure.com/subscriptions/{s_subscriptionId}/resourceGroups/{s_resourceGroup2}/providers/Microsoft.HybridCompute/machines/{s_registrationId}";

            var deviceResource = new ArcDeviceResource()
            {
                Location = "eastus2euap",
                Tags = azureTags
            };

            var arcDeviceResourceUri = $"{arcDeviceUri}{arcDeviceApiVersion}";

            await MakeAzurePutCall(arcDeviceResourceUri, deviceResource, "Arc device");

            // make arc device extension resource

            var deviceExtensionResource = new ArcDeviceExtensionResource()
            {
                Properties = new ArcDeviceExtensionResource.ArcDeviceExtensionResourceProperties()
                {
                    registrationId = s_registrationId.ToLowerInvariant(),
                    provisioningPolicyResourceId = provisioningResourcePolicyFullyQualifiedName,
                }
            };

            //var deviceExtensionUri = $"eastus2euap.{arcDeviceUri}/providers/Private.BBeeSta1/DeviceProvisioningStates/default{provResourceApiVersion}";
            var deviceExtensionUri = $"eastus2euap.{arcDeviceUri}/providers/Microsoft.FairfieldGardens/DeviceProvisioningStates/default{provResourceApiVersion}";

            await MakeAzurePutCall(deviceExtensionUri, deviceExtensionResource, "Arc device extension");

            // add the resources in order of how they should be deleted
            azureResources.Add(deviceExtensionUri);
            azureResources.Add(arcDeviceResourceUri);
        }

        /// <summary>
        /// Polls azure resource until it is in a succeeded state
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="data"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        private async Task PollForAzurePut(string uri, object data, string description)
        {
            await MakeAzurePutCall(uri, data, description);

            HttpResponseMessage response = null;
            int secondsDelay = 10;

            for (int i = 0; i < 40; i++)
            {
                await Task.Delay(secondsDelay * 1000);

                response = await client.GetAsync($"https://{uri}");
                Console.WriteLine($"Get prov resource after {secondsDelay}s: {response.StatusCode}");
                string responseContent = await response.Content.ReadAsStringAsync();

                bool result = responseContent.IndexOf("succeeded", StringComparison.OrdinalIgnoreCase) >= 0;
                bool failed = responseContent.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0;

                if (result)
                {
                    Console.WriteLine($"Finally succeeded!");
                    break;
                }

                if (failed)
                {
                    throw new Exception("Failed to create provisioning resource");
                }
            }
            Console.WriteLine($"Polling completed: ${await response.Content.ReadAsStringAsync()}");
        }

        private async Task MakeAzurePutCall(string uri, object data, string description)
        {
            Console.WriteLine($"Making put call for: {description}");

            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"https://{uri}", content);
            content.Dispose();

            Console.WriteLine($"{description}: {response.StatusCode}");
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Clean up the azure resources created
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            foreach (string resource in azureResources)
            {
                Console.WriteLine($"Going to delete {resource}");

                try
                {
                    var response = client.DeleteAsync($"https://{resource}").Result;
                    Console.WriteLine($"Deleted: {response.StatusCode}");
                    Console.WriteLine(response.Content.ReadAsStringAsync().Result);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Deleting failed: {e.Message}");
                }
            }

            azureResources.Clear();

            client.Dispose();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            // cleanup
        }
    }
}
