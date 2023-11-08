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
        private static readonly string s_proxyServerAddress = "";

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
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestConfiguration.Discovery.AzureBearerToken);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Onboard_Ok()
        {
            await UploadArtifacts();

            await ClientValidOnboardingAsyncOk(false).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Onboard_Proxy_Ok()
        {
            await ClientValidOnboardingAsyncOk(true).ConfigureAwait(false);
        }

        #region InvalidGlobalAddress



        #endregion InvalidGlobalAddress

        public async Task ClientValidOnboardingAsyncOk(
            bool setCustomProxy,
            string proxyServerAddress = null)
        {
            // Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            await ClientValidOnboardingAsyncOk(
                    setCustomProxy,
                    TimeSpan.MaxValue,
                    proxyServerAddress)
                .ConfigureAwait(false);
        }

        private async Task ClientValidOnboardingAsyncOk(
            bool setCustomProxy,
            TimeSpan timeout,
            string proxyServerAddress = null)
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
                s_globalDiscoveryEndpoint,
                security,
                transport);

            using var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

            Console.WriteLine("Getting nonce for challenge... ");
            string nonce = await client.IssueChallengeAsync(cts.Token);

            Console.WriteLine($"Received nonce");

            OnboardingInfo onboardingInfo = await client.GetOnboardingInfoAsync(nonce, cts.Token);

            Console.WriteLine($"Received endpoint: {onboardingInfo.EdgeProvisioningEndpoint}");

            using var cert = new X509Certificate2(onboardingInfo.ProvisioningCertificate.Export(X509ContentType.Pfx));

            using ProvisioningTransportHandler provTranspot = new ProvisioningTransportHandlerHttp();
            using SecurityProvider provSecurity = new SecurityProviderX509Certificate(cert);

            if (setCustomProxy)
            {
                provTranspot.Proxy = proxyServerAddress == null
                    ? null
                    : new WebProxy(s_proxyServerAddress);
            }

            var provClient = ProvisioningDeviceClient.Create(
                onboardingInfo.EdgeProvisioningEndpoint,
                provSecurity,
                provTranspot);

            DeviceOnboardingResult onboardingResult = await provClient.OnboardAsync("random string", cts.Token);

            Console.WriteLine($"Successfully onboarded {onboardingResult.Id} {onboardingResult.Result.RegistrationId}");
        }

        private async Task UploadArtifacts()
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

                var artifacts = new AzureHardwareCenterArtifacts()
                {
                    Metadata = new AzureHardwareCenterMetadataWrapper(new AzureHardwareCenterMetadata()
                    {
                        ProductFamily = "azurestackhci",
                        SerialNumber = serialNumber,
                        ApplianceID = new ApplianceID(Convert.ToBase64String(security.GetStorageRootKey()), "V12"),
                        EndorsementKeyPublic = Convert.ToBase64String(security.GetEndorsementKey()),
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

                // make provisioning resource
                var provisioningResource = new ProvisioningResource()
                {
                    Location = "eastus",
                    Tags = azureTags,
                    Properties = new ProvisioningResource.ProvisioningProperties()
                };

                string provResourceName = GetRandomResourceName(serialNumber);
                string provResourceApiVersion = "?api-version=2023-12-01-preview";

                Console.WriteLine($"Going to create provisioning resource: {provResourceName}");

                string provisioningResourceFullyQualifiedName = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName2}/providers/Private.BbeeSta1/provisioningResources/{provResourceName}";
                string provisioningResourceBaseUri = $"eastus2euap.management.azure.com{provisioningResourceFullyQualifiedName}";
                string provisioningResourceUri = $"{provisioningResourceBaseUri}{provResourceApiVersion}";
                

                content = new StringContent(JsonConvert.SerializeObject(provisioningResource), Encoding.UTF8, "application/json");
                response = await client.PutAsync($"https://{provisioningResourceUri}", content);
                content.Dispose();

                Console.WriteLine($"Provisioning resource: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());

                // make provisioning resource policy
                var provisioningResourcePolicy = new ProvisioningPolicyResource()
                {
                    Location = "eastus",
                    Tags = azureTags,
                    Properties = new ProvisioningPolicyResource.ProvisioningPolicyProperties() 
                    { 
                        BootstrapAuthentication = new ProvisioningPolicyResource.ProvisioningPolicyProperties.ProvisioningPolicyPropertyAuth() { Type = "Discovery" },
                        ResourceDetails = new ProvisioningPolicyResource.ProvisioningPolicyProperties.ProvisioningPolicyResourceDetails() { ResourceType = "Microsoft.HybridCompute/machines" }
                    },
                };

                string provResourcePolicyName = GetRandomResourceName(serialNumber);
                Console.WriteLine($"Going to create provisioning resource policy: {provResourcePolicyName}");

                string provisioningResourcePolicyFullyQualifiedName = $"{provisioningResourceFullyQualifiedName}/provisioningPolicies/{provResourcePolicyName}";
                string provisioningResourcePolicyUri = $"{provisioningResourceBaseUri}/provisioningPolicies/{provResourcePolicyName}{provResourceApiVersion}";

                content = new StringContent(JsonConvert.SerializeObject(provisioningResourcePolicy), Encoding.UTF8, "application/json");
                response = await client.PutAsync($"https://{provisioningResourcePolicyUri}", content);
                content.Dispose();

                Console.WriteLine($"Provisioning policy resource: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());

                // add the resources in order of how they should be deleted
                azureResources.Add(provisioningResourcePolicyUri);
                azureResources.Add(provisioningResourceUri);

                // make arc device resource

                string arcDeviceApiVersion = "?api-version=2023-03-15-preview";

                string arcDeviceUri = $"management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName2}/providers/Microsoft.HybridCompute/machines/{serialNumber}";

                var deviceResource = new ArcDeviceResource()
                {
                    Location = "eastus",
                    Tags = azureTags
                };

                var arcDeviceResourceUri = $"{arcDeviceUri}{arcDeviceApiVersion}";

                await MakeAzurePutCall(arcDeviceResourceUri, deviceResource, "Arc device");

                // make arc device extension resource

                var deviceExtensionResource = new ArcDeviceExtensionResource()
                {
                    Properties = new ArcDeviceExtensionResource.ArcDeviceExtensionResourceProperties()
                    {
                        registrationId = serialNumber,
                        provisioningPolicyResourceId = provisioningResourcePolicyFullyQualifiedName,
                    }
                };

                var deviceExtensionUri = $"eastus2euap.{arcDeviceUri}/providers/Private.BBeeSta1/DeviceProvisioningStates/default{provResourceApiVersion}";

                await MakeAzurePutCall(deviceExtensionUri, deviceExtensionResource, "Arc device extension");

                // add the resources in order of how they should be deleted
                azureResources.Add(deviceExtensionUri);
                azureResources.Add(arcDeviceResourceUri);

                // patch the order
                string edgeOrderPatchUri = $"management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EdgeOrder/orderItems/{serialNumber}?api-version=2023-05-01-preview";

                var patch = new EdgeOrderPatch()
                {
                    Properties = new EdgeOrderPatch.EdgeOrderPatchProperties()
                    {
                        ProvisioningDetails = new EdgeOrderPatch.EdgeOrderPatchProperties.EdgeOrderPatchProvisioningDetails()
                        {
                            SerialNumber = serialNumber,
                            ReadyToConnectArmId = provisioningResourcePolicyFullyQualifiedName,
                            ProvisioningArmId = provisioningResourceFullyQualifiedName,
                            ProvisioningEndpoint = $"{provResourceName}.eastus.test.edgeprov-dev.azure.net"
                        }
                    }
                };
                content = new StringContent(JsonConvert.SerializeObject(patch), Encoding.UTF8, "application/json");
                response = await client.PatchAsync($"https://{edgeOrderPatchUri}", content);
                content.Dispose();

                Console.WriteLine($"Patch order: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
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

        private async Task MakeAzurePutCall(string uri, object data, string description)
        {
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"https://{uri}", content);
            content.Dispose();

            Console.WriteLine($"{description}: {response.StatusCode}");
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            foreach (string resource in azureResources)
            {
                Console.WriteLine($"Going to delete {resource}");
                // cleanup
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
