// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Provisioning.Service.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("Provisioning-E2E")]
    public class ProvisioningServiceClientTests : IDisposable
    {
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private const string RegistrationId = "e2etest-myvalid-registrationid-csharp";
        private const string StatusEnabled = "enabled";
        private const string TpmAttestationType = "tpm";
        private DeviceCapabilities OptionalEdgeCapabilityEnabled = new DeviceCapabilities { IotEdge = true };
        private DeviceCapabilities OptionalEdgeCapabilityDisabled = new DeviceCapabilities { IotEdge = false };

        private readonly VerboseTestLogging _verboseLog = VerboseTestLogging.GetInstance();
        private readonly TestLogging _log = TestLogging.GetInstance();
        private readonly ConsoleEventListener _listener;

        public ProvisioningServiceClientTests()
        {
            _listener = new ConsoleEventListener("Microsoft-Azure-");
        }

        [TestMethod]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Query_Ok()
        {
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningServiceClient();
            await ProvisioningServiceClient_IndividualEnrollments_Query_Ok(provisioningServiceClient).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Create_Ok()
        {
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningServiceClient();
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(provisioningServiceClient).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Update_Ok()
        {
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningServiceClient();
            await ProvisioningServiceClient_IndividualEnrollments_Update_Ok(provisioningServiceClient).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Delete_Ok()
        {
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningServiceClient();
            await ProvisioningServiceClient_IndividualEnrollments_Delete_Ok(provisioningServiceClient).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Query_HttpWithProxy_Ok()
        {
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningServiceClientWithHttpClientHandler(ProxyServerAddress);
            await ProvisioningServiceClient_IndividualEnrollments_Query_Ok(provisioningServiceClient).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Create_HttpWithProxy_Ok()
        {
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningServiceClientWithHttpClientHandler(ProxyServerAddress);
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(provisioningServiceClient).ConfigureAwait(false);
        }

        private async Task ProvisioningServiceClient_IndividualEnrollments_Query_Ok(ProvisioningServiceClient provisioningServiceClient)
        {
            QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments");
            IList<IndividualEnrollment> queryResult = await provisioningServiceClient.QueryIndividualEnrollmentsAsync(querySpecification).ConfigureAwait(false);
            foreach (IndividualEnrollment individualEnrollment in queryResult)
            {
                Assert.IsNotNull(individualEnrollment);
            }
        }

        private async Task ProvisioningServiceClient_IndividualEnrollments_Create_Ok(ProvisioningServiceClient provisioningServiceClient)
        {
            IndividualEnrollment individualEnrollment = await CreateIndividualEnrollment(provisioningServiceClient).ConfigureAwait(false);
            IndividualEnrollment individualEnrollmentResult = await provisioningServiceClient.GetIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);
            Assert.AreEqual(individualEnrollmentResult.ProvisioningStatus, StatusEnabled);

            await provisioningServiceClient.DeleteIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);
        }

        private async Task ProvisioningServiceClient_IndividualEnrollments_Update_Ok(ProvisioningServiceClient provisioningServiceClient)
        {
            IndividualEnrollment individualEnrollment = await CreateIndividualEnrollment(provisioningServiceClient).ConfigureAwait(false);
            individualEnrollment.Capabilities = OptionalEdgeCapabilityDisabled;

            IndividualEnrollment individualEnrollmentUpdateResult = await provisioningServiceClient.GetIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);
            Assert.AreEqual(individualEnrollmentUpdateResult.Capabilities, OptionalEdgeCapabilityDisabled);

            await provisioningServiceClient.DeleteIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);
        }

        private async Task ProvisioningServiceClient_IndividualEnrollments_Delete_Ok(ProvisioningServiceClient provisioningServiceClient)
        {
            IndividualEnrollment individualEnrollment = await CreateIndividualEnrollment(provisioningServiceClient).ConfigureAwait(false);
            await provisioningServiceClient.DeleteIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);

            // TODO: assert
            //Assert.AreEqual(individualEnrollmentUpdateResult.Capabilities, OptionalEdgeCapabilityDisabled);

        }

        private async Task<IndividualEnrollment> CreateIndividualEnrollment(ProvisioningServiceClient provisioningServiceClient)
        {
            var tpmSim = new SecurityProviderTpmSimulator(Configuration.Provisioning.TpmDeviceRegistrationId);
            string base64Ek = Convert.ToBase64String(tpmSim.GetEndorsementKey());
            var tpmAttestation = new TpmAttestation(base64Ek);
            AttestationMechanism attestationMechanism = new AttestationMechanism(TpmAttestationType, tpmAttestation);
            IndividualEnrollment individualEnrollment =
                    new IndividualEnrollment(
                            RegistrationId,
                            attestationMechanism);
            individualEnrollment.Capabilities = OptionalEdgeCapabilityEnabled;

            IndividualEnrollment result = await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(RegistrationId, individualEnrollment).ConfigureAwait(false);
            return result;
        }

        private ProvisioningServiceClient CreateProvisioningServiceClientWithHttpClientHandler(string proxyServerAddress)
        {
            var credentials = ProvisioningServiceClient.CreateCredentialsFromConnectionString(Configuration.Provisioning.ConnectionString);
            var dpsUri = new Uri(Configuration.Provisioning.Host);

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.Proxy = new WebProxy(proxyServerAddress);

            return new ProvisioningServiceClient(dpsUri, credentials, httpClientHandler);
        }

        private ProvisioningServiceClient CreateProvisioningServiceClient()
        {
            var credentials = ProvisioningServiceClient.CreateCredentialsFromConnectionString(Configuration.Provisioning.ConnectionString);
            var dpsUri = new Uri(Configuration.Provisioning.Host);

            return new ProvisioningServiceClient(dpsUri, credentials);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listener.Dispose();
            }
        }
    }
}
