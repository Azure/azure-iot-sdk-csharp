// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// This class holds the ActivityId that would be set on the thread for ETW during the trace.
    /// </summary>
    internal class EventTraceActivity
    {
        private static EventTraceActivity s_empty;

        public EventTraceActivity()
            : this(Guid.NewGuid())
        {
        }

        public EventTraceActivity(Guid activityId)
        {
            ActivityId = activityId;
        }

        public static string Name => "E2EActivity";

        // this field is passed as reference to native code.
        public Guid ActivityId { get; set; }

        public static EventTraceActivity Empty
        {
            get
            {
                if (s_empty == null)
                {
                    s_empty = new EventTraceActivity(Guid.Empty);
                }

                return s_empty;
            }
        }

        public static EventTraceActivity CreateFromThread()
        {
            Guid id = Trace.CorrelationManager.ActivityId;
            return id == Guid.Empty
                ? Empty
                : new EventTraceActivity(id);
        }
    }
}
