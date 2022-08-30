// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.iothub.service
{
    /// <summary>
    /// E2E test class for testing receiving file upload notifications.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class FileUploadNotificationE2eTest : E2EMsTestBase
    {
        private SemaphoreSlim _connectionLossSemaphore = new SemaphoreSlim(1, 1);

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Test()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            serviceClient.FileUploadNotificationProcessor.ErrorProcessor = OnConnectionLost;
            serviceClient.FileUploadNotificationProcessor.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;

            try
            {
                while (true)
                {
                    try
                    {
                        await serviceClient.FileUploadNotificationProcessor.OpenAsync().ConfigureAwait(false);
                        _connectionLossSemaphore.Wait();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            finally
            {
                await serviceClient.FileUploadNotificationProcessor.CloseAsync().ConfigureAwait(false);
                _connectionLossSemaphore.Dispose();
            }
        }

        private void OnConnectionLost(ErrorContext obj)
        {
            Console.WriteLine("Connection lost!");
            _connectionLossSemaphore.Release();
        }

        private AcknowledgementType OnFileUploadNotificationReceived(FileUploadNotification arg)
        {
            Console.WriteLine("Received a notification");
            return AcknowledgementType.Complete;
        }
    }
}
