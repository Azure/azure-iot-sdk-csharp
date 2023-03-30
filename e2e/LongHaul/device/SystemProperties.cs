using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    internal class SystemProperties
    {
        [JsonPropertyName("systemArchitecture")]
        public string SystemArchitecture { get; set; } = RuntimeInformation.OSArchitecture.ToString();

        [JsonPropertyName("osVersion")]
        public string OsVersion { get; set; } = RuntimeInformation.OSDescription;

        [JsonPropertyName("frameworkDescription")]
        public string FrameworkDescription { get; set; } = RuntimeInformation.FrameworkDescription;
    }
}
