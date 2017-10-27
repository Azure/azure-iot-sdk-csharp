// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh. 
    /// </summary>
    public sealed class DeviceAuthenticationWithTpm : DeviceAuthenticationWithTokenRefresh
    {
        private SecurityClientHsmTpm _securityClient;
        private int _timeToLiveSeconds;

        public DeviceAuthenticationWithTpm(
            string deviceId, 
            SecurityClientHsmTpm securityClient,
            int timeToLiveSeconds = 1 * 60 * 60,
            int timeToLiveBufferSeconds = 10 * 60) : base(deviceId, timeToLiveBufferSeconds)
        {
            if (securityClient == null)
            {
                throw new ArgumentNullException(nameof(securityClient));
            }

            if (timeToLiveSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeToLiveSeconds));
            }

            if (timeToLiveBufferSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeToLiveBufferSeconds));
            }

            _securityClient = securityClient;
            _timeToLiveSeconds = timeToLiveSeconds;
        }
        
        protected override Task<string> SafeCreateNewToken(string iotHubName)
        {
            const long WINDOWS_TICKS_PER_SEC = 10000000;
            const long EPOCH_DIFFERNECE = 11644473600;
            long expirationTime = (DateTime.Now.ToUniversalTime().ToFileTime() / WINDOWS_TICKS_PER_SEC) 
                                  - EPOCH_DIFFERNECE + _timeToLiveSeconds;

            string sasToken = "";
            if ((iotHubName.Length > 0) && (DeviceId.Length > 0))
            {
                // Encode the message to sign with the TPM
                UTF8Encoding utf8 = new UTF8Encoding();
                string tokenContent = iotHubName + "/devices/" + DeviceId + "\n" + expirationTime;
                byte[] encodedBytes = utf8.GetBytes(tokenContent);

                // Sign the message
                byte[] hmac = _securityClient.Sign(encodedBytes);

                // If we got a signature format it
                if (hmac.Length > 0)
                {
                    // Encode the output and assemble the connection string
                    string hmacString = AzureUrlEncode(System.Convert.ToBase64String(hmac));
                    sasToken = "SharedAccessSignature sr=" + iotHubName + "/devices/" + DeviceId + "&sig=" + hmacString + "&se=" + expirationTime;
                }
            }

            return Task.FromResult<string>(sasToken);
        }

        private string AzureUrlEncode(string stringIn)
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            string[] conversionTable = {
            "\0", "%01", "%02", "%03", "%04", "%05", "%06", "%07", "%08", "%09", "%0a", "%0b", "%0c", "%0d", "%0e", "%0f",
            "%10", "%11", "%12", "%13", "%14", "%15", "%16", "%17", "%18", "%19", "%1a", "%1b", "%1c", "%1d", "%1e", "%1f",
            "%20", "!", "%22", "%23", "%24", "%25", "%26", "%27", "(", ")", "*", "%2b", "%2c", "-", ".", "%2f",
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "%3a", "%3b", "%3c", "%3d", "%3e", "%3f",
            "%40", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O",
            "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "%5b", "%5c", "%5d", "%5e", "_",
            "%60", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o",
            "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "%7b", "%7c", "%7d", "%7e", "%7f",
            "%c2%80", "%c2%81", "%c2%82", "%c2%83", "%c2%84", "%c2%85", "%c2%86", "%c2%87", "%c2%88", "%c2%89", "%c2%8a", "%c2%8b", "%c2%8c", "%c2%8d", "%c2%8e", "%c2%8f",
            "%c2%90", "%c2%91", "%c2%92", "%c2%93", "%c2%94", "%c2%95", "%c2%96", "%c2%97", "%c2%98", "%c2%99", "%c2%9a", "%c2%9b", "%c2%9c", "%c2%9d", "%c2%9e", "%c2%9f",
            "%c2%a0", "%c2%a1", "%c2%a2", "%c2%a3", "%c2%a4", "%c2%a5", "%c2%a6", "%c2%a7", "%c2%a8", "%c2%a9", "%c2%aa", "%c2%ab", "%c2%ac", "%c2%ad", "%c2%ae", "%c2%af",
            "%c2%b0", "%c2%b1", "%c2%b2", "%c2%b3", "%c2%b4", "%c2%b5", "%c2%b6", "%c2%b7", "%c2%b8", "%c2%b9", "%c2%ba", "%c2%bb", "%c2%bc", "%c2%bd", "%c2%be", "%c2%bf",
            "%c3%80", "%c3%81", "%c3%82", "%c3%83", "%c3%84", "%c3%85", "%c3%86", "%c3%87", "%c3%88", "%c3%89", "%c3%8a", "%c3%8b", "%c3%8c", "%c3%8d", "%c3%8e", "%c3%8f",
            "%c3%90", "%c3%91", "%c3%92", "%c3%93", "%c3%94", "%c3%95", "%c3%96", "%c3%97", "%c3%98", "%c3%99", "%c3%9a", "%c3%9b", "%c3%9c", "%c3%9d", "%c3%9e", "%c3%9f",
            "%c3%a0", "%c3%a1", "%c3%a2", "%c3%a3", "%c3%a4", "%c3%a5", "%c3%a6", "%c3%a7", "%c3%a8", "%c3%a9", "%c3%aa", "%c3%ab", "%c3%ac", "%c3%ad", "%c3%ae", "%c3%af",
            "%c3%b0", "%c3%b1", "%c3%b2", "%c3%b3", "%c3%b4", "%c3%b5", "%c3%b6", "%c3%b7", "%c3%b8", "%c3%b9", "%c3%ba", "%c3%bb", "%c3%bc", "%c3%bd", "%c3%be", "%c3%bf" };
            
            var stringOut = new StringBuilder();

            foreach (char n in stringIn)
            {
                stringOut.Append(conversionTable[n]);
            }

            return stringOut.ToString();
        }
    }
}
