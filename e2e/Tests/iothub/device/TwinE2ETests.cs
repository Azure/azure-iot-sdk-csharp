// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
using FluentAssertions.Specialized;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Twins
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class TwinE2eTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(TwinE2eTests)}_";

        private static readonly IotHubServiceClient s_serviceClient = TestDevice.ServiceClient;

        private static readonly List<object> s_listOfPropertyValues = new()
        {
            1,
            "someString",
            false,
            new CustomTwinProperty
            {
                Id = 123,
                Name = "someName",
            },
        };

        // ISO 8601 date-formatted string with trailing zeros in the microseconds portion.
        // This is to verify the Newtonsoft.Json known issue is worked around in the SDK.
        // See https://github.com/JamesNK/Newtonsoft.Json/issues/1511 for more details about the known issue.
        private const string DateTimeValue = "2023-01-31T10:37:08.4599400";

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Mqtt()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    new IotHubClientMqttSettings(),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Amqp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    new IotHubClientAmqpSettings(),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_MqttWs()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_AmqpWs()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceSetsInvalidReportedPropertyThrowsException_Amqp(IotHubClientTransportProtocol transportProtocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceSetsInvalidReportedPropertyThrowsExceptionAsync(
                    new IotHubClientAmqpSettings(transportProtocol),
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceSetsInvalidReportedPropertyThrowsException_Mqtt(IotHubClientTransportProtocol transportProtocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceSetsInvalidReportedPropertyThrowsExceptionAsync(
                    new IotHubClientMqttSettings(transportProtocol),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_Mqtt()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    new IotHubClientMqttSettings(),
                    Guid.NewGuid().ToString(),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_Amqp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    new IotHubClientAmqpSettings(),
                    Guid.NewGuid().ToString(),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Mqtt()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientMqttSettings(),
                    Guid.NewGuid().ToString(),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Amqp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientAmqpSettings(),
                    Guid.NewGuid().ToString(),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_Mqtt()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientMqttSettings(),
                    s_listOfPropertyValues,
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_Amqp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientAmqpSettings(),
                    s_listOfPropertyValues,
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_StringProperty_Mqtt()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    new IotHubClientMqttSettings(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_StringProperty_Amqp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    new IotHubClientAmqpSettings(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_DateTimeProperty_Mqtt()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    new IotHubClientMqttSettings(),
                    "Iso8601String",
                    DateTimeValue,
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_DateTimeProperty_Amqp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    new IotHubClientAmqpSettings(),
                    "Iso8601String",
                    DateTimeValue,
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Mqtt()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    new IotHubClientMqttSettings(),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Amqp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    new IotHubClientAmqpSettings(),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Mqtt()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    new IotHubClientMqttSettings(),
                    ct)
                .ConfigureAwait(false);
        }

        // This operation behaves the same irrespective of if the client is initialized over tcp or websocket.
        [TestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Amqp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    new IotHubClientAmqpSettings(),
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceSetsReportedPropertyAfterOpenCloseOpen_Mqtt(IotHubClientTransportProtocol transportProtocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceSetsReportedPropertyAfterOpenCloseOpenAsync(
                    new IotHubClientMqttSettings(transportProtocol),
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceSetsReportedPropertyAfterOpenCloseOpen_Amqp(IotHubClientTransportProtocol transportProtocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceSetsReportedPropertyAfterOpenCloseOpenAsync(
                    new IotHubClientAmqpSettings(transportProtocol),
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpen_Mqtt(IotHubClientTransportProtocol transportProtocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpenAsync(
                    new IotHubClientMqttSettings(transportProtocol),
                    s_listOfPropertyValues,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpen_Amqp(IotHubClientTransportProtocol transportProtocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpenAsync(
                    new IotHubClientAmqpSettings(transportProtocol),
                    s_listOfPropertyValues,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_ClientSetsReportedPropertyWithoutDesiredPropertyCallback(IotHubClientTransportProtocol transportProtocol)
        {
            // arrange

            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(transportProtocol));
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString(), ct).ConfigureAwait(false);

            int connectionStatusChangeCount = 0;
            void ConnectionStatusChangeHandler(ConnectionStatusInfo connInfo)
            {
                Interlocked.Increment(ref connectionStatusChangeCount);
            }
            deviceClient.ConnectionStatusChangeCallback = ConnectionStatusChangeHandler;

            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            // act
            await RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue, ct).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);

            // assert
            Assert.AreEqual(0, connectionStatusChangeCount, "AMQP should not be disconnected.");
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        [TestCategory("LongRunning")]
        public async Task Twin_Client_SetETag_Works(IotHubClientTransportProtocol transportProtocol)
        {
            // arrange

            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(transportProtocol));
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            ClientTwin twin = await s_serviceClient.Twins.GetAsync(testDevice.Id, ct).ConfigureAwait(false);
            ETag oldEtag = twin.ETag;

            twin.Properties.Desired[propName] = propValue;
            twin = await s_serviceClient.Twins.UpdateAsync(testDevice.Id, twin, true, ct).ConfigureAwait(false);

            twin.ETag = oldEtag;

            // set the 'onlyIfUnchanged' flag to true to check that, with an out of date ETag, the request throws a PreconditionFailedException.
            Func<Task> act = async () => { twin = await s_serviceClient.Twins.UpdateAsync(testDevice.Id, twin, true, ct).ConfigureAwait(false); };
            ExceptionAssertions<IotHubServiceException> error = await act.Should().ThrowAsync<IotHubServiceException>("Expected test to throw a precondition failed exception since it updated a twin with an out of date ETag");
            error.And.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.PreconditionFailed);
            error.And.IsTransient.Should().BeFalse();

            // set the 'onlyIfUnchanged' flag to false to check that, even with an out of date ETag, the request performs without exception.
            await FluentActions
                .Invoking(async () => { twin = await s_serviceClient.Twins.UpdateAsync(testDevice.Id, twin, false, ct).ConfigureAwait(false); })
                .Should()
                .NotThrowAsync<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to false");

            // set the 'onlyIfUnchanged' flag to true to check that, with an up-to-date ETag, the request performs without exception.
            twin.Properties.Desired[propName] = propValue + "1";
            twin.ETag = ETag.All;
            await FluentActions
                .Invoking(async () => { twin = await s_serviceClient.Twins.UpdateAsync(testDevice.Id, twin, true, ct).ConfigureAwait(false); })
                .Should()
                .NotThrowAsync<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to true");
        }

        public static async Task Twin_DeviceSetsReportedPropertyAndGetsItBackAsync<T>(IotHubDeviceClient deviceClient, string deviceId, T propValue, CancellationToken ct)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_DeviceSetsReportedPropertyAndGetsItBackAsync)}: name={propName}, value={propValue}");

            var props = new ReportedProperties
            {
                [propName] = propValue
            };

            await deviceClient.OpenAsync(ct).ConfigureAwait(false);
            long newTwinVersion = await deviceClient.UpdateReportedPropertiesAsync(props, ct).ConfigureAwait(false);

            // Validate the updated twin from the device-client
            TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync(ct).ConfigureAwait(false);
            bool propertyFound = deviceTwin.Reported.TryGetValue(propName, out T actual);
            propertyFound.Should().BeTrue();
            // We don't support nested deserialization yet, so we'll need to serialize the response and compare them.
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            ClientTwin completeTwin = await s_serviceClient.Twins.GetAsync(deviceId, ct).ConfigureAwait(false);
            object actualProp = completeTwin.Properties.Reported[propName];
            JsonConvert.SerializeObject(actualProp).Should().Be(JsonConvert.SerializeObject(propValue));
            completeTwin.Properties.Reported.Version.Should().Be(newTwinVersion);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString(), ct).ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, s_listOfPropertyValues, ct).ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsInvalidReportedPropertyThrowsExceptionAsync(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            string propName = Guid.NewGuid().ToString();
            var propValue = new Dictionary<string, object>
            {
                { "serialNumber", 5283920058631410000 },
            };

            VerboseTestLogger.WriteLine($"{nameof(Twin_DeviceSetsReportedPropertyAndGetsItBackAsync)}: name={propName}, value={propValue}");

            var props = new ReportedProperties
            {
                [propName] = propValue
            };

            await deviceClient.OpenAsync(ct).ConfigureAwait(false);
            Func<Task> actionAsync = async () => await deviceClient.UpdateReportedPropertiesAsync(props, ct).ConfigureAwait(false);
            await actionAsync
                .Should()
                .ThrowAsync<IotHubClientException>()
                .Where(ex => ex.ErrorCode == IotHubClientErrorCode.ArgumentInvalid);
        }

        private async Task Twin_DeviceSetsReportedPropertyAfterOpenCloseOpenAsync(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            // Close and re-open the client under test.
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);
            await deviceClient.CloseAsync(ct).ConfigureAwait(false);
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);

            // The client should still be able to send reported properties even though it was re-opened.
            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString(), ct).ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpenAsync<T>(
            IotHubClientTransportSettings transportSettings,
            T propValue,
            CancellationToken ct)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            // Close and re-open the client under test.
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);
            await deviceClient.CloseAsync(ct).ConfigureAwait(false);
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
            await testDeviceCallbackHandler.SetTwinPropertyUpdateCallbackHandlerAndProcessAsync<T>(ct).ConfigureAwait(false);

            testDeviceCallbackHandler.ExpectedTwinPatchKeyValuePair = new Tuple<string, object>(propName, propValue);
            Task updateReceivedTask = testDeviceCallbackHandler.WaitForTwinCallbackAsync(ct);

            // The client should still be able to receive desired properties even though it was re-opened.
            await Task
                .WhenAll(
                    RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue, ct),
                    updateReceivedTask)
                .ConfigureAwait(false);

            // Validate the updated twin from the device-client
            TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync(ct).ConfigureAwait(false);
            bool propertyFound = deviceTwin.Desired.TryGetValue(propName, out T actual);
            propertyFound.Should().BeTrue();
            // We don't support nested deserialization yet, so we'll need to serialize the response and compare them.
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            ClientTwin completeTwin = await s_serviceClient.Twins.GetAsync(testDevice.Id, ct).ConfigureAwait(false);
            dynamic actualProp = completeTwin.Properties.Desired[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actualProp), JsonConvert.SerializeObject(propValue));
        }

        public static async Task RegistryManagerUpdateDesiredPropertyAsync(string deviceId, string propName, object propValue, CancellationToken ct)
        {
            var twinPatch = new ClientTwin();
            twinPatch.Properties.Desired[propName] = propValue;

            await s_serviceClient.Twins.UpdateAsync(deviceId, twinPatch, cancellationToken: ct).ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(IotHubClientTransportSettings transportSettings, object propValue, CancellationToken ct)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            await TestDevice.OpenWithRetryAsync(deviceClient, ct).ConfigureAwait(false);

            // Set a callback
            await deviceClient.
                SetDesiredPropertyUpdateCallbackAsync(
                    (patch) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes)}: DesiredProperty: {patch}.");

                        // After unsubscribing it should never reach here
                        Assert.IsNull(patch);

                        return Task.FromResult(true);
                    },
                    ct)
                .ConfigureAwait(false);

            // Unsubscribe
            await deviceClient
                .SetDesiredPropertyUpdateCallbackAsync(null, ct)
                .ConfigureAwait(false);

            await RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue, ct)
                .ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync<T>(
            IotHubClientTransportSettings transportSettings,
            T propValue,
            CancellationToken ct)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);

            await testDeviceCallbackHandler.SetTwinPropertyUpdateCallbackHandlerAndProcessAsync<T>(ct).ConfigureAwait(false);
            testDeviceCallbackHandler.ExpectedTwinPatchKeyValuePair = new Tuple<string, object>(propName, propValue);
            Task updateReceivedTask = testDeviceCallbackHandler.WaitForTwinCallbackAsync(ct);

            await Task.WhenAll(
                RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue, ct),
                updateReceivedTask).ConfigureAwait(false);

            // Validate the updated twin from the device-client
            TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync(ct).ConfigureAwait(false);
            bool propertyFound = deviceTwin.Desired.TryGetValue(propName, out T actual);
            propertyFound.Should().BeTrue();
            // We don't support nested deserialization yet, so we'll need to serialize the response and compare them.
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            ClientTwin completeTwin = await s_serviceClient.Twins.GetAsync(testDevice.Id, ct).ConfigureAwait(false);
            object actualProp = completeTwin.Properties.Desired[propName];
            JsonConvert.SerializeObject(actualProp).Should().Be(JsonConvert.SerializeObject(propValue));
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(IotHubClientTransportSettings transportSettings, string propName, string propValue, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            var twinPatch = new ClientTwin();
            twinPatch.Properties.Desired[propName] = propValue;
            await s_serviceClient.Twins.UpdateAsync(testDevice.Id, twinPatch, cancellationToken: ct).ConfigureAwait(false);

            await TestDevice.OpenWithRetryAsync(deviceClient, ct).ConfigureAwait(false);

            TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync(ct).ConfigureAwait(false);
            bool propertyFound = deviceTwin.Desired.TryGetValue(propName, out string actual);
            propertyFound.Should().BeTrue();
            actual.Should().Be(propValue);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);

            var patch = new ReportedProperties
            {
                [propName] = propValue
            };
            await deviceClient.UpdateReportedPropertiesAsync(patch, ct).ConfigureAwait(false);

            await deviceClient.CloseAsync(ct).ConfigureAwait(false);

            ClientTwin serviceTwin = await s_serviceClient.Twins.GetAsync(testDevice.Id, ct).ConfigureAwait(false);
            Assert.AreEqual<string>(serviceTwin.Properties.Reported[propName].ToString(), propValue);

            VerboseTestLogger.WriteLine($"Verified {serviceTwin.Properties.Reported[propName]}={propValue}");
        }

        private async Task Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            string propName1 = Guid.NewGuid().ToString();
            string propName2 = Guid.NewGuid().ToString();

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);

            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);

            await deviceClient
                .UpdateReportedPropertiesAsync(
                    new ReportedProperties
                    {
                        [propName1] = null
                    },
                    ct)
                .ConfigureAwait(false);

            ClientTwin serviceTwin = await s_serviceClient.Twins.GetAsync(testDevice.Id, ct).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(propName1).Should().BeFalse();

            await deviceClient
                .UpdateReportedPropertiesAsync(
                    new ReportedProperties
                    {
                        [propName1] = new Dictionary<string, object>
                        {
                            [propName2] = null
                        }
                    },
                    ct)
                .ConfigureAwait(false);

            serviceTwin = await s_serviceClient.Twins.GetAsync(testDevice.Id, ct).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(propName1).Should().BeTrue();
            serviceTwin.Properties.Reported.TryGetValue(propName1, out Dictionary<string, object> value1).Should().BeTrue();
            value1.Count.Should().Be(0);
        }
    }

    internal class CustomTwinProperty
    {
        // The properties in here need to be public otherwise NewtonSoft.Json cannot serialize and deserialize them properly.
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
