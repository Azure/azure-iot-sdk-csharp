// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    using QuotaExceededException = Microsoft.ServiceBus.Messaging.QuotaExceededException;

    // EventHubListener Platform Adaptation Layer for .NET Framework.
    // This is using the WindowsAzure.ServiceBus NuGet dependency.
    public partial class EventHubTestListener
    {
        
    }
}
