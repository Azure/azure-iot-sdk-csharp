namespace Microsoft.Azure.Devices.Client.Test
{
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Devices.Client.Transport;
    
    [TestClass]
    public class DeviceClientConnectionStatusManagerTests
    {
        [TestMethod]
        public void TestNoConnection()
        {
            var deviceClientConnectionStatusManager = new Microsoft.Azure.Devices.Client.Transport.DeviceClientConnectionStatusManager();

            Assert.AreEqual(deviceClientConnectionStatusManager.State, ConnectionStatus.Disabled); 
        }
        
        [TestMethod]
        public void TestSingleConnectionStatusTransitions()
        {
            var deviceClientConnectionStatusManager = new DeviceClientConnectionStatusManager();
            ConnectionStatusChangeResult changeResult;

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Connected);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Connected);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Disconnected_Retrying);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Disconnected_Retrying);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Disconnected, ConnectionStatus.Connected);
            AssertConnectionStatusChangeResult(changeResult, false, false, ConnectionStatus.Disconnected_Retrying);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Disconnected);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Disconnected);
            
            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Disconnected_Retrying);
            AssertConnectionStatusChangeResult(changeResult, false, false, ConnectionStatus.Disconnected);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Disabled);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Disabled);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Disconnected_Retrying);
            AssertConnectionStatusChangeResult(changeResult, false, false, ConnectionStatus.Disabled);
        }

        static void AssertConnectionStatusChangeResult(ConnectionStatusChangeResult changeResult, bool isConnectionStatusChanged, bool isClientStatusChanged, ConnectionStatus clientStatus)
        {
            Assert.AreEqual(changeResult.IsConnectionStatusChanged, isConnectionStatusChanged);
            Assert.AreEqual(changeResult.IsClientStatusChanged, isClientStatusChanged);
            Assert.AreEqual(changeResult.ClientStatus, clientStatus);
        }

        [TestMethod]
        public void TestMultipleConnectionStatusTransitions()
        {
            var deviceClientConnectionStatusManager = new DeviceClientConnectionStatusManager();
            ConnectionStatusChangeResult changeResult;

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Connected);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Connected);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpMessaging, ConnectionStatus.Connected);
            AssertConnectionStatusChangeResult(changeResult, true, false, ConnectionStatus.Connected);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpMessaging, ConnectionStatus.Disconnected_Retrying);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Disconnected_Retrying);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Disconnected_Retrying);
            AssertConnectionStatusChangeResult(changeResult, true, false, ConnectionStatus.Disconnected_Retrying);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Connected);
            AssertConnectionStatusChangeResult(changeResult, true, false, ConnectionStatus.Disconnected_Retrying);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpMessaging, ConnectionStatus.Disconnected);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Disconnected);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Disconnected_Retrying);
            AssertConnectionStatusChangeResult(changeResult, true, false, ConnectionStatus.Disconnected);
            
            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpMessaging, ConnectionStatus.Disabled);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Disconnected_Retrying);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Disabled);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Disabled);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Connected);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Connected);
        }

        [TestMethod]
        public void TestCancelRetryingTaskWhenTransitToDisabled()
        {
            var deviceClientConnectionStatusManager = new DeviceClientConnectionStatusManager();
            ConnectionStatusChangeResult changeResult;

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Connected);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Connected);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Disconnected_Retrying);
            var cancellationTokenSource1 = changeResult.StatusChangeCancellationTokenSource;
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Disconnected_Retrying);

            Assert.IsFalse(cancellationTokenSource1.IsCancellationRequested);
            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Disabled);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Disabled);
            Assert.IsTrue(cancellationTokenSource1.IsCancellationRequested);
        }

        [TestMethod]
        public void TestCancelRetryingTaskWhenDisableMultipleConnections()
        {
            var deviceClientConnectionStatusManager = new DeviceClientConnectionStatusManager();
            ConnectionStatusChangeResult changeResult;

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Connected);
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Connected);

            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpMessaging, ConnectionStatus.Connected);
            AssertConnectionStatusChangeResult(changeResult, true, false, ConnectionStatus.Connected);

            //var cancellationTokenSource1 = new CancellationTokenSource();
            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpTelemetry, ConnectionStatus.Disconnected_Retrying);
            var cancellationTokenSource1 = changeResult.StatusChangeCancellationTokenSource;
            AssertConnectionStatusChangeResult(changeResult, true, true, ConnectionStatus.Disconnected_Retrying);

            //var cancellationTokenSource2 = new CancellationTokenSource();
            changeResult = deviceClientConnectionStatusManager.ChangeTo(ConnectionType.AmqpMessaging, ConnectionStatus.Disconnected_Retrying);
            var cancellationTokenSource2 = changeResult.StatusChangeCancellationTokenSource;
            AssertConnectionStatusChangeResult(changeResult, true, false, ConnectionStatus.Disconnected_Retrying);

            Assert.IsFalse(cancellationTokenSource1.IsCancellationRequested);
            Assert.IsFalse(cancellationTokenSource2.IsCancellationRequested);
            deviceClientConnectionStatusManager.DisableAllConnections();
            Assert.AreEqual(deviceClientConnectionStatusManager.State, ConnectionStatus.Disabled);
            Assert.IsTrue(cancellationTokenSource1.IsCancellationRequested);
            Assert.IsTrue(cancellationTokenSource2.IsCancellationRequested);
        }
    }
}