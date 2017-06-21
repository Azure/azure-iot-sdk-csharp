// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    
    public class DeviceClientConnectionStatusManager
    {
        object lockObject = new Object();

        volatile ConnectionStatus status = ConnectionStatus.Disabled;
        
        private readonly Dictionary<string, Tuple<ConnectionStatus, CancellationTokenSource>> connections;

        public DeviceClientConnectionStatusManager()
        {
            connections = new Dictionary<string, Tuple<ConnectionStatus, CancellationTokenSource>>();
        }

        public ConnectionStatus State => this.status;
        
        public ConnectionStatusChangeResult ChangeTo(string connectionKey, ConnectionStatus toState, ConnectionStatus? fromState = null, CancellationTokenSource cancellationTokenSource = null)
        {
            if (toState == ConnectionStatus.Disabled)
            {
                return Disable(connectionKey, true);
            }
            
            Tuple<ConnectionStatus, CancellationTokenSource> connectionValue;
            if (toState == ConnectionStatus.Disconnected_Retrying)
            {
                if (cancellationTokenSource == null)
                {
                    throw new ArgumentNullException($"{nameof(cancellationTokenSource)} should be provided for retrying state.");
                }
                connectionValue = Tuple.Create(toState, cancellationTokenSource);
            }
            else
            {
                connectionValue = new Tuple<ConnectionStatus, CancellationTokenSource>(toState, null);
            }

            var changeResult = new ConnectionStatusChangeResult
            {
                ClientStatus = this.status
            };

            lock (lockObject)
            {
                if (connections.ContainsKey(connectionKey))
                {
                    ConnectionStatus existingConnectionState = connections[connectionKey].Item1;
                    if (((existingConnectionState != ConnectionStatus.Disconnected && existingConnectionState != ConnectionStatus.Disabled) || toState == ConnectionStatus.Connected) && 
                        (!fromState.HasValue || fromState.Value == existingConnectionState))
                    {
                        connections[connectionKey] = connectionValue;
                        changeResult.IsConnectionStatusChanged = true;
                    }
                }
                else
                {
                    if (toState == ConnectionStatus.Connected && (!fromState.HasValue || fromState.Value == ConnectionStatus.Disconnected))
                    {
                        connections.Add(connectionKey, connectionValue);
                        changeResult.IsConnectionStatusChanged = true;
                    }
                }

                if (changeResult.IsConnectionStatusChanged)
                {
                    Tuple<ConnectionStatus, ConnectionStatus> beforeAndAfterState = UpdateDeviceClientState();
                    changeResult.IsClientStatusChanged = beforeAndAfterState.Item1 != beforeAndAfterState.Item2;
                    changeResult.ClientStatus = beforeAndAfterState.Item2;
                }
            }

            return changeResult;
        }

        public void DisableAllConnections()
        {
            lock (lockObject)
            {
                List<string> connectionKeys = new List<string>(connections.Keys);
                foreach (string connectionKey in connectionKeys)
                {
                    Disable(connectionKey, false);
                }
            }
            this.status = ConnectionStatus.Disabled;
        }

        private ConnectionStatusChangeResult Disable(string connectionKey, bool updateState)
        {
            var changeResult = new ConnectionStatusChangeResult();

            lock (lockObject)
            {
                if (connections.ContainsKey(connectionKey))
                {
                    Tuple<ConnectionStatus, CancellationTokenSource>  previousConnectionValue = connections[connectionKey];
                    connections[connectionKey] = new Tuple<ConnectionStatus, CancellationTokenSource>(ConnectionStatus.Disabled, null);
                    changeResult.IsConnectionStatusChanged = true;

                    if (updateState)
                    {
                        Tuple<ConnectionStatus, ConnectionStatus> beforeAndAfterState = UpdateDeviceClientState();
                        changeResult.IsClientStatusChanged = beforeAndAfterState.Item1 != beforeAndAfterState.Item2;
                        changeResult.ClientStatus = beforeAndAfterState.Item2;
                    }
                    
                    if (previousConnectionValue.Item2 != null)
                    {
                        previousConnectionValue.Item2.Cancel();
                    }
                }
            }

            return changeResult;
        }

        private Tuple<ConnectionStatus, ConnectionStatus> UpdateDeviceClientState()
        {
            ConnectionStatus currentState = (ConnectionStatus)this.status;
            ConnectionStatus combinedState = ConnectionStatus.Disabled;
            
            foreach (Tuple<ConnectionStatus, CancellationTokenSource> connectionValue in connections.Values)
            {
                combinedState = combinedState | connectionValue.Item1;
            }

            if (combinedState.HasFlag(ConnectionStatus.Disconnected))
            {
                this.status = ConnectionStatus.Disconnected;
            }
            else if (combinedState.HasFlag(ConnectionStatus.Disconnected_Retrying))
            {
                this.status = ConnectionStatus.Disconnected_Retrying;
            }
            else if (combinedState.HasFlag(ConnectionStatus.Connected))
            {
                this.status = ConnectionStatus.Connected;
            }
            else
            {
                this.status = ConnectionStatus.Disabled;
            }

            return Tuple.Create(currentState, (ConnectionStatus)this.status);
        }
    }
}
