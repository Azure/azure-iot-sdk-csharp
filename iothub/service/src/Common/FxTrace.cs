// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common
{
    internal static class FxTrace
    {
        private const string EventSourceName = "Microsoft.IotHub";
        private static ExceptionTrace s_exceptionTrace;

        public static ExceptionTrace Exception
        {
            get
            {
                if (s_exceptionTrace == null)
                {
                    // don't need a lock here since a true singleton is not required
                    s_exceptionTrace = new ExceptionTrace(EventSourceName);
                }

                return s_exceptionTrace;
            }
        }
    }
}
