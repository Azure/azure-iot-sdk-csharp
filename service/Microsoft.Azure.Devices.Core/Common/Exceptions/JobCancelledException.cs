// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    using System;

#if !WINDOWS_UWP
    
#endif
    public sealed class JobCancelledException : IotHubException
    {
        public JobCancelledException()
            : base("Job has been cancelled")
        {

        }
    }
}
