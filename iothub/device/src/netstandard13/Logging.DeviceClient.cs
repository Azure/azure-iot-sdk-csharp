// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Text;

namespace Microsoft.Azure.Devices.Shared
{
    // ddbee999-a79e-5050-ea3c-6d1a8a7bafdd
    [EventSource(Name = "Microsoft-Azure-Devices-Device-Client")]
    internal sealed partial class Logging : EventSource
    {
        private const int CreateId = 20;
        private const int GenerateTokenId = 21;

        [NonEvent]
        public static void CreateFromConnectionString(
            object thisOrContextObject, 
            string iotHubConnectionString,
            ITransportSettings[] transportSettings)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(iotHubConnectionString);

            if (IsEnabled)
            {
                var sb = new StringBuilder();
                foreach (ITransportSettings transport in transportSettings)
                {
                    sb.Append(transport.GetTransportType().ToString());
                }

                Log.CreateFromConnectionString(
                    IdOf(thisOrContextObject),
                    iotHubConnectionString,
                    sb.ToString());

            }
        }

        [NonEvent]
        public static void GenerateToken(
            object thisOrContextObject,
            DateTime expirationDateTime,
            long epochTime)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(expirationDateTime);
            DebugValidateArg(epochTime);

            Log.CreateFromConnectionString(
                IdOf(thisOrContextObject),
                DateTime.Now.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                expirationDateTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                epochTime);
        }

        [Event(CreateId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void CreateFromConnectionString(
            string thisOrContextObject,
            string iotHubConnectionString,
            string transportSettingsString) =>
            WriteEvent(CreateId, thisOrContextObject, iotHubConnectionString, transportSettingsString);

        [Event(GenerateTokenId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void GenerateToken(
            string thisOrContextObject,
            string currentDateTime,
            string expirationDateTime,
            long epochTime) =>
            WriteEvent(GenerateTokenId, thisOrContextObject, currentDateTime, expirationDateTime, epochTime);
    }
}
