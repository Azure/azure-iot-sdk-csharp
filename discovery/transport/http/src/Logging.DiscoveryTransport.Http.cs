// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.Shared
{
    // d209b8a1-2e02-5724-f341-677227d0ed22
    [EventSource(Name = "Microsoft-Azure-Devices-Discovery-Transport-Http")]
    internal sealed partial class Logging : EventSource
    {
        private const int IssueChallengeId = 12;
        private const int GetOnboardingInfoId = 13;

        [NonEvent]
        public static void IssueChallenge(
            object thisOrContextObject,
            string registrationId)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);

                Log.IssueChallenge(
                IdOf(thisOrContextObject),
                registrationId);
            }
        }

        [Event(IssueChallengeId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void IssueChallenge(
            string thisOrContextObject,
            string registrationId) =>
                WriteEvent(
                    IssueChallengeId,
                    thisOrContextObject,
                    registrationId);

        [NonEvent]
        public static void GetOnboardingInfo(
            object thisOrContextObject,
            string registrationId)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);

                Log.GetOnboardingInfo(
                IdOf(thisOrContextObject),
                registrationId);
            }
        }

        [Event(GetOnboardingInfoId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void GetOnboardingInfo(
            string thisOrContextObject,
            string registrationId) =>
                WriteEvent(
                    GetOnboardingInfoId,
                    thisOrContextObject,
                    registrationId);
    }
}
