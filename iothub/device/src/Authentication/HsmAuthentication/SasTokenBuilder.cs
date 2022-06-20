// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication
{
    internal static class SasTokenBuilder
    {
        public static string BuildSasToken(string audience, string signature, string expiry)
        {
            // Example returned string:
            // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]

            var buffer = new StringBuilder();
            buffer.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0} {1}={2}&{3}={4}&{5}={6}",
                SharedAccessSignatureConstants.SharedAccessSignature,
                SharedAccessSignatureConstants.AudienceFieldName,
                audience,
                SharedAccessSignatureConstants.SignatureFieldName,
                WebUtility.UrlEncode(signature),
                SharedAccessSignatureConstants.ExpiryFieldName,
                WebUtility.UrlEncode(expiry));

            return buffer.ToString();
        }

        public static string BuildExpiresOn(DateTime startTime, TimeSpan timeToLive)
        {
            DateTime expiresOn = startTime.Add(timeToLive);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            long seconds = Convert.ToInt64(secondsFromBaseTime.TotalSeconds, CultureInfo.InvariantCulture);
            return Convert.ToString(seconds, CultureInfo.InvariantCulture);
        }

        public static string BuildAudience(string iotHub, string deviceId, string moduleId)
        {
            // DeviceId and ModuleId need to be double encoded.
            string audience = WebUtility.UrlEncode(
                "{0}/devices/{1}/modules/{2}".FormatInvariant(
                    iotHub,
                    WebUtility.UrlEncode(deviceId),
                    WebUtility.UrlEncode(moduleId)));

            return audience;
        }
    }
}
