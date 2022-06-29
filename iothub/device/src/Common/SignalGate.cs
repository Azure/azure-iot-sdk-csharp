// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Microsoft.Azure.Devices.Client
{
    [Fx.Tag.SynchronizationPrimitive(Fx.Tag.BlocksUsing.NonBlocking)]
    //TODO: 171524 - Across remoting boundary Serializable is not sufficient, and requires AsyncResult that derives from MarshalByRefObject.
    [Serializable]
    internal class SignalGate
    {
        [Fx.Tag.SynchronizationObject(Blocking = false, Kind = Fx.Tag.SynchronizationKind.InterlockedNoSpin)]
        private int _state;

        internal bool IsLocked => _state == GateState.Locked;

        internal bool IsSignalled => _state == GateState.Signalled;

        // Returns true if this brings the gate to the Signalled state.
        // Transitions - Locked -> SignalPending | Completed before it was unlocked
        //               Unlocked -> Signaled
        public bool Signal()
        {
            int lastState = _state;

            if (lastState == GateState.Locked)
            {
                lastState = Interlocked.CompareExchange(ref _state, GateState.SignalPending, GateState.Locked);
            }

            if (lastState == GateState.Unlocked)
            {
                _state = GateState.Signalled;
                return true;
            }

            if (lastState != GateState.Locked)
            {
                ThrowInvalidSignalGateState();
            }

            return false;
        }

        // Returns true if this brings the gate to the Signalled state.
        // Transitions - SignalPending -> Signaled | return the AsyncResult since the callback already
        //                                         | completed and provided the result on its thread
        //               Locked -> Unlocked
        public bool Unlock()
        {
            int lastState = _state;

            if (lastState == GateState.Locked)
            {
                lastState = Interlocked.CompareExchange(ref _state, GateState.Unlocked, GateState.Locked);
            }

            if (lastState == GateState.SignalPending)
            {
                _state = GateState.Signalled;
                return true;
            }

            if (lastState != GateState.Locked)
            {
                ThrowInvalidSignalGateState();
            }

            return false;
        }

        // This is factored out to allow Signal and Unlock to be inlined.
        private static void ThrowInvalidSignalGateState()
        {
            throw Fx.Exception.AsError(new InvalidOperationException(CommonResources.InvalidSemaphoreExit));
        }

        private static class GateState
        {
            public const int Locked = 0;
            public const int SignalPending = 1;
            public const int Unlocked = 2;
            public const int Signalled = 3;
        }
    }

    [Fx.Tag.SynchronizationPrimitive(Fx.Tag.BlocksUsing.NonBlocking)]
    [Serializable]
    internal class SignalGateT<T> : SignalGate
    {
        private T Result { get; set; }

        public bool Signal(T result)
        {
            Result = result;
            return Signal();
        }

        public bool Unlock(out T result)
        {
            if (Unlock())
            {
                result = Result;
                return true;
            }

            result = default;
            return false;
        }
    }
}
