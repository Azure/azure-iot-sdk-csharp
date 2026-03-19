// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal sealed partial class Logging : EventSource
    {
        private const int RegisterDeviceId = 12;
        private const int OperationStatusLookupId = 13;

        [NonEvent]
        public static void RegisterDevice(
            object thisOrContextObject,
            string registrationId,
            string idScope,
            string attestationType,
            string operationId,
            TimeSpan? retryAfter,
            string status)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(attestationType);
                DebugValidateArg(retryAfter);

                Log.RegisterDevice(
                IdOf(thisOrContextObject),
                registrationId,
                idScope,
                attestationType,
                operationId,
                (int)(retryAfter?.TotalSeconds ?? 0),
                status);
            }
        }

        [Event(RegisterDeviceId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void RegisterDevice(
            string thisOrContextObject,
            string registrationId,
            string idScope,
            string attestationType,
            string operationId,
            int retryAfterSeconds,
            string status) =>
                WriteEvent(
                    RegisterDeviceId,
                    thisOrContextObject,
                    registrationId,
                    idScope,
                    attestationType,
                    operationId,
                    retryAfterSeconds,
                    status);

        [NonEvent]
        public static void OperationStatusLookup(
            object thisOrContextObject,
            string registrationId,
            string operationId,
            TimeSpan? retryAfter,
            string status,
            int attempts)
        {
            if (IsEnabled)
            {
                DebugValidateArg(thisOrContextObject);
                DebugValidateArg(retryAfter);

                Log.OperationStatusLookup(
                    IdOf(thisOrContextObject),
                    registrationId,
                    operationId,
                    (int)(retryAfter?.TotalSeconds ?? 0),
                    status,
                    attempts);
            }
        }

        [Event(OperationStatusLookupId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void OperationStatusLookup(
            string thisOrContextObject,
            string registrationId,
            string operationId,
            int retryAfterSeconds,
            string status,
            int attempts) =>
                WriteEvent(
                    OperationStatusLookupId,
                    thisOrContextObject,
                    registrationId,
                    operationId,
                    retryAfterSeconds,
                    status,
                    attempts);
    }
}
