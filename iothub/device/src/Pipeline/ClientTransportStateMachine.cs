// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal enum ClientTransportState
    {
        Closed,
        Opening,
        Open,
        Closing,
    }

    internal enum ClientStateAction
    {
        OpenStart,
        OpenSuccess,
        OpenFailure,
        CloseStart,
        CloseComplete,
        ConnectionLost,
    }

    internal class ClientTransportStateMachine
    {
        internal class StateTransition
        {
            private readonly ClientTransportState _currentState;
            private readonly ClientStateAction _nextAction;

            internal StateTransition(ClientTransportState state, ClientStateAction action)
            {
                _currentState = state;
                _nextAction = action;
            }

            public override bool Equals(object obj)
            {
                return obj is StateTransition other
                    && other._currentState == _currentState
                    && other._nextAction == _nextAction;
            }

            public override int GetHashCode()
            {
                return ShiftAndWrap(_currentState.GetHashCode(), 2) ^ _nextAction.GetHashCode();
            }

            // Hashcode evaluation example: https://learn.microsoft.com/dotnet/api/system.object.gethashcode?view=net-8.0#examples
            private static int ShiftAndWrap(int value, int positions)
            {
                positions &= 0x1F;

                // Save the existing bit pattern, but interpret it as an unsigned integer.
                uint number = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
                // Preserve the bits to be discarded.
                uint wrapped = number >> (32 - positions);
                // Shift and wrap the discarded bits.
                return BitConverter.ToInt32(BitConverter.GetBytes((number << positions) | wrapped), 0);
            }
        }

        private static readonly Dictionary<StateTransition, ClientTransportState> s_transitions = new()
        {
                { new StateTransition(ClientTransportState.Closed, ClientStateAction.OpenStart), ClientTransportState.Opening },
                { new StateTransition(ClientTransportState.Opening, ClientStateAction.OpenSuccess), ClientTransportState.Open },
                { new StateTransition(ClientTransportState.Opening, ClientStateAction.OpenFailure), ClientTransportState.Closed },
                { new StateTransition(ClientTransportState.Opening, ClientStateAction.CloseStart), ClientTransportState.Closing },
                { new StateTransition(ClientTransportState.Open, ClientStateAction.CloseStart), ClientTransportState.Closing },
                { new StateTransition(ClientTransportState.Open, ClientStateAction.ConnectionLost), ClientTransportState.Opening },
                { new StateTransition(ClientTransportState.Closing, ClientStateAction.CloseComplete), ClientTransportState.Closed },
            };

        private readonly object _stateTransitionLock = new();
        private ClientTransportState _currentState = ClientTransportState.Closed;

        internal ClientTransportState GetCurrentState()
        {
            lock (_stateTransitionLock)
            {
                return _currentState;
            }
        }

        internal void MoveNext(ClientStateAction action)
        {
            lock (_stateTransitionLock)
            {
                _currentState = GetNextState(action);
            }
        }

        private ClientTransportState GetNextState(ClientStateAction action)
        {
            var transition = new StateTransition(_currentState, action);

            return s_transitions.TryGetValue(transition, out ClientTransportState nextState)
                ? nextState
                : throw new InvalidOperationException($"Invalid transition: {_currentState} -> {action}.");
        }
    }
}