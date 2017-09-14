using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Security;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        private const int MAX_PAST_CONNECTION_STRINGS = 5;

        private bool devicesListed = false;
        private static int eventHubPartitionsCount;
        private static string activeIoTHubConnectionString;
        private string iotHubHostName;

        private static string cloudToDeviceMessage;

        private static int deviceSelectedIndexForEvent = 0;
        private static int deviceSelectedIndexForC2DMessage = 0;
        private static int deviceSelectedIndexForDeviceMethod = 0;

        private static CancellationTokenSource ctsForDataMonitoring;
        private static CancellationTokenSource ctsForFeedbackMonitoring;
        private static CancellationTokenSource ctsForDeviceMethod;

        private const string DEFAULT_CONSUMER_GROUP = "$Default";
        #endregion

        #region properties

        /// <summary>
        /// Retrieve past connection strings stored in settings
        /// </summary>
        IEnumerable<string> PastConnectionStrings
        {
            get
            {
                if (Properties.Settings.Default["Microsoft_IoTHub_PastConnectionStrings"] is StringCollection pastStringsCollection)
                {
                    foreach (string item in pastStringsCollection)
                        yield return item;
                }
            }
        }

        #endregion

        public MainForm()
        {
            InitializeComponent();

            // Extract settings from local config file
            try
            {
                Properties.Settings.Default.Reload();

                dhConStringTextBox.Items.AddRange(PastConnectionStrings.ToArray());
                dhConStringTextBox.Text = Properties.Settings.Default["Microsoft_IoTHub_ConnectionString"] as string;

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

            try
            {
                if (Properties.Settings.Default["Microsoft_IoTHub_ConsumerGroup"] is string consumerGroup)
                    groupNameTextBox.Text = consumerGroup == "" ? DEFAULT_CONSUMER_GROUP : consumerGroup;
            }
            catch
            {
                groupNameTextBox.Text = DEFAULT_CONSUMER_GROUP;
            }

            // Initialize fields
            activeIoTHubConnectionString = dhConStringTextBox.Text;
            dateTimePicker.Value = DateTime.Now;

            numericUpDownTTL.Maximum = MAX_TTL_VALUE;
            numericUpDownTTL.Value = MAX_TTL_VALUE;
            cancelMonitoringButton.Enabled = false;

            // Set up the DataGridView.
            devicesGridView.MultiSelect = false;
            devicesGridView.ScrollBars = ScrollBars.Both;

            updateDeviceButton.Enabled = false;
            deleteDeviceButton.Enabled = false;
            sasTokenButton.Enabled = false;

            // Select connection box by default
            dhConStringTextBox.Select(0, 0);
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
                var builder = IotHubConnectionStringBuilder.Create(connectionString);
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

        private async Task updateDeviceIdsComboBoxes(bool runIfNullOrEmpty = true)
        {
            if (!String.IsNullOrEmpty(activeIoTHubConnectionString) || runIfNullOrEmpty)
            {
                List<string> deviceIdsForEvent = new List<string>();
                List<string> deviceIdsForC2DMessage = new List<string>();
                List<string> deviceIdsForDeviceMethod = new List<string>();
                RegistryManager registryManager = RegistryManager.CreateFromConnectionString(activeIoTHubConnectionString);

                var devices = await registryManager.GetDevicesAsync(MAX_COUNT_OF_DEVICES);
                foreach (var device in devices)
                {
                    deviceIdsForEvent.Add(device.Id);
                    deviceIdsForC2DMessage.Add(device.Id);
                    deviceIdsForDeviceMethod.Add(device.Id);
                }
                await registryManager.CloseAsync();
                deviceIDsComboBoxForEvent.DataSource = deviceIdsForEvent.OrderBy(c => c).ToList();
                deviceIDsComboBoxForCloudToDeviceMessage.DataSource = deviceIdsForC2DMessage.OrderBy(c => c).ToList();
                deviceIDsComboBoxForDeviceMethod.DataSource = deviceIdsForDeviceMethod.OrderBy(c => c).ToList();

                deviceIDsComboBoxForEvent.SelectedIndex = deviceSelectedIndexForEvent;
                deviceIDsComboBoxForCloudToDeviceMessage.SelectedIndex = deviceSelectedIndexForC2DMessage;
                deviceIDsComboBoxForDeviceMethod.SelectedIndex = deviceSelectedIndexForDeviceMethod;
            }
        }

        private void persistSettingsToAppConfig()
        {
            var dhSetting = dhConStringTextBox.Text;
            var pgHostnameSetting = protocolGatewayHost.Text;

            Properties.Settings.Default["Microsoft_IoTHub_ConnectionString"] = dhSetting;
            Properties.Settings.Default["Microsoft_Protocol_Gateway_Hostname"] = pgHostnameSetting;

            // Insert latest string
            if (dhConStringTextBox.Items.Contains(dhSetting) == false)
                dhConStringTextBox.Items.Add(dhSetting);

            // Save active connection string list. Union current with other items
            var collection = new StringCollection();
            collection.AddRange(
                new string[] { dhSetting }
                .Union(from string item in dhConStringTextBox.Items select item)
                .Take(MAX_PAST_CONNECTION_STRINGS)
                .ToArray());
            Properties.Settings.Default["Microsoft_IoTHub_PastConnectionStrings"] = collection;

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
            var devicesProcessor           = new DevicesProcessor(activeIoTHubConnectionString, MAX_COUNT_OF_DEVICES, protocolGatewayHost.Text);
            var devicesList                = await devicesProcessor.GetDevices();
            var sortableDevicesBindingList = new SortableBindingList<DeviceEntity>(devicesList.OrderByDescending(p => p.LastActivityTime));

            devicesGridView.DataSource = sortableDevicesBindingList;
            devicesGridView.ReadOnly = true;
            devicesGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            devicesGridView.Sort(devicesGridView.Columns[nameof(DeviceEntity.LastActivityTime)], System.ComponentModel.ListSortDirection.Descending);

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

        private T InFormThread<T>(Func<T> action)
        {
            if (this.InvokeRequired)
                return (T)this.Invoke(action);
            else
                return action();
        }

        private void InFormThread(Action action)
        {
            if (this.InvokeRequired)
                this.Invoke(action);
            else
                action();
        }

        private async void MonitorEventHubAsync(DateTime startTime, CancellationToken ct, string consumerGroupName,
                                                string selectedDevice,
                                                bool showSystemProperties, bool showOperationsMonitoring)
        {
            EventHubClient eventHubClient = null;
            EventHubReceiver eventHubReceiver = null;

            /// Log the event to the eventhubdata textbox
            void LogEvent(EventData eventData)
            {
                var data = Encoding.UTF8.GetString(eventData.GetBytes());
                var enqueuedTime = eventData.EnqueuedTimeUtc.ToLocalTime();
                var connectionDeviceId = showOperationsMonitoring
                    ? data
                    : eventData.SystemProperties["iothub-connection-device-id"].ToString();

                if (connectionDeviceId.IndexOf(selectedDevice, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    var addedText = new StringBuilder()
                        .AppendLine($"{enqueuedTime}> Device: [{selectedDevice}]")
                        .AppendLine("Data:").Append(data);

                    if (eventData.Properties.Count > 0)
                    {
                        addedText.AppendLine();
                        addedText.AppendLine("Properties:");
                        foreach (var property in eventData.Properties)
                        {
                            addedText.AppendLine($"'{property.Key}': '{property.Value}'");
                        }
                    }
                    if (showSystemProperties)
                    {
                        if (eventData.Properties.Count == 0)
                        {
                            addedText.AppendLine();
                        }
                        foreach (var item in eventData.SystemProperties)
                        {
                            addedText.AppendLine($"SYSTEM>{item.Key}={item.Value}");
                        }
                    }
                    addedText.AppendLine();

                    InFormThread(() =>
                    {
                        eventHubTextBox.Text += addedText.ToString();

                        // scroll text box to last line by moving caret to the end of the text
                        eventHubTextBox.SelectionStart = eventHubTextBox.Text.Length - 1;
                        eventHubTextBox.SelectionLength = 0;

                        eventHubTextBox.ScrollToCaret();
                    });
                }
            }

            try
            {
                InFormThread(() => eventHubTextBox.Text = "Receiving events...\r\n");
                
                if (showOperationsMonitoring)
                    eventHubClient = EventHubClient.CreateFromConnectionString(activeIoTHubConnectionString, "messages/operationsMonitoringEvents");
                else
                    eventHubClient = EventHubClient.CreateFromConnectionString(activeIoTHubConnectionString, "messages/events");

                eventHubPartitionsCount = eventHubClient.GetRuntimeInformation().PartitionCount;
                string partition = EventHubPartitionKeyResolver.ResolveToPartition(selectedDevice, eventHubPartitionsCount);
                eventHubReceiver = eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partition, startTime);

                //receive the events from startTime until current time in a single call and process them
                var events = await eventHubReceiver.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(20));

                foreach (var eventData in events)
                {
                    LogEvent(eventData);
                }

                //having already received past events, monitor current events in a loop
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    var eventData = await eventHubReceiver.ReceiveAsync(TimeSpan.FromSeconds(1));

                    if (eventData != null)
                        LogEvent(eventData);
                }
            }
            catch (Exception ex)
            {
                if (ct.IsCancellationRequested)
                {
                    InFormThread(() => eventHubTextBox.Text += $"Stopped Monitoring events. {ex.Message}\r\n");
                }
                else
                {
                    InFormThread(() =>
                    {
                        using (new CenterDialog(this))
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        eventHubTextBox.Text += $"Stopped Monitoring events. {ex.Message}\r\n";
                    });
                }
                if (eventHubReceiver != null)
                {
                    eventHubReceiver.Close();
                }
                if (eventHubClient != null)
                {
                    eventHubClient.Close();
                }
                InFormThread(() =>
                {
                    dataMonitorButton.Enabled = true;
                    deviceIDsComboBoxForEvent.Enabled = true;
                    cancelMonitoringButton.Enabled = false;
                });
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

            var dateValue = dateTimePicker.Value;
            var groupName = groupNameTextBox.Text;
            var showSystemProperties = enableSystemProperties.Checked;
            var showOperationsMonitoring = enableOperationMonitoring.Checked;
            var selectedDevice = deviceIDsComboBoxForEvent.SelectedItem.ToString();

            if (consumerGroupCheckBox.Checked && groupNameTextBox.Text != DEFAULT_CONSUMER_GROUP)
            {
                Properties.Settings.Default["Microsoft_IoTHub_ConsumerGroup"] = groupNameTextBox.Text;
                Properties.Settings.Default.Save();
            }

            ThreadPool.QueueUserWorkItem((state) =>
                MonitorEventHubAsync(dateValue, ctsForDataMonitoring.Token, 
                    groupName, selectedDevice, showSystemProperties, showOperationsMonitoring), 
                null);
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
                serviceMessage.Ack = DeliveryAcknowledgement.Full;
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
                StartMonitoringFeedback();
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
                if (e.TabPage == tabData || e.TabPage == tabMessagesToDevice || e.TabPage == tabDeviceMethod)
                {
                    await updateDeviceIdsComboBoxes(runIfNullOrEmpty: false);
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

        private void copyDeviceIdToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var rows = from DataGridViewRow row in devicesGridView.SelectedRows
                       select row.DataBoundItem as DeviceEntity;

            if (rows.FirstOrDefault() is DeviceEntity selected)
                Clipboard.SetText(selected.Id);
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

        private async Task MonitorFeedback(CancellationToken ct, string deviceId)
        {
            ServiceClient serviceClient = null;

            serviceClient = ServiceClient.CreateFromConnectionString(activeIoTHubConnectionString);
            var feedbackReceiver = serviceClient.GetFeedbackReceiver();

            while (InFormThread(() => checkBoxMonitorFeedbackEndpoint.CheckState == CheckState.Checked) && (!ct.IsCancellationRequested))
            {
                var feedbackBatch = await feedbackReceiver.ReceiveAsync(TimeSpan.FromSeconds(0.5));
                if (feedbackBatch != null)
                {
                    foreach (var feedbackMessage in feedbackBatch.Records.Where(fm => fm.DeviceId == deviceId))
                    {
                        if (feedbackMessage != null)
                        {
                            InFormThread(() => 
                                messagesTextBox.Text += $"Message Feedback status: \"{feedbackMessage.StatusCode}\", Description: \"{feedbackMessage.Description}\", Original Message Id: {feedbackMessage.OriginalMessageId}\n");
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

        private void StartMonitoringFeedback()
        {
            StopMonitoringFeedback();

            ctsForFeedbackMonitoring = new CancellationTokenSource();

            messagesTextBox.Text += $"Started monitoring feedback for device {deviceIDsComboBoxForCloudToDeviceMessage.SelectedItem.ToString()}.\r\n";

            ThreadPool.QueueUserWorkItem(async (state) =>
                await MonitorFeedback(ctsForFeedbackMonitoring.Token, deviceIDsComboBoxForCloudToDeviceMessage.SelectedItem.ToString())
            );
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
                StartMonitoringFeedback();
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

        private void tabCallDeviceMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            const int Configuration      = 0,
                      Management         = 1,
                      Data               = 2,
                      MessagesToDevice   = 3,
                      CallMethodOnDevice = 4;

            switch (tabCallDeviceMethod.SelectedIndex)
            {
                case Configuration:
                    this.AcceptButton = updateSettingsButton;
                    break;
                case Data:
                    this.AcceptButton = dataMonitorButton;
                    break;

                default:
                case Management:
                case MessagesToDevice:
                case CallMethodOnDevice:
                    this.AcceptButton = null;
                    break;
            }
        }
    }
}
