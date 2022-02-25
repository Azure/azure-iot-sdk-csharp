// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Tests
{
    internal class CustomClientProperty
    {
        // The properties in here need to be public otherwise NewtonSoft.Json cannot serialize and deserialize them properly.
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
