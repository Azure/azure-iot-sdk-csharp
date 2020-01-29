// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNetty.Common.Utilities;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class MessageSendE2ETests
    {
        [IotHubFact]
        public void iothubTest()
        {
            throw new IllegalReferenceCountException(1);
        }

        [ProvisioningFact]
        public void provisioningTest()
        {
            throw new IllegalReferenceCountException(1);
        }
    }
}
