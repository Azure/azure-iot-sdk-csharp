// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    using HttpTransportSettings = Microsoft.Azure.Devices.Provisioning.Service.HttpTransportSettings;

    [TestClass]
    [TestCategory("Provisioning-E2E")]
    public class ProvisioningServiceClientE2ETests : IDisposable
    {
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private const string RegistrationId = "e2etest-myvalid-registrationid-csharp";

        private readonly VerboseTestLogging _verboseLog = VerboseTestLogging.GetInstance();
        private readonly TestLogging _log = TestLogging.GetInstance();
        private readonly ConsoleEventListener _listener;

        public ProvisioningServiceClientE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Query_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Query_Ok(ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Create_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(ProxyServerAddress).ConfigureAwait(false);
        }

        private async Task ProvisioningServiceClient_IndividualEnrollments_Query_Ok(string proxyServerAddress)
        {
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningServiceWithProxy(proxyServerAddress);
            QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments");
            using (Query query = provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification))
            {
                while (query.HasNext())
                {
                    QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
                    Assert.AreEqual(queryResult.Type, QueryResultType.Enrollment);
                }
            }
        }

        private async Task ProvisioningServiceClient_IndividualEnrollments_Create_Ok(string proxyServerAddress)
        {
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningServiceWithProxy(proxyServerAddress);
            IndividualEnrollment individualEnrollment = await CreateIndividualEnrollment(provisioningServiceClient).ConfigureAwait(false);
            IndividualEnrollment individualEnrollmentResult = await provisioningServiceClient.GetIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);
            Assert.AreEqual(individualEnrollmentResult.ProvisioningStatus, ProvisioningStatus.Enabled);

            await provisioningServiceClient.DeleteIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);
        }

        private async Task<IndividualEnrollment> CreateIndividualEnrollment(ProvisioningServiceClient provisioningServiceClient)
        {
            var tpmSim = new SecurityProviderTpmSimulator(Configuration.Provisioning.TpmDeviceRegistrationId);
            string base64Ek = Convert.ToBase64String(tpmSim.GetEndorsementKey());
            var attestation = new TpmAttestation(base64Ek);
            IndividualEnrollment individualEnrollment =
                    new IndividualEnrollment(
                            RegistrationId,
                            attestation);

            IndividualEnrollment result = await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
            return result;
        }

        private ProvisioningServiceClient CreateProvisioningServiceWithProxy(string proxyServerAddress)
        {
            HttpTransportSettings transportSettings = new HttpTransportSettings();
            transportSettings.Proxy = new WebProxy(proxyServerAddress);

            return ProvisioningServiceClient.CreateFromConnectionString(Configuration.Provisioning.ConnectionString, transportSettings);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
