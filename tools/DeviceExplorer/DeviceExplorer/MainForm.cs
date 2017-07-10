﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Security;
using Microsoft.ServiceBus.Messaging;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Devices.Client;

namespace DeviceExplorer
{
    /// <summary>
    /// Provides the following functionalities:
    /// - CRUD operations for devices
    /// - Receiving Events from a device
    /// - Sending Messages to a device
    /// </summary>
    public partial class MainForm : Form
    {
        #region fields
        private const int MAX_TTL_VALUE = 365;
        private const int MAX_COUNT_OF_DEVICES = 1000;

        private bool devicesListed = false;
        private static int eventHubPartitionsCount;
        private static string activeIoTHubConnectionString;
        private string iotHubHostName;

        private static string cloudToDeviceMessage;

        private static int deviceSelectedIndexForEvent = 0;
        private static int deviceSelectedIndexForC2DMessage = 0;
        private static int deviceSelectedIndexForDeviceMethod = 0;
        private static int deviceSelectedIndexForCommandReceiver = 0;
        private static int actionSelectedIndexForCommandReceiver = 0;

        private static CancellationTokenSource ctsForDataMonitoring;
        private static CancellationTokenSource ctsForFeedbackMonitoring;
        private static CancellationTokenSource ctsForDeviceMethod;
        private static CancellationTokenSource ctsForReceiveCommand;

        private const string DEFAULT_CONSUMER_GROUP = "$Default";
        #endregion

        public MainForm()
        {
            InitializeComponent();

            // Extract settings from local config file
            try
            {
                Properties.Settings.Default.Reload();
                dhConStringTextBox.Text = (string)Properties.Settings.Default["Microsoft_IoTHub_ConnectionString"];
            }
            catch
            {
                dhConStringTextBox.Text = "";
            }
            try
            {
                protocolGatewayHost.Text = (string)Properties.Settings.Default["Microsoft_Protocol_Gateway_Hostname"];
            }
            catch
            {
                protocolGatewayHost.Text = string.Empty;
            }
            // Initialize fields
            activeIoTHubConnectionString = dhConStringTextBox.Text;
            dateTimePicker.Value = DateTime.Now;
            groupNameTextBox.Text = DEFAULT_CONSUMER_GROUP;

            numericUpDownTTL.Maximum = MAX_TTL_VALUE;
            numericUpDownTTL.Value = MAX_TTL_VALUE;
            cancelMonitoringButton.Enabled = false;

            // Set up the DataGridView.
            devicesGridView.MultiSelect = false;
            devicesGridView.ScrollBars = ScrollBars.Both;

            updateDeviceButton.Enabled = false;
            deleteDeviceButton.Enabled = false;
            sasTokenButton.Enabled = false;
        }

