// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if DEBUG
[assembly: InternalsVisibleTo("Microsoft.Azure.Devices.Client.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // for Moq
#endif
