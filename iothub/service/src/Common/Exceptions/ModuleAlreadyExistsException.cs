// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
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

#if !NETSTANDARD1_3
        public ModuleAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
