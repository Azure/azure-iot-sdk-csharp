using System;
using System.Text;
using System.Threading;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using Microsoft.Azure.Devices.Client;

namespace XamarinSample
{
    public partial class MainPage : ContentPage
    {
        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        // Either set the IOTHUB_DEVICE_CONN_STRING environment variable or within launchSettings.json:
        private static string DeviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING_XAMARIN");

        private static int MESSAGE_COUNT = 5;
        private const int TEMPERATURE_THRESHOLD = 30;
        private static String deviceId = "MyXamarinDevice";
        private static float temperature;
        private static float humidity;
        private static Random rnd = new Random();
        private static TransportType s_transportType;
        private static DeviceClient s_deviceClient;

        public ObservableCollection<string> SentMessagesList = new ObservableCollection<string>();
        public ObservableCollection<string> ReceivedMessagesList = new ObservableCollection<string>();

        public MainPage()
        {
            InitializeComponent();
            deviceID.Text = deviceId;
            s_transportType = TransportType.Http1;
            s_deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, s_transportType);
        }

        private void Picker_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (Transport != null && Transport.SelectedIndex <= Transport.Items.Count)
            {
                var selecteditem = Transport.Items[Transport.SelectedIndex];
                switch (selecteditem)
                {
                    case "HTTP":
                        s_transportType = TransportType.Http1;
                        break;
                    case "MQTT":
                        s_transportType = TransportType.Mqtt;
                        break;
                    case "AMQP":
                        s_transportType = TransportType.Amqp;
                        break;
                }
                s_deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, s_transportType);
            }
        }

        async void SendEvent(object sender, EventArgs e)
        {
            string dataBuffer;

            Console.WriteLine("\n Device sending {0} messages to IoTHub...\n", MESSAGE_COUNT);
            SentMessagesList.Add(String.Format("Device sending {0} messages to IoTHub...\n", MESSAGE_COUNT));

            for (int count = 0; count < MESSAGE_COUNT; count++)
            {
                temperature = rnd.Next(20, 35);
                humidity = rnd.Next(60, 80);
                dataBuffer = string.Format("{{\"deviceId\":\"{0}\",\"messageId\":{1},\"temperature\":{2},\"humidity\":{3}}}", deviceId, count, temperature, humidity);
                Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                eventMessage.Properties.Add("temperatureAlert", (temperature > TEMPERATURE_THRESHOLD) ? "true" : "false");
                Console.WriteLine("\t{0}> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), count, dataBuffer);

                Device.BeginInvokeOnMainThread(() => {
                    SentMessagesList.Add(String.Format("> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), count, dataBuffer));
                    sentMessagesText.ItemsSource = SentMessagesList;
                });

                await s_deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);
            }
        }

        async void ReceiveCommands(object sender, EventArgs e)
        {
            Console.WriteLine("\nDevice waiting for commands from IoTHub...\n");
            ReceivedMessagesList.Add("Device waiting for commands from IoTHub...");
            Message receivedMessage;
            string messageData;

            while (true)
            {
                receivedMessage = await s_deviceClient.ReceiveAsync().ConfigureAwait(false);

                if (receivedMessage != null)
                {
                    messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Console.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);
                    Device.BeginInvokeOnMainThread(() => {
                        ReceivedMessagesList.Add(String.Format("Received message: {1}", DateTime.Now.ToLocalTime(), messageData));
                        receivedMessagesText.ItemsSource = ReceivedMessagesList;
                    });

                    int propCount = 0;
                    foreach (var prop in receivedMessage.Properties)
                    {
                        Console.WriteLine("\t\tProperty[{0}> Key={1} : Value={2}", propCount++, prop.Key, prop.Value);
                    }

                    await s_deviceClient.CompleteAsync(receivedMessage).ConfigureAwait(false);
                }

                //  Note: In this sample, the polling interval is set to 
                //  10 seconds to enable you to see messages as they are sent.
                //  To enable an IoT solution to scale, you should extend this //  interval. For example, to scale to 1 million devices, set 
                //  the polling interval to 25 minutes.
                //  For further information, see
                //  https://azure.microsoft.com/documentation/articles/iot-hub-devguide/#messaging
                Thread.Sleep(10000);
            }
        }
    }
}
