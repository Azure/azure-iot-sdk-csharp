using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        IoTClient client;

        struct ProtocolNameToIdLookupEntry
        {
            public string Name;
            public TransportType Id;
        }

        List<ProtocolNameToIdLookupEntry> protocolLookup = new List<ProtocolNameToIdLookupEntry>()
        {
            new ProtocolNameToIdLookupEntry { Name = "HTTP", Id = TransportType.Http1 },
            new ProtocolNameToIdLookupEntry { Name = "AMQP", Id = TransportType.Amqp },
            new ProtocolNameToIdLookupEntry { Name = "MQTT", Id = TransportType.Mqtt },
            new ProtocolNameToIdLookupEntry { Name = "MQTT-TCP", Id = TransportType.Mqtt_Tcp_Only },
            new ProtocolNameToIdLookupEntry { Name = "MQTT-WebSockets", Id = TransportType.Mqtt_WebSocket_Only },
        };

        string GetProtocolNameFromId(TransportType protocolId)
        {
            return protocolLookup.Where(_ => _.Id == protocolId).First().Name;
        }

        TransportType GetProtocolIdFromName(string protocolName)
        {
            return protocolLookup.Where(_ => _.Name == protocolName).First().Id;
        }

        public MainPage()
        {
            this.InitializeComponent();

            var defaultProtocol = TransportType.Mqtt;

            this.client = new IoTClient(defaultProtocol, CallMeLogger, GetDeviceNameLogger, ErrorHandler);

            protocolLookup.ForEach(_ =>
            {
                this.protocolComboBox.Items.Add(_.Name);
            });

            this.protocolComboBox.SelectedIndex = protocolLookup.IndexOf(new ProtocolNameToIdLookupEntry { Id = defaultProtocol, Name = GetProtocolNameFromId(defaultProtocol) });

#pragma warning disable 4014
            client.Start();
#pragma warning restore 4014
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            string msg = this.messageText.Text;
            this.client.SendEvent(msg);
        }

        private async void receiveButton_Click(object sender, RoutedEventArgs e)
        {
            string msg = await this.client.ReceiveCommand();
            this.messageList.Items.Add(msg);
        }

        private async void OnProtocolSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TransportType protocol = GetProtocolIdFromName(this.protocolComboBox.SelectedValue.ToString());

            if (protocol != client.Protocol)
            {
                if (this.client != null)
                {
                    this.client.CloseAsync();
                }
                this.client = new IoTClient(protocol, CallMeLogger, GetDeviceNameLogger, ErrorHandler);
                this.client.Start();
            }
        }

        int callMeCounter = 0;
        int getDeviceNameCounter = 0;

        private void CallMeLogger(object element)
        {
            AddItemToListBox(methodCallList, string.Format("[{0}] {1}", callMeCounter++, element));
        }

        private void GetDeviceNameLogger(object element)
        {
            AddItemToListBox(getDeviceNameList, string.Format("[{0}] {1}", getDeviceNameCounter++, element));
        }

        private void ErrorHandler(object element)
        {
            AddItemToListBox(methodCallList, element.ToString());
            AddItemToListBox(getDeviceNameList, element.ToString());
        }

        private void AddItemToListBox(ListBox list, string item)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    list.Items.Add(item);

                    var selectedIndex = list.Items.Count - 1;
                    if (selectedIndex < 0)
                        return;

                    list.SelectedIndex = selectedIndex;
                    list.UpdateLayout();

                    list.ScrollIntoView(list.SelectedItem);
                });
        }
    }
}
