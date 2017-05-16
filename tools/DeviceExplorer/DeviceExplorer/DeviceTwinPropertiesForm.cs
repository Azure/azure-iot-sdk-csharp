using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace DeviceExplorer
{
    public partial class DeviceTwinPropertiesForm : Form
    {
        private string _iotHubConnectionString;
        private string _deviceName;
        private List<string> _deviceList;
        private string _deviceJson;
        private string _tagsJson;
        private string _reportedPropertiesJson;
        private string _desiredPropertiesJson;
        private bool _initialIndexSet;
        private bool _runOnce = true;

        public DeviceTwinPropertiesForm()
        {
            InitializeComponent();
        }

        public async Task<bool> GetDeviceTwinData()
        {
            DeviceTwinAndMethod deviceMethod = new DeviceTwinAndMethod(_iotHubConnectionString, _deviceName);
            DeviceTwinData deviceTwinData = await deviceMethod.GetDeviceTwinData();

            _deviceJson = deviceTwinData.DeviceJson;
            _tagsJson = deviceTwinData.TagsJson;
            _reportedPropertiesJson = deviceTwinData.ReportedPropertiesJson;
            _desiredPropertiesJson = deviceTwinData.DesiredPropertiesJson;

            if (_deviceJson == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        static string FormatJson(string json)
        {
            // Taken from: https://stackoverflow.com/a/21407175/8080
            var parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        public async Task<bool> UpdateDialogData()
        {
            bool isOk = await GetDeviceTwinData();

            jsonRichTextBox0.Text = FormatJson(_deviceJson);
            jsonRichTextBox1.Text = FormatJson(_tagsJson);
            jsonRichTextBox2.Text = FormatJson(_reportedPropertiesJson);
            jsonRichTextBox3.Text = FormatJson(_desiredPropertiesJson);

            if (_runOnce)
            {
                _runOnce = false;
                jsonEditRichTextBox.Text = FormatJson(
                    $@"{{ ""properties"": {{ ""desired"": {{ {Environment.NewLine}{Environment.NewLine}{
                            Environment.NewLine
                        }{Environment.NewLine}}}}}}}"
                );
            }

            return isOk;
        }

        public async void Execute(string activeIotHubConnectionString, string selectedDeviceName, List<string> currentDeviceList)
        {
            _iotHubConnectionString = activeIotHubConnectionString;
            _deviceName = selectedDeviceName;
            _deviceList = currentDeviceList;

            _initialIndexSet = true;

            deviceListCombo.Items.Clear();
            if (_deviceList != null)
            {
                for (int i = 0; i < _deviceList.Count; i++)
                {
                    deviceListCombo.Items.Add(_deviceList[i]);
                }
            }
            deviceListCombo.SelectedItem = _deviceName;

            if (await UpdateDialogData())
            {
                ShowDialog();
            }
        }

        private void deviceListCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_initialIndexSet)
            {
                _deviceName = deviceListCombo.SelectedItem.ToString();
                refreshBtn_Click(this, null);
            }
            _initialIndexSet = false;
        }

        private async void refreshBtn_Click(object sender, EventArgs e)
        {
            await UpdateDialogData();
        }

        private async void sendBtn_Click(object sender, EventArgs e)
        {
            jsonRichTextBox0.Text = "";
            jsonRichTextBox1.Text = "";
            jsonRichTextBox2.Text = "";
            jsonRichTextBox3.Text = "";

            var deviceMethod = new DeviceTwinAndMethod(_iotHubConnectionString, _deviceName);
            await deviceMethod.UpdateTwinData(jsonEditRichTextBox.Text);

            refreshBtn_Click(this, null);
        }
    }
}
