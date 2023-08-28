// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.Shared
{
    // e927240b-7198-5cc8-72f1-7ddcf31bb8cb
    [EventSource(Name = "Microsoft-Azure-Devices-Provisioning-Client")]
    internal sealed partial class Logging : EventSource
    {
        private const int RegisterAsyncId = 11;
        private const int OnboardAsyncId = 12;

        [NonEvent]
        public static void RegisterAsync(
            object thisOrContextObject,
            string globalDeviceEndpoint,
            string idScope,
            object transport,
            object security)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(security);
            DebugValidateArg(transport);

            if (IsEnabled)
            {
                Log.RegisterAsync(
                IdOf(thisOrContextObject),
                globalDeviceEndpoint,
                idScope,
                IdOf(transport),
                IdOf(security));
            }
        }

        [Event(RegisterAsyncId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void RegisterAsync(
            string thisOrContextObject,
            string globalDeviceEndpoint,
            string idScope,
            string transport,
            string security) =>
            WriteEvent(RegisterAsyncId, thisOrContextObject, globalDeviceEndpoint, idScope, transport, security);

        [NonEvent]
        public static void OnboardAsync(
            object thisOrContextObject,
            string globalDeviceEndpoint,
            object transport,
            object security)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(security);
            DebugValidateArg(transport);

            if (IsEnabled)
            {
                Log.OnboardAsync(
                IdOf(thisOrContextObject),
                globalDeviceEndpoint,
                IdOf(transport),
                IdOf(security));
            }
        }

        [Event(OnboardAsyncId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void OnboardAsync(
            string thisOrContextObject,
            string globalDeviceEndpoint,
            string transport,
            string security) =>
            WriteEvent(OnboardAsyncId, thisOrContextObject, globalDeviceEndpoint, transport, security);
    }
}
