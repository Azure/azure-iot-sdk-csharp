// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.Shared
{
	[EventSource(Name = "Microsoft-Azure-Devices-Provisioning-Client")]
    internal sealed partial class Logging : EventSource
    {
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

            if (IsEnabled) Log.RegisterAsync(
                IdOf(thisOrContextObject), 
                globalDeviceEndpoint, 
                idScope,
                IdOf(transport),
                IdOf(security));
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
    	public static void RegisterDevice(
            object thisOrContextObject,
            string registrationId, 
            string idScope,
            string attestationType, 
            string operationId, 
            TimeSpan? retryAfter, 
            string status)
    	{
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(attestationType);
            DebugValidateArg(retryAfter);
            if (IsEnabled) Log.RegisterDevice(
                IdOf(thisOrContextObject),
                registrationId,
                idScope,
                attestationType,
                operationId,
                (int)(retryAfter?.TotalSeconds),
                status);
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
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(retryAfter);

            if (IsEnabled) Log.OperationStatusLookup(
                IdOf(thisOrContextObject),
                registrationId,
                operationId,
                (int)(retryAfter?.TotalSeconds),
                status,
                attempts);
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
