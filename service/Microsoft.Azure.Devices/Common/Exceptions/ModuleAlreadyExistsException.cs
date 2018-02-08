// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if ENABLE_MODULES_SDK
namespace Microsoft.Azure.Devices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

#if !WINDOWS_UWP
    [Serializable]
#endif
    public sealed class ModuleAlreadyExistsException : IotHubException
    {
        public ModuleAlreadyExistsException(string moduleId)
            : this(moduleId, string.Empty)
        {
        }

        public ModuleAlreadyExistsException(string moduleId, string trackingId)
            : base("Module {0} already registered".FormatInvariant(moduleId), trackingId)
        {
        }

        public ModuleAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if !WINDOWS_UWP && !NETSTANDARD1_3
        public ModuleAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
#endif
