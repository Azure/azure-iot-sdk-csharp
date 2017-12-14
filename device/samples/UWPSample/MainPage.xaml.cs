﻿using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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

        private async void fileUploadButton_Click(object sender, RoutedEventArgs e)
        {

            Windows.Storage.Pickers.FileOpenPicker picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add("*");
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                try
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    fileUploadTime.Items.Clear();
                    fileUploadTime.Items.Add("Uploading " + "'" + file.Name + "'");

                    await this.client.UploadFile(file);

                    fileUploadTime.Items.Clear();
                    watch.Stop();
                    fileUploadTime.Items.Add("'" + file.Name + "'" + " uploaded in: "+ watch.ElapsedMilliseconds + "ms");
                }
                catch (Exception ex)
                {
                    fileUploadTime.Items.Clear();
                    fileUploadTime.Items.Add("Error occurred uploading the file. " + ex.Message);
                    Debug.WriteLine("{0}\n", ex.Message);
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine(ex.InnerException.Message + "\n");
                    }
                }
            }

        }
        private async void OnProtocolSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TransportType protocol = GetProtocolIdFromName(this.protocolComboBox.SelectedValue.ToString());

            if (protocol != client.Protocol)
            {
                if (this.client != null)
                {
                    try
                    {
                        await this.client.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        var err = string.Format("Error {0} while closing the client", ex.Message);
                        Debug.WriteLine(err);
                    }
                }
                this.client = new IoTClient(protocol, CallMeLogger, GetDeviceNameLogger, ErrorHandler);
                await this.client.Start();
            }
        }

        int callMeCounter = 0;
        int getDeviceNameCounter = 0;

        private async void CallMeLogger(object element)
        {
            await AddItemToListBox(methodCallList, string.Format("[{0}] {1}", callMeCounter++, element));
        }

        private async void GetDeviceNameLogger(object element)
        {
            await AddItemToListBox(getDeviceNameList, string.Format("[{0}] {1}", getDeviceNameCounter++, element));
        }

        private async void ErrorHandler(object element)
        {
            await AddItemToListBox(methodCallList, element.ToString());
            await AddItemToListBox(getDeviceNameList, element.ToString());
        }

        private async Task AddItemToListBox(ListBox list, string item)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
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
