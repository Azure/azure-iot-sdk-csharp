// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using Shared;
    using System;
    using System.Globalization;
    using System.Threading.Tasks;

    internal class IoTHubClientDiagnostic
    {
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private const string DiagnosticCreationTimeUtcKey = "creationtimeutc";
        private static readonly DateTime Dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal const string TwinDiagSamplingRateKey = "__e2e_diag_sample_rate";
        internal const string TwinDiagErrorKey = "__e2e_diag_info";

        internal DeviceClient deviceClient;
        internal int currentSamplingRate = 0;
        private bool isStarted;

        internal IoTHubClientDiagnostic(DeviceClient device)
        {
            this.deviceClient = device;
        }

        internal async Task StartListeningE2EDiagnosticSettingChanges()
        {
            await this.Start();
        }

        internal void ParseDiagnosticInfoFromTwin(TwinCollection desiredProperties, bool receivedFullTwinForTheFirstTime = false)
        {
            if (desiredProperties.Contains(TwinDiagSamplingRateKey))
            {
                if (desiredProperties[TwinDiagSamplingRateKey] == null)
                {
                    this.currentSamplingRate = 0;
                    this.ReportDiagnosticSettings($"Property {TwinDiagSamplingRateKey} is null, so disable E2E diagnostic by setting sampling percentage to 0.");
                }
                else
                {
                    try
                    {
                        var rate = (int)desiredProperties[TwinDiagSamplingRateKey].Value;
                        if (rate < 0 || rate > 100)
                        {
                            this.ReportDiagnosticSettings($"Property {TwinDiagSamplingRateKey} = {desiredProperties[TwinDiagSamplingRateKey].ToString()} should be between [0, 100].");
                        }
                        else
                        {
                            this.currentSamplingRate = rate;
                            this.ReportDiagnosticSettings();
                        }
                    }
                    catch (Exception)
                    {
                        this.ReportDiagnosticSettings($"Cannot parse {TwinDiagSamplingRateKey} = {desiredProperties[TwinDiagSamplingRateKey].ToString()} property from twin settings.");
                    }
                }
            }
            else if (receivedFullTwinForTheFirstTime)
            {
                this.currentSamplingRate = 0;
                this.ReportDiagnosticSettings($"Property {TwinDiagSamplingRateKey} is not exist, so disable E2E diagnostic by setting sampling percentage to 0.");
            }

            this.OnDiagnosticSettingsChanged(this.currentSamplingRate);
        }

        internal static bool AddDiagnosticInfoIfNecessary(Message message, int diagnosticSamplingPercentage, ref int currentMessageCount)
        {
            bool result = false;

            if (ShouldAddDiagnosticInfo(diagnosticSamplingPercentage, ref currentMessageCount))
            {
                string creationTimeUtc = string.Format(CultureInfo.InvariantCulture, "{0}={1}", DiagnosticCreationTimeUtcKey, CurrentUtcTimeToSecond().ToString("0.000", CultureInfo.InvariantCulture));
                message.SystemProperties[MessageSystemPropertyNames.DiagId] = GenerateEightRandomCharacters();
                message.SystemProperties[MessageSystemPropertyNames.DiagCorrelationContext] = creationTimeUtc;
                result = true;
            }

            return result;
        }

        internal static bool HasDiagnosticProperties(Message message)
        {
            return message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagId) && message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagCorrelationContext);
        }

        private void DisableE2EDiagnostic()
        {
            this.isStarted = false;
            this.OnDiagnosticSettingsChanged(0);
        }

        private void ReportDiagnosticSettings(string message = "")
        {
            var reportedProperties = new TwinCollection();
            reportedProperties[TwinDiagSamplingRateKey] = this.currentSamplingRate;
            reportedProperties[TwinDiagErrorKey] = message;

            this.deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        private void OnDiagnosticSettingsChanged(int samplingPercentage)
        {
            this.deviceClient.diagnosticSamplingPercentage = samplingPercentage;
        }

        private async Task Start()
        {
            if (!this.isStarted)
            {
                try
                {
                    Twin twin = await this.deviceClient.GetTwinAsync();
                    if (twin != null)
                    {
                        this.ParseDiagnosticInfoFromTwin(twin.Properties.Desired, true);
                    }
                    this.isStarted = true;
                }
                catch (Exception)
                {
                    this.isStarted = false;
                    this.DisableE2EDiagnostic();
                }
            }
        }

        private static string GenerateEightRandomCharacters()
        {
            var stringChars = new char[8];
            var random = new Random();
            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = Chars[random.Next(Chars.Length)];
            }

            return new string(stringChars);
        }

        private static bool ShouldAddDiagnosticInfo(int diagnosticSamplingPercentage, ref int currentMessageCount)
        {
            bool result = false;

            if (diagnosticSamplingPercentage > 0)
            {
                currentMessageCount = currentMessageCount == 100 ? 1 : currentMessageCount + 1;
                result = Math.Floor((currentMessageCount - 2) * diagnosticSamplingPercentage / 100.0) < Math.Floor((currentMessageCount - 1) * diagnosticSamplingPercentage / 100.0);
            }

            return result;
        }

        private static double CurrentUtcTimeToSecond()
        {
            TimeSpan span = DateTime.UtcNow - Dt1970;
            return span.TotalSeconds;
        }
    }
}