        /// <summary>
        /// Invoked whenever the form is first shown.
        /// Method will attempt to apply the settings retrieved from App.config
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Shown(object sender, EventArgs e)
        {
            try
            {
                //Setting the second optional parameter to false will prevent the parsing functions from processing empty strings
                parseIoTHubConnectionString(dhConStringTextBox.Text, true);
            }
            catch (Exception ex)
            {
                //Exception will only be thrown on startup if the app settings contain invalid connection strings. No exception will be thrown if app settings contain empty connection strings
                using (new CenterDialog(this))
                {
                    MessageBox.Show(
                        "Device Explorer does not yet contain valid connection strings.\nPlease provide valid connection strings then hit [Update].", $"Attention: {ex.Message}", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        #region Helper Methods

        private void parseIoTHubConnectionString(string connectionString, bool skipException = false)
        {
            try
            {
                var builder = Microsoft.Azure.Devices.IotHubConnectionStringBuilder.Create(connectionString);
                iotHubHostName = builder.HostName;

                targetTextBox.Text = builder.HostName;
                keyValueTextBox.Text = builder.SharedAccessKey;
                keyNameTextBox.Text = builder.SharedAccessKeyName;

                string iotHubName = builder.HostName.Split('.')[0];
                iotHubNameTextBox.Text = iotHubName;
                eventHubNameTextBoxForDataTab.Text = iotHubName;
                iotHubNameTextBoxForDeviceMethod.Text = iotHubName;

                activeIoTHubConnectionString = connectionString;
            }
            catch (Exception ex)
            {
                if (!skipException)
                {
                    throw new ArgumentException("Invalid IoTHub connection string. " + ex.Message);
                }
            }
        }

        private void fillCommandReceiverActionComboBox(bool runIfNullOrEmpty = true)
        {
            if (actionComboBoxForCommandReceiver.Items.Count == 0 || runIfNullOrEmpty)
            {
                actionComboBoxForCommandReceiver.Items.Add("None");
                actionComboBoxForCommandReceiver.Items.Add("Complete");
                actionComboBoxForCommandReceiver.Items.Add("Abondon");

                actionComboBoxForCommandReceiver.SelectedIndex = actionSelectedIndexForCommandReceiver;
            }
        }

        private async Task updateDeviceIdsComboBoxes(bool runIfNullOrEmpty = true)
        {
            if (!String.IsNullOrEmpty(activeIoTHubConnectionString) || runIfNullOrEmpty)
            {
                List<string> deviceIdsForEvent = new List<string>();
                List<string> deviceIdsForC2DMessage = new List<string>();
                List<string> deviceIdsForDeviceMethod = new List<string>();
                List<string> deviceIdsForDeviceEmulation = new List<string>();
                RegistryManager registryManager = RegistryManager.CreateFromConnectionString(activeIoTHubConnectionString);

                var devices = await registryManager.GetDevicesAsync(MAX_COUNT_OF_DEVICES);

                var orderedDeviceIds = devices.Select(d => d.Id).OrderBy(c => c);

                deviceIdsForEvent.AddRange(orderedDeviceIds);
                deviceIdsForC2DMessage.AddRange(orderedDeviceIds);
                deviceIdsForDeviceMethod.AddRange(orderedDeviceIds);
                deviceIdsForDeviceEmulation.AddRange(orderedDeviceIds);

                await registryManager.CloseAsync();
                deviceIDsComboBoxForEvent.DataSource = deviceIdsForEvent;
                deviceIDsComboBoxForCloudToDeviceMessage.DataSource = deviceIdsForC2DMessage;
                deviceIDsComboBoxForDeviceMethod.DataSource = deviceIdsForDeviceMethod;
                deviceIDComboBoxForCommandReceiver.DataSource = deviceIdsForDeviceEmulation;

                deviceIDsComboBoxForEvent.SelectedIndex = deviceSelectedIndexForEvent;
                deviceIDsComboBoxForCloudToDeviceMessage.SelectedIndex = deviceSelectedIndexForC2DMessage;
                deviceIDsComboBoxForDeviceMethod.SelectedIndex = deviceSelectedIndexForDeviceMethod;
                deviceIDComboBoxForCommandReceiver.SelectedIndex = deviceSelectedIndexForCommandReceiver;
            }
        }
        private void persistSettingsToAppConfig()
        {
            var dhSetting = dhConStringTextBox.Text;
            var pgHostnameSetting = protocolGatewayHost.Text;

            Properties.Settings.Default["Microsoft_IoTHub_ConnectionString"] = dhSetting;
            Properties.Settings.Default["Microsoft_Protocol_Gateway_Hostname"] = pgHostnameSetting;

            Properties.Settings.Default.Save();
        }

        private string sanitizeConnectionString(string connectionString)
        {
            // Does the following:
            //  - trim leading/trailing white space from the connection string
            //  - scan and remove CR and LF characters
            return connectionString.Trim().Replace("\r", "").Replace("\n", "");
        }

        private bool isInJsonFormat(string str)
        {
            str = str.Trim();
            if ((str.StartsWith("{") && str.EndsWith("}")) || //For object
                 (str.StartsWith("[") && str.EndsWith("]"))) //For array
            {
                try
                {
                    JToken t = JToken.Parse(str);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    MessageBox.Show($"JSON string with invalid format.\n[{jex.Message}]", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region ConfigurationsTab

        private void updateSettingsButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Clear readonly text box controls that rely on the new settings.
                keyNameTextBox.Text = String.Empty;
                keyValueTextBox.Text = String.Empty;
                targetTextBox.Text = String.Empty;
                eventHubNameTextBoxForDataTab.Text = String.Empty;
                iotHubNameTextBox.Text = String.Empty;

                // scrub the connection string
                dhConStringTextBox.Text = sanitizeConnectionString(dhConStringTextBox.Text);

                // Attempt to apply the new settings
                parseIoTHubConnectionString(dhConStringTextBox.Text);

                // Persist the new settings to the App.config
                persistSettingsToAppConfig();

                // Referesh the device grid if devices were arleady listed:
                if (devicesListed)
                {
                    UpdateListOfDevices();
                }

                using (new CenterDialog(this))
                {
                    MessageBox.Show("Settings updated successfully", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                //Identify the possible methods from which the exception could have originated in order to highlight the text in the correct box.
                //Doing this since exceptions caught here are only of type ArgumentException
                System.Reflection.MethodBase iotmb = typeof(MainForm).GetMethod("parseIoTHubConnectionString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                using (new CenterDialog(this))
                {
                    var invalidConnectionStringDialogResult = MessageBox.Show("Unable to update settings.\nPlease ensure that valid connection strings have been provided then hit [Update].\nPress OK to modify the current invalid connection strings, or Cancel to revert to the most recent working connection strings.\n\n" +
                            $"Error: {ex.Message}", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);

                    //Only revert to the previous connection strings if the user chooses not edit the one they were attempting to use
                    if (invalidConnectionStringDialogResult == DialogResult.Cancel)
                    {
                        dhConStringTextBox.Text = activeIoTHubConnectionString;
                    }
                }

                //Determine the origin of the exception (parseIoTHubConnectionString or parseEventHubConnectionString) and highlight the corresponding textbox
                if (ex.TargetSite.Equals(iotmb))
                {
                    dhConStringTextBox.Focus();
                    dhConStringTextBox.SelectAll();
                }
            }
        }

        private void generateSASButton_Click(object sender, EventArgs e)
        {
            try
            {
                var builder = new SharedAccessSignatureBuilder()
                {
                    KeyName = keyNameTextBox.Text,
                    Key = keyValueTextBox.Text,
                    Target = targetTextBox.Text,
                    TimeToLive = TimeSpan.FromDays(Convert.ToDouble(numericUpDownTTL.Value))
                }.ToSignature();

                sasRichTextBox.Text = builder + "\r\n";
            }
            catch (Exception ex)
            {
                using (new CenterDialog(this))
                {
                    MessageBox.Show($"Unable to generate SAS. Verify connection strings.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        #region ManagementTab
        private async Task updateDevicesGridView()
        {
            var devicesProcessor = new DevicesProcessor(activeIoTHubConnectionString, MAX_COUNT_OF_DEVICES, protocolGatewayHost.Text);
            var devicesList = await devicesProcessor.GetDevices();
            devicesList.Sort();
            var sortableDevicesBindingList = new SortableBindingList<DeviceEntity>(devicesList);

            devicesGridView.DataSource = sortableDevicesBindingList;
            devicesGridView.ReadOnly = true;
            devicesGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            if (devicesList.Count() > MAX_COUNT_OF_DEVICES)
            {
                deviceCountLabel.Text = MAX_COUNT_OF_DEVICES + "+";
            }
            else
            {
                deviceCountLabel.Text = devicesList.Count().ToString();
            }
        }

        private void createButton_Click(object sender, EventArgs e)
        {
            try
            {
                CreateDeviceForm createForm = new CreateDeviceForm(activeIoTHubConnectionString, MAX_COUNT_OF_DEVICES);
                createForm.ShowDialog();    // Modal window
                UpdateListOfDevices();
            }
            catch (Exception ex)
            {
                using (new CenterDialog(this))
                {
                    MessageBox.Show($"Unable to create new device. Please verify your connection strings.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void listButton_Click(object sender, EventArgs e)
        {
            UpdateListOfDevices();
        }

        private async void UpdateListOfDevices()
        {
            try
            {
                await updateDevicesGridView();
                devicesListed = true;
                listDevicesButton.Text = "Refresh";
            }
            catch (Exception ex)
            {
                listDevicesButton.Text = "List";
                devicesListed = false;
                using (new CenterDialog(this))
                {
                    MessageBox.Show($"Unable to retrieve list of devices. Please verify your connection strings.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void updateDeviceButton_Click(object sender, EventArgs e)
        {
            try
            {
                string selectedDeviceId = devicesGridView.CurrentRow.Cells[0].Value.ToString();
                RegistryManager registryManager = RegistryManager.CreateFromConnectionString(activeIoTHubConnectionString);
                DeviceUpdateForm updateForm = new DeviceUpdateForm(registryManager, MAX_COUNT_OF_DEVICES, selectedDeviceId);
                updateForm.ShowDialog(this);
                updateForm.Dispose();
                await updateDevicesGridView();
                await registryManager.CloseAsync();
            }
            catch (Exception ex)
            {
                using (new CenterDialog(this))
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void deleteButton_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryManager registryManager = RegistryManager.CreateFromConnectionString(activeIoTHubConnectionString);
                string selectedDeviceId = devicesGridView.CurrentRow.Cells[0].Value.ToString();
                using (new CenterDialog(this))
                {
                    var dialogResult = MessageBox.Show($"Are you sure you want to delete the following device?\n[{selectedDeviceId}]", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dialogResult == DialogResult.Yes)
                    {
                        await registryManager.RemoveDeviceAsync(selectedDeviceId);
                        using (new CenterDialog(this))
                        {
                            MessageBox.Show("Device deleted successfully!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        await updateDevicesGridView();
                        await registryManager.CloseAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                using (new CenterDialog(this))
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion

        #region DataMonitorTab

        private async void MonitorEventHubAsync(DateTime startTime, CancellationToken ct, string consumerGroupName)
        {
            EventHubClient eventHubClient = null;
            EventHubReceiver eventHubReceiver = null;

            try
            {
                string selectedDevice = deviceIDsComboBoxForEvent.SelectedItem.ToString();
                eventHubClient = EventHubClient.CreateFromConnectionString(activeIoTHubConnectionString, "messages/events");
                eventHubTextBox.Text = "Receiving events...\r\n";
                eventHubPartitionsCount = eventHubClient.GetRuntimeInformation().PartitionCount;
                string partition = EventHubPartitionKeyResolver.ResolveToPartition(selectedDevice, eventHubPartitionsCount);
                eventHubReceiver = eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partition, startTime);

                //receive the events from startTime until current time in a single call and process them
                var events = await eventHubReceiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(20));

                foreach (var eventData in events)
                {
                    var data = Encoding.UTF8.GetString(eventData.GetBytes());
                    var enqueuedTime = eventData.EnqueuedTimeUtc.ToLocalTime();
                    var connectionDeviceId = eventData.SystemProperties["iothub-connection-device-id"].ToString();

                    if (string.CompareOrdinal(selectedDevice.ToUpper(), connectionDeviceId.ToUpper()) == 0)
                    {
                        eventHubTextBox.Text += $"{enqueuedTime}> Device: [{connectionDeviceId}], Data:[{data}]";

                        if (eventData.Properties.Count > 0)
                        {
                            eventHubTextBox.Text += "Properties:\r\n";
                            foreach (var property in eventData.Properties)
                            {
                                eventHubTextBox.Text += $"'{property.Key}': '{property.Value}'\r\n";
                            }
                        }
                        if(enableSystemProperties.Checked)
                        {
                            if (eventData.Properties.Count == 0)
                            {
                                eventHubTextBox.Text += "\r\n";
                            }
                            foreach (var item in eventData.SystemProperties)
                            {
                                eventHubTextBox.Text += $"SYSTEM>{item.Key}={item.Value}\r\n";
                            }
                        }
                        eventHubTextBox.Text += "\r\n";

                        // scroll text box to last line by moving caret to the end of the text
                        eventHubTextBox.SelectionStart = eventHubTextBox.Text.Length - 1;
                        eventHubTextBox.SelectionLength = 0;
                        eventHubTextBox.ScrollToCaret();
                    }
                }

                //having already received past events, monitor current events in a loop
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    var eventData = await eventHubReceiver.ReceiveAsync(TimeSpan.FromSeconds(1));

                    if (eventData != null)
                    {
                        var data = Encoding.UTF8.GetString(eventData.GetBytes());
                        var enqueuedTime = eventData.EnqueuedTimeUtc.ToLocalTime();

                        // Display only data from the selected device; otherwise, skip.
                        var connectionDeviceId = eventData.SystemProperties["iothub-connection-device-id"].ToString();

                        if (string.CompareOrdinal(selectedDevice, connectionDeviceId) == 0)
                        {
                            eventHubTextBox.Text += $"{enqueuedTime}> Device: [{connectionDeviceId}], Data:[{data}]";

                            if (eventData.Properties.Count > 0)
                            {
                                eventHubTextBox.Text += "Properties:\r\n";
                                foreach (var property in eventData.Properties)
                                {
                                    eventHubTextBox.Text += $"'{property.Key}': '{property.Value}'\r\n";
                                }
                            }

                            if (enableSystemProperties.Checked)
                            {
                                if (eventData.Properties.Count == 0)
                                {
                                    eventHubTextBox.Text += "\r\n";
                                }
                                foreach (var item in eventData.SystemProperties)
                                {
                                    eventHubTextBox.Text += $"SYSTEM>{item.Key}={item.Value}\r\n";
                                }
                            }

                            eventHubTextBox.Text += "\r\n";
                        }

                        // scroll text box to last line by moving caret to the end of the text
                        eventHubTextBox.SelectionStart = eventHubTextBox.Text.Length - 1;
                        eventHubTextBox.SelectionLength = 0;
                        eventHubTextBox.ScrollToCaret();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ct.IsCancellationRequested)
                {
                    eventHubTextBox.Text += $"Stopped Monitoring events. {ex.Message}\r\n";
                }
                else
                {
                    using (new CenterDialog(this))
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    eventHubTextBox.Text += $"Stopped Monitoring events. {ex.Message}\r\n";
                }
                if (eventHubReceiver != null)
                {
                    eventHubReceiver.Close();
                }
                if (eventHubClient != null)
                {
                    eventHubClient.Close();
                }
                dataMonitorButton.Enabled = true;
                deviceIDsComboBoxForEvent.Enabled = true;
                cancelMonitoringButton.Enabled = false;
            }
        }

        private void dataMonitorButton_Click(object sender, EventArgs e)
        {
            dataMonitorButton.Enabled = false;
            deviceIDsComboBoxForEvent.Enabled = false;
            cancelMonitoringButton.Enabled = true;
            ctsForDataMonitoring = new CancellationTokenSource();

            // If the user has not specified a start time by selecting the check box
            // the monitoring start time will be the current time
            if (dateTimePicker.Checked == false)
            {
                dateTimePicker.Value = DateTime.Now;
                dateTimePicker.Checked = false;
            }

            MonitorEventHubAsync(dateTimePicker.Value, ctsForDataMonitoring.Token, groupNameTextBox.Text);
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            cancelMonitoringButton.Enabled = false;
            eventHubTextBox.Text += "Cancelling...\r\n";
            ctsForDataMonitoring.Cancel();
        }

        private void clearDataButton_Click(object sender, EventArgs e)
        {
            eventHubTextBox.Clear();
        }

        private void deviceIDsComboBoxForEvent_SelectionChangeCommitted(object sender, EventArgs e)
        {
            deviceSelectedIndexForEvent = ((ComboBox)sender).SelectedIndex;
        }

        private void consumerGroupCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (consumerGroupCheckBox.Checked)
            {
                groupNameTextBox.Enabled = true;
            }
            else
            {
                groupNameTextBox.Text = DEFAULT_CONSUMER_GROUP;
                groupNameTextBox.Enabled = false;
            }
        }
        #endregion

        #region SendMessageToDeviceTab
        private async void sendMessageToDeviceButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (checkBox1.Checked)
                {
                    if (string.IsNullOrEmpty(textBoxMessage.Text))
                    {
                        cloudToDeviceMessage = DateTime.Now.ToLocalTime().ToString();
                    }
                    else
                    {
                        if (isInJsonFormat(textBoxMessage.Text)) //any JSON format string should start with "{" || "[" and end with "}" || "]"
                        {
                            JValue date = new JValue(DateTime.Now.ToLocalTime());
                            JToken t = JToken.Parse(textBoxMessage.Text);
                            if (t.Type.Equals(JTokenType.Object)) //JSON string is of type Object
                            {
                                JObject o = (JObject)t;
                                o.Add("timestamp", date);
                                cloudToDeviceMessage = o.ToString();
                            }
                            else if (t.Type.Equals(JTokenType.Array)) //JSON string is of type Array
                            {
                                JObject o = new JObject();
                                o.Add("message", (JArray)t);
                                o.Add("timestamp", date);
                                cloudToDeviceMessage = o.ToString();
                            }
                        }
                        else
                        {
                            cloudToDeviceMessage = DateTime.Now.ToLocalTime() + " - " + textBoxMessage.Text;
                        }
                    }
                }
                else
                {
                    isInJsonFormat(textBoxMessage.Text); //any JSON format string should start with "{" || "[" and end with "}" || "]"
                    cloudToDeviceMessage = textBoxMessage.Text;
                }

                ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(activeIoTHubConnectionString);

                var serviceMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes(cloudToDeviceMessage));
                serviceMessage.Ack = Microsoft.Azure.Devices.DeliveryAcknowledgement.Full;
                serviceMessage.MessageId = Guid.NewGuid().ToString();

                for (var i = 0; i < messagePropertiesGrid.Rows.Count - 1; i++)
                {
                    var row = messagePropertiesGrid.Rows[i];
                    if (row.Cells[0].Value == null && row.Cells[1].Value == null)
                    {
                        continue;
                    }

                    if (row.Cells[0].Value == null || row.Cells[1].Value == null)
                    {
                        throw new InvalidOperationException("Properties have null key or value.");
                    }

                    serviceMessage.Properties.Add(row.Cells[0].Value?.ToString() ?? string.Empty, row.Cells[1].Value?.ToString() ?? string.Empty);
                }

                await serviceClient.SendAsync(deviceIDsComboBoxForCloudToDeviceMessage.SelectedItem.ToString(), serviceMessage);

                messagesTextBox.Text += $"Sent to Device ID: [{deviceIDsComboBoxForCloudToDeviceMessage.SelectedItem.ToString()}], Message:\"{cloudToDeviceMessage}\", message Id: {serviceMessage.MessageId}\n";

                await serviceClient.CloseAsync();

            }
            catch (Exception ex)
            {
                using (new CenterDialog(this))
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void messageClearButton_Click(object sender, EventArgs e)
        {
            messagesTextBox.Clear();
        }

        private async void deviceIDsComboBoxForMessage_SelectionChangeCommitted(object sender, EventArgs e)
        {
            deviceSelectedIndexForC2DMessage = ((ComboBox)sender).SelectedIndex;
            if (checkBoxMonitorFeedbackEndpoint.Checked)
            {
                StopMonitoringFeedback();
                await StartMonitoringFeedback();
            }
        }
        #endregion

        #region CallDeviceMethod

        private async void callDeviceMethodButton_Click(object sender, EventArgs e)
        {
            returnStatusTextBox.Text = "";
            returnPayloadTextBox.Text = "";

            string deviceId = deviceIDsComboBoxForDeviceMethod.SelectedItem.ToString();

            string methodName = methodNameTextBox.Text;
            string payload = methodPayloadTextBox.Text;

            double timeout = System.Convert.ToDouble(callDeviceMethodNumericUpDown.Value);

            DeviceTwinAndMethod deviceMethod = new DeviceTwinAndMethod(activeIoTHubConnectionString, deviceId);

            ctsForDeviceMethod = new CancellationTokenSource();
            try
            {
                callDeviceMethodButton.Enabled = false;
                callDeviceMethodCancelButton.Enabled = true;
                DeviceMethodReturnValue deviceMethodReturnValue = await deviceMethod.CallDeviceMethod(methodName, payload, TimeSpan.FromSeconds(timeout), ctsForDeviceMethod.Token);
                returnStatusTextBox.Text = deviceMethodReturnValue.Status;
                returnPayloadTextBox.Text = deviceMethodReturnValue.Payload;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Device Method", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                callDeviceMethodButton.Enabled = true;
                callDeviceMethodCancelButton.Enabled = false;
            }
        }

        private void callDeviceMethodCancelButton_Click(object sender, EventArgs e)
        {
            ctsForDeviceMethod.Cancel();
        }

        private void deviceIDsComboBoxForDeviceMethod_SelectionChangeCommitted(object sender, EventArgs e)
        {
            deviceSelectedIndexForDeviceMethod = ((ComboBox)sender).SelectedIndex;
        }
        #endregion

        private async void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            try
            {
                if (e.TabPage == tabData || e.TabPage == tabMessagesToDevice || e.TabPage == tabDeviceMethod || e.TabPage == tabCommandReceiver)
                {
                    await updateDeviceIdsComboBoxes(runIfNullOrEmpty: false);
                }

                if (e.TabPage == tabCommandReceiver)
                {
                    fillCommandReceiverActionComboBox(runIfNullOrEmpty: false);
                }

                if (e.TabPage == tabManagement)
                {
                    UpdateListOfDevices();
                }
            }
            catch (Exception ex)
            {
                using (new CenterDialog(this))
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void dhConStringTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control & e.KeyCode == Keys.A)
            {
                dhConStringTextBox.SelectAll();
            }
        }

        private void copyAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder str = new StringBuilder();

            List<string> content = new List<string>();

            for (int i = 0; i < devicesGridView.Columns.Count; i++)
            {
                content.Add(devicesGridView.Columns[i].Name);
            }

            str.Append(String.Join(",", content));

            for (int i = 0; i < devicesGridView.Rows.Count; i++)
            {
                content.Clear();

                for (int j = 0; j < devicesGridView.Rows[i].Cells.Count; j++)
                {
                    object obj = devicesGridView.Rows[i].Cells[j].Value;
                    content.Add(obj != null ? obj.ToString() : String.Empty);
                }

                str.AppendLine();
                str.Append(String.Join(",", content));
            }

            if (str.Length > 0)
            {
                Clipboard.SetText(str.ToString());
            }
        }

        private void copySelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder str = new StringBuilder();

            List<string> content = new List<string>();

            for (int i = 0; i < devicesGridView.Columns.Count; i++)
            {
                content.Add(devicesGridView.Columns[i].Name);
            }

            str.Append(String.Join(",", content));

            for (int i = 0; i < devicesGridView.SelectedRows.Count; i++)
            {
                content.Clear();

                for (int j = 0; j < devicesGridView.Rows[i].Cells.Count; j++)
                {
                    object obj = devicesGridView.Rows[devicesGridView.SelectedRows[0].Index].Cells[j].Value;
                    content.Add(obj != null ? obj.ToString() : String.Empty);
                }

                str.AppendLine();
                str.Append(String.Join(",", content));
            }

            if (str.Length > 0)
            {
                Clipboard.SetText(str.ToString());
            }
        }

        private void getDeviceConnectionStringToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (devicesGridView.SelectedRows.Count > 0)
            {
                Clipboard.SetText(devicesGridView.Rows[devicesGridView.SelectedRows[0].Index].Cells[5].Value.ToString());
            }
        }

        private void devicesGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            updateDeviceButton.Enabled = true;
            deleteDeviceButton.Enabled = true;
            sasTokenButton.Enabled = true;
        }

        private async Task ReceiveCommands(CancellationToken ct, string deviceId)
        {
            using (DeviceClient client = DeviceClient.CreateFromConnectionString(activeIoTHubConnectionString, deviceId))
            {

                while (!ct.IsCancellationRequested)
                {
                    var message = await client.ReceiveAsync();

                    if (message == null)
                        continue;

                    var bytes = message.GetBytes();

                    var msgString = Encoding.UTF8.GetString(bytes);

                    deviceCommandsRichTxtBox.AppendText("Received Command:" + Environment.NewLine);
                    deviceCommandsRichTxtBox.AppendText(msgString);
                    deviceCommandsRichTxtBox.AppendText(Environment.NewLine);

                    var action = actionComboBoxForCommandReceiver.SelectedItem?.ToString();

                    switch (action)
                    {
                        case "Complete":
                            {
                                await client.CompleteAsync(message);
                                deviceCommandsRichTxtBox.AppendText($"Command was completed succesful.");
                                deviceCommandsRichTxtBox.AppendText(Environment.NewLine);
                                break;
                            }
                        case "Abondon":
                            {
                                await client.AbandonAsync(message);
                                deviceCommandsRichTxtBox.AppendText($"Command was abondened succesful.");
                                deviceCommandsRichTxtBox.AppendText(Environment.NewLine);
                                break;
                            }
                        case "None":
                            break;
                    }
                }
            }
        }

        private async Task MonitorFeedback(CancellationToken ct, string deviceId)
        {
            ServiceClient serviceClient = null;

            serviceClient = ServiceClient.CreateFromConnectionString(activeIoTHubConnectionString);
            var feedbackReceiver = serviceClient.GetFeedbackReceiver();

            while ((checkBoxMonitorFeedbackEndpoint.CheckState == CheckState.Checked) && (!ct.IsCancellationRequested))
            {
                var feedbackBatch = await feedbackReceiver.ReceiveAsync(TimeSpan.FromSeconds(0.5));
                if (feedbackBatch != null)
                {
                    foreach (var feedbackMessage in feedbackBatch.Records.Where(fm => fm.DeviceId == deviceId))
                    {
                        if (feedbackMessage != null)
                        {
                            messagesTextBox.Text += $"Message Feedback status: \"{feedbackMessage.StatusCode}\", Description: \"{feedbackMessage.Description}\", Original Message Id: {feedbackMessage.OriginalMessageId}\n";
                        }
                    }

                    await feedbackReceiver.CompleteAsync(feedbackBatch);
                }
            }

            if (serviceClient != null)
            {
                await serviceClient.CloseAsync();
            }
        }

        private async Task StartReceiveCommand()
        {
            ctsForReceiveCommand = new CancellationTokenSource();
            deviceCommandsRichTxtBox.AppendText($"Started receiving commands for device {deviceIDComboBoxForCommandReceiver.SelectedItem.ToString()}.");
            deviceCommandsRichTxtBox.AppendText(Environment.NewLine);

            await ReceiveCommands(ctsForReceiveCommand.Token, deviceIDComboBoxForCommandReceiver.SelectedItem.ToString());
        }

        private void StopReceiveCommands()
        {
            if (ctsForReceiveCommand != null)
            {
                deviceCommandsRichTxtBox.AppendText("Stopped receiving commands.");
                deviceCommandsRichTxtBox.AppendText(Environment.NewLine);

                ctsForReceiveCommand.Cancel();
                ctsForReceiveCommand = null;
            }
        }

        private async Task StartMonitoringFeedback()
        {
            StopMonitoringFeedback();

            ctsForFeedbackMonitoring = new CancellationTokenSource();

            messagesTextBox.Text += $"Started monitoring feedback for device {deviceIDsComboBoxForCloudToDeviceMessage.SelectedItem.ToString()}.\r\n";

            await MonitorFeedback(ctsForFeedbackMonitoring.Token, deviceIDsComboBoxForCloudToDeviceMessage.SelectedItem.ToString());
        }

        void StopMonitoringFeedback()
        {
            if (ctsForFeedbackMonitoring != null)
            {
                messagesTextBox.Text += "Stopped monitoring feedback.\r\n";

                ctsForFeedbackMonitoring.Cancel();
                ctsForFeedbackMonitoring = null;
            }
        }

        private async void checkBoxMonitorFeedbackEndpoint_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxMonitorFeedbackEndpoint.CheckState == CheckState.Checked)
            {
                await StartMonitoringFeedback();
            }
            else
            {
                StopMonitoringFeedback();
            }
        }

        private async void sasTokenButton_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryManager registryManager = RegistryManager.CreateFromConnectionString(activeIoTHubConnectionString);
                SASTokenForm sasForm = new SASTokenForm(registryManager, MAX_COUNT_OF_DEVICES, iotHubHostName);
                sasForm.ShowDialog(this);
                sasForm.Dispose();
                await updateDevicesGridView();
                await registryManager.CloseAsync();
            }
            catch (Exception ex)
            {
                using (new CenterDialog(this))
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void showDevicePropertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeviceTwinPropertiesForm deviceTwinPropertiesForm = new DeviceTwinPropertiesForm();

            if (devicesGridView.SelectedRows.Count > 0)
            {
                String deviceName = devicesGridView.Rows[devicesGridView.SelectedRows[0].Index].Cells[0].Value.ToString();
                List<string> deviceList = new List<string>();
                for (int i = 0; i < devicesGridView.RowCount - 1; i++)
                {
                    deviceList.Add(devicesGridView.Rows[i].Cells[0].Value.ToString());
                }
                deviceTwinPropertiesForm.Execute(activeIoTHubConnectionString, deviceName, deviceList);
            }
        }

        private void deviceTwinPropertiesBtn_Click(object sender, EventArgs e)
        {
            showDevicePropertiesToolStripMenuItem_Click(this, null);
        }

        private void clearCommandsBtn_Click(object sender, EventArgs e)
        {
            deviceCommandsRichTxtBox.Clear();
        }

        private async void receiveCommandsBtn_Click(object sender, EventArgs e)
        {
            deviceIDComboBoxForCommandReceiver.Enabled = false;
            actionComboBoxForCommandReceiver.Enabled = false;
            receiveCommandsBtn.Enabled = false;
            cancelReceiveCommandsBtn.Enabled = true;

            await StartReceiveCommand();
        }

        private void cancelReceiveCommandsBtn_Click(object sender, EventArgs e)
        {
            StopReceiveCommands();

            deviceIDComboBoxForCommandReceiver.Enabled = true;
            actionComboBoxForCommandReceiver.Enabled = true;
            receiveCommandsBtn.Enabled = true;
            cancelReceiveCommandsBtn.Enabled = false;
        }

        private void deviceIDComboBoxForCommandReceiver_SelectionChangeCommitted(object sender, EventArgs e)
        {
            deviceSelectedIndexForCommandReceiver = ((ComboBox)sender).SelectedIndex;
        }
    }
}
