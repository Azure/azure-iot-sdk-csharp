// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("FaultInjection")]
    [TestCategory("IoTHub-Client")]
    public class FileUploadFaultInjectionTests : E2EMsTestBase
    {
        private const int FileSizeSmall = 10 * 1024;
        private readonly string _devicePrefix = $"{nameof(FileUploadFaultInjectionTests)}_";

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        public async Task FileUpload_GetFileUploadSasUri_ConnectionLossRecovery_Mqtt(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            await GetFileUploadSasUriRecoveryAsync(
                    new IotHubClientMqttSettings(protocol),
                    faultType,
                    faultReason,
                    cts.Token)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        public async Task FileUpload_GetFileUploadSasUri_ConnectionLossRecovery_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            await GetFileUploadSasUriRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    faultType,
                    faultReason,
                    cts.Token)
                .ConfigureAwait(false);
        }

        internal async Task GetFileUploadSasUriRecoveryAsync(
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            CancellationToken ct)
        {
            string smallFileBlobName = await FileUploadE2ETests.GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));

            Task fileUploadTask = deviceClient.GetFileUploadSasUriAsync(new FileUploadSasUriRequest(smallFileBlobName), ct);
            Task errorInjectionTask = SendErrorInjectionMessageAsync(deviceClient, faultType, reason, ct);
            await Task.WhenAll(fileUploadTask, errorInjectionTask).ConfigureAwait(false);

            try
            {
                await deviceClient.CloseAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                // catch and ignore exceptions resulted incase device client close failed while offline
            }
        }

        private static async Task SendErrorInjectionMessageAsync(
            IotHubDeviceClient deviceClient,
            string faultType,
            string reason,
            CancellationToken ct)
        {
            try
            {
                TelemetryMessage faultInjectionMessage = FaultInjection.ComposeErrorInjectionProperties(
                    faultType,
                    reason,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration);
                await deviceClient.SendTelemetryAsync(faultInjectionMessage, ct).ConfigureAwait(false);
            }
            catch
            {
                // catch and ignore exceptions resulted from error injection and continue to check result of the file upload status
            }
        }
    }
}
