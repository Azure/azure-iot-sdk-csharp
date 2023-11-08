// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    public class ArcDeviceResource : AzureResource
    {
        public class ArcDeviceResourceProperties
        {
            public string osName { get; set; } = "HCI";
        }
        public ArcDeviceResourceProperties properties { get; set; } = new ArcDeviceResourceProperties();
        public string kind { get; set; } = "EPS";
    }
}
