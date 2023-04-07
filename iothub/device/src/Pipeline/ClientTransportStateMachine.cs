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
        Open_Start,
        Open_Success,
        Open_Failure,
        Close_Start,
        Close_Complete,
        Connection_Lost,
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
                    && other._currentState == _currentState && other._nextAction == _nextAction;
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }

        private readonly Dictionary<StateTransition, ClientTransportState> _transitions;
        private readonly object _stateTransitionLock = new();

        internal ClientTransportStateMachine()
        {
            CurrentState = ClientTransportState.Closed;
            _transitions = new Dictionary<StateTransition, ClientTransportState>
            {
                { new StateTransition(ClientTransportState.Closed, ClientStateAction.Open_Start), ClientTransportState.Opening },
                { new StateTransition(ClientTransportState.Opening, ClientStateAction.Open_Success), ClientTransportState.Open },
                { new StateTransition(ClientTransportState.Opening, ClientStateAction.Open_Failure), ClientTransportState.Closed },
                { new StateTransition(ClientTransportState.Opening, ClientStateAction.Close_Start), ClientTransportState.Closing },
                { new StateTransition(ClientTransportState.Open, ClientStateAction.Close_Start), ClientTransportState.Closing },
                { new StateTransition(ClientTransportState.Open, ClientStateAction.Connection_Lost), ClientTransportState.Closed },
                { new StateTransition(ClientTransportState.Closing, ClientStateAction.Close_Complete), ClientTransportState.Closed },
            };
        }

        internal ClientTransportState CurrentState { get; private set; }

        private ClientTransportState GetNextState(ClientStateAction action)
        {
            var transition = new StateTransition(CurrentState, action);

            return _transitions.TryGetValue(transition, out ClientTransportState nextState)
                ? nextState
                : throw new InvalidOperationException("Invalid transition: " + CurrentState + " -> " + action);
        }

        internal ClientTransportState MoveNext(ClientStateAction action)
        {
            lock (_stateTransitionLock)
            {
                CurrentState = GetNextState(action);
                return CurrentState;
            }
        }
    }
}