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
        [DataRow(ClientTransportState.Closed, ClientStateAction.OpenStart, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.OpenSuccess, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.OpenFailure, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.CloseStart, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Open, ClientStateAction.CloseStart, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Open, ClientStateAction.ConnectionLost, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.CloseComplete, ClientTransportState.Closed)]
        public void ClientTransportStateMachine_AllowedStateTransition(object initialState, object action, object nextState)
        {
            // arrange
            var sut = new ClientTransportStateMachine((ClientTransportState)initialState);

            // act
            Action actual = () => sut.MoveNext((ClientStateAction)action, (ClientTransportState)nextState);

            //assert
            actual.Should().NotThrow();
            sut.GetCurrentState().Should().Be((ClientTransportState)nextState);
        }

        [DataTestMethod]
        [DataRow(ClientTransportState.Closed, ClientStateAction.OpenStart, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.OpenStart, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.OpenStart, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.OpenSuccess, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.OpenSuccess, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.OpenSuccess, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.OpenSuccess, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.OpenFailure, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.OpenFailure, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.OpenFailure, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.OpenFailure, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.CloseStart, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.CloseStart, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.CloseStart, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.CloseStart, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.CloseComplete, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.CloseComplete, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.CloseComplete, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.CloseComplete, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.ConnectionLost, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.ConnectionLost, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.ConnectionLost, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Closed, ClientStateAction.ConnectionLost, ClientTransportState.Closed)]
        public void ClientTransportStateMachine_InvalidStateTransition_FromClosed(object initialState, object action, object nextState)
        {
            // arrange
            var sut = new ClientTransportStateMachine((ClientTransportState)initialState);

            // act
            Action actual = () => sut.MoveNext((ClientStateAction)action, (ClientTransportState)nextState);

            //assert
            actual.Should().Throw<InvalidOperationException>();
        }

        [DataTestMethod]
        [DataRow(ClientTransportState.Opening, ClientStateAction.OpenStart, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.OpenStart, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.OpenStart, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.OpenStart, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.OpenSuccess, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.OpenSuccess, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.OpenSuccess, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.OpenFailure, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.OpenFailure, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.OpenFailure, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.CloseStart, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.CloseStart, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.CloseStart, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.CloseComplete, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.CloseComplete, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.CloseComplete, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.CloseComplete, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.ConnectionLost, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.ConnectionLost, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.ConnectionLost, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Opening, ClientStateAction.ConnectionLost, ClientTransportState.Closed)]
        public void ClientTransportStateMachine_InvalidStateTransition_FromOpening(object initialState, object action, object nextState)
        {
            // arrange
            var sut = new ClientTransportStateMachine((ClientTransportState)initialState);

            // act
            Action actual = () => sut.MoveNext((ClientStateAction)action, (ClientTransportState)nextState);

            //assert
            actual.Should().Throw<InvalidOperationException>();
        }

        [DataTestMethod]
        [DataRow(ClientTransportState.Open, ClientStateAction.OpenStart, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Open, ClientStateAction.OpenStart, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Open, ClientStateAction.OpenStart, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Open, ClientStateAction.OpenStart, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Open, ClientStateAction.OpenSuccess, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Open, ClientStateAction.OpenSuccess, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Open, ClientStateAction.OpenSuccess, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Open, ClientStateAction.OpenSuccess, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Open, ClientStateAction.OpenFailure, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Open, ClientStateAction.OpenFailure, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Open, ClientStateAction.OpenFailure, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Open, ClientStateAction.OpenFailure, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Open, ClientStateAction.CloseStart, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Open, ClientStateAction.CloseStart, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Open, ClientStateAction.CloseStart, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Open, ClientStateAction.CloseComplete, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Open, ClientStateAction.CloseComplete, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Open, ClientStateAction.CloseComplete, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Open, ClientStateAction.CloseComplete, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Open, ClientStateAction.ConnectionLost, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Open, ClientStateAction.ConnectionLost, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Open, ClientStateAction.ConnectionLost, ClientTransportState.Closed)]
        public void ClientTransportStateMachine_InvalidStateTransition_FromOpen(object initialState, object action, object nextState)
        {
            // arrange
            var sut = new ClientTransportStateMachine((ClientTransportState)initialState);

            // act
            Action actual = () => sut.MoveNext((ClientStateAction)action, (ClientTransportState)nextState);

            //assert
            actual.Should().Throw<InvalidOperationException>();
        }

        [DataTestMethod]
        [DataRow(ClientTransportState.Closing, ClientStateAction.OpenStart, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.OpenStart, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.OpenStart, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.OpenStart, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.OpenSuccess, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.OpenSuccess, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.OpenSuccess, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.OpenSuccess, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.OpenFailure, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.OpenFailure, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.OpenFailure, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.OpenFailure, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.CloseStart, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.CloseStart, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.CloseStart, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.CloseStart, ClientTransportState.Closed)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.CloseComplete, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.CloseComplete, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.CloseComplete, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.ConnectionLost, ClientTransportState.Opening)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.ConnectionLost, ClientTransportState.Open)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.ConnectionLost, ClientTransportState.Closing)]
        [DataRow(ClientTransportState.Closing, ClientStateAction.ConnectionLost, ClientTransportState.Closed)]
        public void ClientTransportStateMachine_InvalidStateTransition_FromClosing(object initialState, object action, object nextState)
        {
            // arrange
            var sut = new ClientTransportStateMachine((ClientTransportState)initialState);

            // act
            Action actual = () => sut.MoveNext((ClientStateAction)action, (ClientTransportState)nextState);

            //assert
            actual.Should().Throw<InvalidOperationException>();
        }
    }
}
