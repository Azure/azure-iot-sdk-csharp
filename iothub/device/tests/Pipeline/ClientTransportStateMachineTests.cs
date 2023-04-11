// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ClientTransportStateMachineTests
    {
        [DataTestMethod]
        [DataRow(ClientStateAction.OpenStart, ClientTransportState.Opening)]
        public void ClientTransportStateMachine_AllowedStateTransition(object action, object nextState)
        {
            // arrange
            var sut = new ClientTransportStateMachine();

            // act and assert
            Action actual = () => sut.MoveNext((ClientStateAction)action, (ClientTransportState)nextState);
            actual.Should().NotThrow();
        }
    }
}
