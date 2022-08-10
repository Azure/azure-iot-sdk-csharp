// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Microsoft.Azure.Devices
{
    internal class Utils
    {
        public static string GetClientVersion()
        {
            var a = Assembly.GetExecutingAssembly();
            var attribute = (AssemblyInformationalVersionAttribute)a
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), true)[0];
            return a.GetName().Name + "/" + attribute.InformationalVersion;
        }

        public static void ValidateBufferBounds(byte[] buffer, int offset, int size)
        {
            Argument.RequireNotNull(buffer, nameof(buffer));

            if (offset < 0 || offset > buffer.Length || size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            int remainingBufferSpace = buffer.Length - offset;
            if (size > remainingBufferSpace)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }
        }
    }
}
