namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;

    [TestClass]
    public class IoTHubClientDiagnosticTest
    {
        static string fakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";

        [TestMethod]
        [TestCategory("IoTHubClientDiagnostic")]
        public void IoTHubClientDiagnostic_AddDiagnosticInfoIfNecessary_Test()
        {
            Message message = CreateMessage();
            const int DiagPercentageWithoutDiagnostic = 0;
            const int DiagPercentageWithDiagnostic = 100;

            int messageCount = 0;
            IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, DiagPercentageWithoutDiagnostic, ref messageCount);

            Assert.IsFalse(message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagId));
            Assert.IsFalse(message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagCorrelationContext));

            message = CreateMessage();
            messageCount = 0;
            IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, DiagPercentageWithDiagnostic, ref messageCount);
            Assert.IsTrue(message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagId));
            Assert.IsTrue(message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagCorrelationContext));
        }

        [TestMethod]
        [TestCategory("IoTHubClientDiagnostic")]
        public void IoTHubClientDiagnostic_CopyDiagnosticPropertiesToAmqpAnnotations_Test()
        {
            Message message = CreateMessage();
            const int DiagPercentageWithDiagnostic = 100;
            int messageCount = 0;
            IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, DiagPercentageWithDiagnostic, ref messageCount);
            AmqpMessage amqpMessage = message.ToAmqpMessage();

            Assert.IsTrue(message.SystemProperties[MessageSystemPropertyNames.DiagId] == amqpMessage.MessageAnnotations.Map["Diagnostic-Id"]);
            Assert.IsTrue(message.SystemProperties[MessageSystemPropertyNames.DiagCorrelationContext] == amqpMessage.MessageAnnotations.Map["Correlation-Context"]);
        }

        [TestMethod]
        [TestCategory("IoTHubClientDiagnostic")]
        public void IoTHubClientDiagnostic_DiagId_Test()
        {
            Message message = CreateMessage();
            const int DiagPercentageWithDiagnostic = 100;
            int messageCount = 0;
            IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, DiagPercentageWithDiagnostic, ref messageCount);
            string diagId = message.SystemProperties[MessageSystemPropertyNames.DiagId].ToString();
            var r = new Regex("^[a-zA-Z0-9]{8}$");
            Assert.IsTrue(r.IsMatch(diagId));
        }

        [TestMethod]
        [TestCategory("IoTHubClientDiagnostic")]
        public void IoTHubClientDiagnostic_CorrelationContext_Test()
        {
            Message message = CreateMessage();
            const int DiagPercentageWithDiagnostic = 100;
            int messageCount = 0;
            IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, DiagPercentageWithDiagnostic, ref messageCount);

            string diagctx = message.SystemProperties[MessageSystemPropertyNames.DiagCorrelationContext].ToString();

            var r = new Regex("^creationtimeutc=\\d*\\.\\d*$");
            Assert.IsTrue(r.IsMatch(diagctx));

            NameValueCollection properties = HttpUtility.ParseQueryString(diagctx.Replace(",", "&"));
            string second = properties["creationtimeutc"];
            double epochTime = double.Parse(second, CultureInfo.InvariantCulture);
            DateTime creationTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochTime);

            Assert.IsTrue((DateTime.UtcNow - creationTime).TotalSeconds < 60);
        }

        [TestMethod]
        [TestCategory("IoTHubClientDiagnostic")]
        public void IoTHubClientDiagnostic_SamplingPercentage_Test()
        {
            int percentage = 0;
            int count = 0;
            int messageCount = 0;
            for (int i = 1; i <= 100; i++)
            {
                Message message = CreateMessage();
                IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, percentage, ref messageCount);
                if (IoTHubClientDiagnostic.HasDiagnosticProperties(message))
                {
                    count++;
                }
            }
            Assert.AreEqual(count, 0);

            count = 0;
            percentage = 10;
            messageCount = 0;
            for (int i = 1; i <= 100; i++)
            {
                Message message = CreateMessage();
                IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, percentage, ref messageCount);
                if (IoTHubClientDiagnostic.HasDiagnosticProperties(message))
                {
                    Assert.IsTrue(i % 10 == 1);
                    count++;
                }
            }
            Assert.AreEqual(count, 10);

            count = 0;
            percentage = 20;
            messageCount = 0;
            for (int i = 1; i <= 100; i++)
            {
                Message message = CreateMessage();
                IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, percentage, ref messageCount);
                if (IoTHubClientDiagnostic.HasDiagnosticProperties(message))
                {
                    Assert.IsTrue(i % 5 == 1);
                    count++;
                }
            }
            Assert.AreEqual(count, 20);

            count = 0;
            percentage = 50;
            messageCount = 0;
            for (int i = 1; i <= 100; i++)
            {
                Message message = CreateMessage();
                IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, percentage, ref messageCount);
                if (IoTHubClientDiagnostic.HasDiagnosticProperties(message))
                {
                    Assert.IsTrue(i % 2 == 1);
                    count++;
                }
            }
            Assert.AreEqual(count, 50);

            count = 0;
            percentage = 70;
            messageCount = 0;
            for (int i = 1; i <= 100; i++)
            {
                Message message = CreateMessage();
                IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, percentage, ref messageCount);
                if (IoTHubClientDiagnostic.HasDiagnosticProperties(message))
                {
                    count++;
                }
            }
            Assert.AreEqual(count, 70);

            count = 0;
            percentage = 100;
            messageCount = 0;
            for (int i = 1; i <= 100; i++)
            {
                Message message = CreateMessage();
                IoTHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, percentage, ref messageCount);
                if (IoTHubClientDiagnostic.HasDiagnosticProperties(message))
                {
                    count++;
                }
            }
            Assert.AreEqual(count, 100);
        }

        [TestMethod]
        [TestCategory("IoTHubClientDiagnostic")]
        public void IoTHubClientDiagnostic_InitialSuccess_Test()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var diag = new IoTHubClientDiagnostic(deviceClient);
            Assert.IsNotNull(diag.deviceClient);
            Assert.AreEqual(diag.currentSamplingRate, 0);
        }

        [TestMethod]
        [TestCategory("IoTHubClientDiagnostic")]
        public async Task IoTHubClientDiagnostic_GetDiagTwinSettingsWhenStart_Test()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var diag = new IoTHubClientDiagnostic(deviceClient);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            diag.deviceClient.InnerHandler = innerHandler;
            var twin = new Twin();
            var twinCollection = new TwinCollection();
            twinCollection[IoTHubClientDiagnostic.TwinDiagSamplingRateKey] = 50;
            twin.Properties.Desired = twinCollection;

            innerHandler.SendTwinGetAsync(Arg.Any<CancellationToken>()).Returns(twin);
            await diag.StartListeningE2EDiagnosticSettingChanges();

            Assert.AreEqual(diag.currentSamplingRate, 50);
        }

        [TestMethod]
        [TestCategory("IoTHubClientDiagnostic")]
        public async Task IoTHubClientDiagnostic_GetDiagTwinSettingsFromServer_Test()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            await deviceClient.EnableE2EDiagnosticWithCloudSetting();

            int samplingRate1 = 50;
            Twin twin = GenerateDiagTwin(samplingRate1);

            deviceClient.OnReportedStatePatchReceived(twin.Properties.Desired);
            Assert.AreEqual(deviceClient.Diagnostic.currentSamplingRate, samplingRate1);

            int samplingRate2 = 30;
            twin = GenerateDiagTwin(samplingRate2);
            deviceClient.OnReportedStatePatchReceived(twin.Properties.Desired);
            Assert.AreEqual(deviceClient.Diagnostic.currentSamplingRate, samplingRate2);

            int samplingRate3 = 101;
            twin = GenerateDiagTwin(samplingRate3);
            deviceClient.OnReportedStatePatchReceived(twin.Properties.Desired);
            Assert.AreEqual(deviceClient.Diagnostic.currentSamplingRate, samplingRate2);

            int? samplingRate4 = null;
            twin = GenerateDiagTwin(samplingRate4);
            deviceClient.OnReportedStatePatchReceived(twin.Properties.Desired);
            Assert.AreEqual(deviceClient.Diagnostic.currentSamplingRate, 0);

            string samplingRate5 = "Not a valid twin settings";
            twin = GenerateDiagTwin(samplingRate5);
            deviceClient.OnReportedStatePatchReceived(twin.Properties.Desired);
            Assert.AreEqual(deviceClient.Diagnostic.currentSamplingRate, 0);

            twin = GenerateInvalidDiagTwin();
            deviceClient.OnReportedStatePatchReceived(twin.Properties.Desired);
            Assert.AreEqual(deviceClient.Diagnostic.currentSamplingRate, 0);
        }

        [TestMethod]
        [TestCategory("IoTHubClientDiagnostic")]
        // Tests_SRS_IoTHubClientDiagnostic_01_01: [ If diagnostic settings from server is not correct, the sampling percentage wiil be reset to 0. ]
        public async Task IoTHubClientDiagnostic_GetWrongDiagTwinSettingsWhenStart_Test()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var diag = new IoTHubClientDiagnostic(deviceClient);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            diag.deviceClient.InnerHandler = innerHandler;

            Twin twin1 = GenerateDiagTwin(200);
            innerHandler.SendTwinGetAsync(Arg.Any<CancellationToken>()).Returns(twin1);
            await diag.StartListeningE2EDiagnosticSettingChanges();
            await innerHandler.Received().SendTwinPatchAsync(Arg.Is<TwinCollection>(collection => collection.Contains(IoTHubClientDiagnostic.TwinDiagErrorKey)), Arg.Any<CancellationToken>());
            Assert.AreEqual(diag.currentSamplingRate, 0);

            Twin twin2 = GenerateDiagTwin(null);
            innerHandler.SendTwinGetAsync(Arg.Any<CancellationToken>()).Returns(twin2);
            await diag.StartListeningE2EDiagnosticSettingChanges();
            await innerHandler.Received().SendTwinPatchAsync(Arg.Is<TwinCollection>(collection => collection.Contains(IoTHubClientDiagnostic.TwinDiagErrorKey)), Arg.Any<CancellationToken>());
            Assert.AreEqual(diag.currentSamplingRate, 0);

            Twin twin3 = GenerateInvalidDiagTwin();
            innerHandler.SendTwinGetAsync(Arg.Any<CancellationToken>()).Returns(twin3);
            await diag.StartListeningE2EDiagnosticSettingChanges();
            await innerHandler.Received().SendTwinPatchAsync(Arg.Is<TwinCollection>(collection => collection.Contains(IoTHubClientDiagnostic.TwinDiagErrorKey)), Arg.Any<CancellationToken>());
            Assert.AreEqual(diag.currentSamplingRate, 0);
        }

        private static Message CreateMessage()
        {
            const string MessageBody = "My Message";
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(MessageBody));
            var message = new Message(memoryStream);
            return message;
        }

        private static Twin GenerateDiagTwin(dynamic samplingRate)
        {
            var twin = new Twin();
            var twinCollection = new TwinCollection();
            twinCollection[IoTHubClientDiagnostic.TwinDiagSamplingRateKey] = samplingRate;
            twin.Properties.Desired = twinCollection;
            return twin;
        }

        private static Twin GenerateInvalidDiagTwin()
        {
            var twin = new Twin();
            var twinCollection = new TwinCollection();
            twinCollection["OtherProperty"] = "valude";
            twin.Properties.Desired = twinCollection;
            return twin;
        }
    }
}
