// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class FxTests
    {
        [TestMethod]
        public void IotHubDeviceClient_FatalException()
        {
            OutOfMemoryException exception = new();
            Fx.IsFatal(exception).Should().BeTrue();

            SEHException sEHException = new();
            Fx.IsFatal(sEHException).Should().BeTrue();

            NullReferenceException nullReferenceException = new();
            Fx.IsFatal(nullReferenceException).Should().BeTrue();

            TypeInitializationException typeInitializationException = new("name", new OutOfMemoryException());
            Fx.IsFatal(typeInitializationException).Should().BeTrue();

            TargetInvocationException targetInvocationException = new(new OutOfMemoryException());
            Fx.IsFatal(targetInvocationException).Should().BeTrue();

            AggregateException aggregateException = new("error", new OutOfMemoryException());
            Fx.IsFatal(aggregateException).Should().BeTrue();
        }

        [TestMethod]
        public void IotHubDeviceClient_NonFatalException()
        {
            AggregateException aggregateException = new("error", new ArgumentException());
            Fx.IsFatal(aggregateException).Should().BeFalse();

            ArgumentOutOfRangeException rangeException = new();
            Fx.IsFatal(rangeException).Should().BeFalse();

            InvalidOperationException operationException = new();
            Fx.IsFatal(operationException).Should().BeFalse();
        }
    }
}