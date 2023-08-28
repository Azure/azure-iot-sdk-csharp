// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.Shared
{
    // e927240b-7198-5cc8-72f1-7ddcf31bb8cb
    [EventSource(Name = "Microsoft-Azure-Devices-Discovery-Client")]
    internal sealed partial class Logging : EventSource
    {
        private const int IssueChallengeAsyncId = 15;
        private const int GetOnboardingInfoAsyncId = 16;

        [NonEvent]
        public static void IssueChallengeAsync(
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
                Log.IssueChallengeAsync(
                IdOf(thisOrContextObject),
                globalDeviceEndpoint,
                IdOf(transport),
                IdOf(security));
            }
        }

        [Event(IssueChallengeAsyncId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void IssueChallengeAsync(
            string thisOrContextObject,
            string globalDeviceEndpoint,
            string transport,
            string security) =>
            WriteEvent(IssueChallengeAsyncId, thisOrContextObject, globalDeviceEndpoint, transport, security);

        [NonEvent]
        public static void GetOnboardingInfoAsync(
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
                Log.GetOnboardingInfoAsync(
                IdOf(thisOrContextObject),
                globalDeviceEndpoint,
                IdOf(transport),
                IdOf(security));
            }
        }

        [Event(GetOnboardingInfoAsyncId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void GetOnboardingInfoAsync(
            string thisOrContextObject,
            string globalDeviceEndpoint,
            string transport,
            string security) =>
            WriteEvent(GetOnboardingInfoAsyncId, thisOrContextObject, globalDeviceEndpoint, transport, security);
    }
}
