// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// The different client transport states that are used for the delegating pipeline state management.
    /// </summary>
    internal enum ClientTransportState
    {
        Closed,
        Opening,
        Open,
        Closing,
    }

    /// <summary>
    /// The different actions that are executed on the delegating pipeline.
    /// </summary>
    internal enum ClientStateAction
    {
        OpenStart,
        OpenSuccess,
        OpenFailure,
        CloseStart,
        CloseComplete,
        ConnectionLost,
    }

    /// <summary>
    /// This state machine stores the current state of the delegating pipeline and evalautes if the requested state transition is possible.
    /// </summary>
    internal class ClientTransportStateMachine
    {
        /// <summary>
        /// This internal class is created for the purpose of evaluating if the proposed state transition is acceptable.
        /// Objects of this class are maintained in a dictionary of "acceptable" state transitions.
        /// </summary>
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

            // Hashcode evaluation example: https://learn.microsoft.com/dotnet/api/system.object.gethashcode?view=net-8.0#examples
            public override int GetHashCode()
            {
                return ShiftAndWrap(_currentState.GetHashCode(), 2) ^ _nextAction.GetHashCode();
            }

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

        // Acceptable state transitions
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

        internal ClientTransportStateMachine()
        {
            _currentState = ClientTransportState.Closed;
        }

# if DEBUG
        // This is added only for the purpose of unit-testing.
        // SDK code shouldn't be relying on this constructor, hence this is inside a debug block.
        internal ClientTransportStateMachine(ClientTransportState initialState)
        {
            _currentState = initialState;
        }
#endif

        // Return the current state, managed by a lock
        internal ClientTransportState GetCurrentState()
        {
            lock (_stateTransitionLock)
            {
                return _currentState;
            }
        }

        /// <summary>
        /// Transition to the desired state, if possible.
        /// This operation is performed under a lock to ensure state is not changed while the operation is in progress.
        /// </summary>
        /// <param name="action">The action executed on the delegating pipeline.</param>
        /// <param name="desiredState">The desired state to transition the delegating pipeline to.</param>
        /// <exception cref="InvalidOperationException">The requested state transition is not a valid transition.</exception>
        internal void MoveNext(ClientStateAction action, ClientTransportState desiredState)
        {
            lock (_stateTransitionLock)
            {
                ClientTransportState previousState = _currentState;
                _currentState = GetNextState(action, desiredState);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Client transport state changed from {previousState} -> {_currentState} because {action}.", nameof(MoveNext));
            }
        }

        private ClientTransportState GetNextState(ClientStateAction action, ClientTransportState desiredState)
        {
            var transition = new StateTransition(_currentState, action);
            bool transitionPossible = s_transitions.TryGetValue(transition, out ClientTransportState nextState);

            return transitionPossible
                ? nextState == desiredState
                    ? nextState
                    : throw new InvalidOperationException($"Invalid transition requested. {_currentState} -> {action} results in {nextState} and not {desiredState}.")
                : throw new InvalidOperationException($"Invalid transition: {_currentState} -> {action}.");
        }
    }
}