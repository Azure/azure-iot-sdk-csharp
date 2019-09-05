// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public abstract class DeviceClientScenario : PerfScenario
    {
        private const int AmqpPrefetchCount = 100;

        private DeviceClient _dc;

        // Shared by Create, Open and Send
        private TelemetryMetrics _m = new TelemetryMetrics();
        private Stopwatch _sw = new Stopwatch();

        // Separate metrics and time calculation for operations that can be parallelized.
        private TelemetryMetrics _mRecv = new TelemetryMetrics();
        private Stopwatch _swRecv = new Stopwatch();

        private const string TestMethodName = "PerfTestMethod";
        private TelemetryMetrics _mMethod = new TelemetryMetrics();
        private Stopwatch _swMethod = new Stopwatch();
        private SemaphoreSlim _methodSemaphore = new SemaphoreSlim(0);
        private static readonly MethodResponse s_methodResponse = new MethodResponse(200);

        private TelemetryMetrics _mConnectionStatus = new TelemetryMetrics();
        private SemaphoreSlim _connectionStatusChangedSemaphore = new SemaphoreSlim(0);
        private SemaphoreSlim _waitForDisconnectSemaphore = new SemaphoreSlim(0);
        private bool _connected;

        private byte[] _messageBytes;

        private bool _pooled;
        private int _poolSize;

        public DeviceClientScenario(PerfScenarioConfig config) : base(config)
        {
            _m.Id = _id;

            _mRecv.Id = _id;
            _mRecv.OperationType = TelemetryMetrics.DeviceOperationReceive;

            _mMethod.Id = _id;

            _mConnectionStatus.Id = _id;
            _mConnectionStatus.ExecuteTime = null;
            _mConnectionStatus.ScheduleTime = null;

            _messageBytes = new byte[_sizeBytes];

            _pooled = config.PoolSize > 0;
            if (_pooled) _poolSize = config.PoolSize;

            BitConverter.TryWriteBytes(_messageBytes, _id);
        }

        protected async Task CreateDeviceAsync()
        {
            _sw.Restart();
            _m.OperationType = TelemetryMetrics.DeviceOperationCreate;

            ITransportSettings transportSettings = null;

            if (_pooled && ((_transport == Client.TransportType.Amqp_Tcp_Only) || (_transport == Client.TransportType.Amqp_WebSocket_Only)))
            {
                transportSettings = new AmqpTransportSettings(
                    _transport,
                    AmqpPrefetchCount,
                    new AmqpConnectionPoolSettings()
                    {
                        Pooling = true,
                        MaxPoolSize = (uint)_poolSize,
                    });
            }

            if (_authType == "sas")
            {
                if (transportSettings == null)
                {
                    _dc = DeviceClient.CreateFromConnectionString(Configuration.Stress.GetConnectionStringById(_id, _authType), _transport);
                }
                else
                {
                    _dc = DeviceClient.CreateFromConnectionString(Configuration.Stress.GetConnectionStringById(_id, _authType), new ITransportSettings[] { transportSettings });
                }
            }
            else if (_authType == "x509")
            {
                _dc = DeviceClient.Create(
                    Configuration.Stress.Endpoint,
                    new DeviceAuthenticationWithX509Certificate(
                        Configuration.Stress.GetDeviceNameById(_id, _authType),
                        Configuration.Stress.Certificate),
                    _transport);
            }
            else
            {
                throw new NotImplementedException($"Not implemented for authType {_authType}");
            }

            _dc.SetConnectionStatusChangesHandler(OnConnectionStatusChanged);

            _m.ExecuteTime = _sw.ElapsedMilliseconds;
            _m.ScheduleTime = null; // sync operation
            await _writer.WriteAsync(_m).ConfigureAwait(false);
        }

        protected void DisableRetry()
        {
            _dc.SetRetryPolicy(new NoRetry());
        }

        private async void OnConnectionStatusChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            try
            {
                await _connectionStatusChangedSemaphore.WaitAsync().ConfigureAwait(false);

                switch (status)
                {
                    case ConnectionStatus.Disconnected:
                        if (_connected)
                        {
                            SystemMetrics.DeviceDisconnected();
                            _connected = false;
                        }

                        _mConnectionStatus.OperationType = TelemetryMetrics.DeviceStateDisconnected;
                        _waitForDisconnectSemaphore.Release();
                        break;
                    case ConnectionStatus.Connected:
                        if (!_connected)
                        {
                            SystemMetrics.DeviceConnected();
                            _connected = true;
                        }

                        _mConnectionStatus.OperationType = TelemetryMetrics.DeviceStateConnected;
                        break;
                    case ConnectionStatus.Disconnected_Retrying:
                        if (_connected)
                        {
                            SystemMetrics.DeviceDisconnected();
                            _connected = false;
                        }

                        _mConnectionStatus.OperationType = TelemetryMetrics.DeviceStateDisconnectedRetrying;
                        _waitForDisconnectSemaphore.Release();
                        break;
                    case ConnectionStatus.Disabled:
                        if (_connected)
                        {
                            SystemMetrics.DeviceDisconnected();
                            _connected = false;
                        }

                        _mConnectionStatus.OperationType = TelemetryMetrics.DeviceStateDisconnected;
                        break;
                    default:
                        _mConnectionStatus.OperationType = TelemetryMetrics.DeviceStateUnknown;
                        break;
                }

                _mConnectionStatus.ErrorMessage = $"ConnectionStatus: {status} reason: {reason}";
                await _writer.WriteAsync(_mConnectionStatus).ConfigureAwait(false);
            }
            finally
            {
                _connectionStatusChangedSemaphore.Release();
            }
        }

        protected Task WaitForDisconnectedAsync(CancellationToken ct)
        {
            return _waitForDisconnectSemaphore.WaitAsync(ct);
        }

        protected async Task OpenDeviceAsync(CancellationToken ct)
        {
            _m.OperationType = TelemetryMetrics.DeviceOperationOpen;
            _m.ScheduleTime = null;
            _sw.Restart();
            try
            {
                Task t = _dc.OpenAsync(ct);
                _m.ScheduleTime = _sw.ElapsedMilliseconds;

                _sw.Restart();
                await t.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _m.ErrorMessage = $"{ex.GetType().Name} - {ex.Message}";
                throw;
            }
            finally
            {
                _m.ExecuteTime = _sw.ElapsedMilliseconds;
                await _writer.WriteAsync(_m).ConfigureAwait(false);
            }
        }

        protected async Task SendMessageAsync(CancellationToken ct)
        {
            _m.OperationType = TelemetryMetrics.DeviceOperationSend;
            _m.ScheduleTime = null;
            _sw.Restart();

            try
            {
                Client.Message message = new Client.Message(_messageBytes);
                Task t = _dc.SendEventAsync(message, ct);
                _m.ScheduleTime = _sw.ElapsedMilliseconds;

                _sw.Restart();
                await t.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _m.ErrorMessage = $"{ex.GetType().Name} - {ex.Message}";
                throw;
            }
            finally
            {
                _m.ExecuteTime = _sw.ElapsedMilliseconds;
                await _writer.WriteAsync(_m).ConfigureAwait(false);
            }
        }

        protected async Task ReceiveMessageAsync(CancellationToken ct)
        {
            _mRecv.ScheduleTime = null;
            _swRecv.Restart();

            try
            {
                Task<Client.Message> t = _dc.ReceiveAsync(ct);
                _mRecv.ScheduleTime = _swRecv.ElapsedMilliseconds;

                _swRecv.Restart();
                Client.Message msg = await t.ConfigureAwait(false);
                await _dc.CompleteAsync(msg).ConfigureAwait(false);

                int deviceIdFromMessage = BitConverter.ToInt32(msg.GetBytes());
                if (_id != deviceIdFromMessage) throw new InvalidOperationException($"DeviceId mismatch: Expected {_id} actual {deviceIdFromMessage}.");
            }
            catch (Exception ex)
            {
                _mRecv.ErrorMessage = $"{ex.GetType().Name} - {ex.Message}";
                throw;
            }
            finally
            {
                _mRecv.ExecuteTime = _swRecv.ElapsedMilliseconds;
                await _writer.WriteAsync(_mRecv).ConfigureAwait(false);
            }
        }

        protected async Task EnableMethodsAsync(CancellationToken ct)
        {
            _mMethod.ScheduleTime = null;
            _mMethod.OperationType = TelemetryMetrics.DeviceOperationMethodEnable;
            _swMethod.Restart();

            try
            {
                Task t = _dc.SetMethodHandlerAsync(TestMethodName, MethodHandlerAsync, null);
                _mMethod.ScheduleTime = _swMethod.ElapsedMilliseconds;

                _swMethod.Restart();
                await t.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _mMethod.ErrorMessage = $"{ex.GetType().Name} - {ex.Message}";
                throw;
            }
            finally
            {
                _mMethod.ExecuteTime = _swMethod.ElapsedMilliseconds;
                await _writer.WriteAsync(_mMethod).ConfigureAwait(false);
            }
        }

        private Task<MethodResponse> MethodHandlerAsync(MethodRequest methodRequest, object userContext)
        {
            _methodSemaphore.Release();
            return Task.FromResult(s_methodResponse);
        }

        protected async Task WaitForMethodAsync(CancellationToken ct)
        {
            _mMethod.ScheduleTime = null;
            _mMethod.OperationType = TelemetryMetrics.DeviceOperationMethodCalled;
            _swMethod.Restart();

            try
            {
                Task t = _methodSemaphore.WaitAsync(ct);
                _mMethod.ScheduleTime = _swMethod.ElapsedMilliseconds;

                _swMethod.Restart();
                await t.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _mMethod.ErrorMessage = $"{ex.GetType().Name} - {ex.Message}";
                throw;
            }
            finally
            {
                _mMethod.ExecuteTime = _swMethod.ElapsedMilliseconds;
                await _writer.WriteAsync(_mMethod).ConfigureAwait(false);
            }
        }

        protected async Task CloseAsync(CancellationToken ct)
        {
            if (_dc == null) return;

            _m.ScheduleTime = null;
            _m.OperationType = TelemetryMetrics.DeviceOperationClose;
            _sw.Restart();

            try
            {
                await _dc.CloseAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _m.ErrorMessage = $"{ex.GetType().Name} - {ex.Message}";
                throw;
            }
            finally
            {
                _m.ExecuteTime = _sw.ElapsedMilliseconds;
                await _writer.WriteAsync(_m).ConfigureAwait(false);
            }
        }

        protected void DisposeDevice()
        {
            _dc.Dispose();
        }
    }
}
