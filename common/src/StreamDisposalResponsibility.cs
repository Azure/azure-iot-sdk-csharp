// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Shared
{
    // Enum that indicates if the Stream used to initialize an IoT hub message
    // should be disposed by the application or by the SDK.
    internal enum StreamDisposalResponsibility
    {
        App,
        Sdk
    }
}
