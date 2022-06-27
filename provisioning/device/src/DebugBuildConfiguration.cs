// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if DEBUG
[assembly: InternalsVisibleTo("Microsoft.Azure.Devices.Provisioning.Transport.Amqp.Tests")]
[assembly: InternalsVisibleTo("Microsoft.Azure.Devices.Provisioning.Transport.Mqtt.Tests")]
[assembly: InternalsVisibleTo("Microsoft.Azure.Devices.Provisioning.Transport.Http.Tests")]
#endif
