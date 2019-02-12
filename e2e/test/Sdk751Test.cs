using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.E2ETests;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using DC= Microsoft.Azure.Devices.Client;
using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTEdge-E2E")]
    public class Sdk751Test : IDisposable
    {
        string conn_string = "HostName=ebelouso-hub.azure-devices.net;DeviceId=donotdelete;SharedAccessKey=fvaeN3fjzLjobNXjOLWXJld4sdWAWHojeLkvaob3SRo=;GatewayHostName=127.0.0.1";

        ITransportSettings[] transportSettings = new ITransportSettings[]
        {
            new MqttTransportSettings(DC.TransportType.Mqtt_Tcp_Only)
            {
             RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true
            }
        };

        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener = TestConfig.StartEventListener();

        [Ignore]
        [TestMethod]
        public async Task EdgeHubRestart()
        { 
            var device = await TestDevice.GetTestDeviceAsync("edgetest", TestDeviceType.Sasl).ConfigureAwait(false);
            
            var client = device.CreateDeviceClient(DC.TransportType.Mqtt_Tcp_Only);

            try
            {
                DC.Message msg;
                for (int i = 0; i < 20; i++)
                {
                    var temp = DateTime.Now.ToString();
                    msg = new DC.Message(Encoding.UTF8.GetBytes(temp));
                    Console.Write(temp + " - ");
                    await client.SendEventAsync(msg).ConfigureAwait(false);
                    Console.WriteLine("Done");
                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _log.WriteLine(e.ToString());
                Assert.Fail();
            }
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
