// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.Shared
{
    // TODO:
    [EventSource(Name = "Microsoft-Azure-Devices-Device-Client")]
    internal sealed partial class Logging : EventSource
    {
        private const int CreateId = 20;

        [NonEvent]
        public static void Create(
            object thisOrContextObject)
        {
            DebugValidateArg(thisOrContextObject);

            if (IsEnabled) Log.RegisterAsync(
                IdOf(thisOrContextObject));
        }

        [Event(CreateId, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        private void RegisterAsync(
            string thisOrContextObject) =>
            WriteEvent(CreateId, thisOrContextObject);
    }
}
