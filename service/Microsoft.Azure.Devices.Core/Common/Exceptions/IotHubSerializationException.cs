// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

#if !WINDOWS_UWP
    
#endif
    public class IotHubSerializationException : IotHubException
    {
        public IotHubSerializationException(string message)
            : base(message)
        {
        }
    }
}
