// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices.Client
{
    internal class IotHubClientDiagnostic
    {
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private const string DiagnosticCreationTimeUtcKey = "creationtimeutc";
        private static readonly DateTime Dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static bool AddDiagnosticInfoIfNecessary(MessageBase message, int diagnosticSamplingPercentage, ref int currentMessageCount)
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

        internal static bool HasDiagnosticProperties(MessageBase message)
        {
            return message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagId) && message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagCorrelationContext);
        }

        private static string GenerateEightRandomCharacters()
        {
            char[] stringChars = new char[8];
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
