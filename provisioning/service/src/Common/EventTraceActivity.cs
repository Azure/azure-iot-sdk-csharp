// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.Common
{
    /// <summary>
    /// This class holds the ActivityId that would be set on the thread for ETW during the trace. 
    /// </summary>
    internal class EventTraceActivity
    {
        private static EventTraceActivity _empty;

        public EventTraceActivity()
            : this(Guid.NewGuid())
        {
        }

        public EventTraceActivity(Guid activityId)
        {
            this.ActivityId = activityId;
        }

        public static EventTraceActivity Empty
        {
            get
            {
                if (_empty == null)
                {
                    _empty = new EventTraceActivity(Guid.Empty);
                }

                return _empty;
            }
        }

        public static string Name
        {
            get { return "E2EActivity"; }
        }

        // this field is passed as reference to native code. 
        public Guid ActivityId;

#if !WINDOWS_UWP && !NETSTANDARD1_3
        public static EventTraceActivity CreateFromThread()
        {
            Guid id = Trace.CorrelationManager.ActivityId;
            if (id == Guid.Empty)
            {
                return EventTraceActivity.Empty;
            }

            return new EventTraceActivity(id);
        }
#endif
    }
}
