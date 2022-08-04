// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices
{
    // e927240b-7198-5cc8-72f1-7ddcf31bb8cb
    [EventSource(Name = "Microsoft-Azure-Devices-Provisioning-Client")]
    internal sealed partial class Logging : EventSource
    {
        private const int RegisterAsyncId = 11;

        [NonEvent]
        public static void RegisterAsync(
            object thisOrContextObject,
            string globalDeviceEndpoint,
            string idScope,
            object options,
            object security)
        {
            DebugValidateArg(thisOrContextObject);
            DebugValidateArg(security);
            DebugValidateArg(options);

            if (IsEnabled)
            {
                Log.RegisterAsync(
                IdOf(thisOrContextObject),
                globalDeviceEndpoint,
                idScope,
                IdOf(options),
                IdOf(security));
            }
        }

        [Event(RegisterAsyncId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void RegisterAsync(
            string thisOrContextObject,
            string globalDeviceEndpoint,
            string idScope,
            string options,
            string security) =>
            WriteEvent(RegisterAsyncId, thisOrContextObject, globalDeviceEndpoint, idScope, options, security);
    }
}
