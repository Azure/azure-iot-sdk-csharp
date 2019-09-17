// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public abstract class ServiceClientScenario : PerfScenario
    {
        private static ServiceClient s_sc;

        // Shared by Create, Open and Send
        private TelemetryMetrics _m = new TelemetryMetrics();
        private Stopwatch _sw = new Stopwatch();

        // Separate metrics and time calculation for operations that can be parallelized.
        private const string TestMethodName = "PerfTestMethod";
        private const int MethodPassStatus = 200;
        private const int MethodConnectionTimeoutSeconds = 30;
        private const int MethodResponseTimeoutSeconds = 30;

        private const int C2DExpiryTimeSeconds = 90;

        private TelemetryMetrics _mMethod = new TelemetryMetrics();
        private Stopwatch _swMethod = new Stopwatch();

        private readonly byte[] _messageBytes;
        private readonly string _methodPayload;

        public ServiceClientScenario(PerfScenarioConfig config) : base(config)
        {
            _m.Id = _id;
            _mMethod.Id = _id;

            _messageBytes = new byte[_sizeBytes];
            byte[] idBytes = BitConverter.GetBytes(_id);
            Buffer.BlockCopy(idBytes, 0, _messageBytes, 0, idBytes.Length);

            _methodPayload = 
                "{\"Data\":\"" +
                Convert.ToBase64String(_messageBytes) + "\"}";
        }

        protected void CreateServiceClient()
        {
            if (_id != 0) return;
            s_sc?.Dispose();

            switch (_transport)
            {
                case Client.TransportType.Amqp_WebSocket_Only:
                    s_sc = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, TransportType.Amqp_WebSocket_Only);
                    break;
                case Client.TransportType.Amqp_Tcp_Only:
                    s_sc = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, TransportType.Amqp);
                    break;

                case Client.TransportType.Amqp:
                case Client.TransportType.Http1:
                case Client.TransportType.Mqtt:
                case Client.TransportType.Mqtt_WebSocket_Only:
                case Client.TransportType.Mqtt_Tcp_Only:
                default:
                    s_sc = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
                    break;
            }
        }

        protected async Task OpenServiceClientAsync(CancellationToken ct)
        {
            _m.Clear(TelemetryMetrics.ServiceOperationOpen);
            _sw.Restart();
            try
            {
                Task t = s_sc.OpenAsync();
                _m.ScheduleTime = _sw.ElapsedMilliseconds;

                _sw.Restart();
                await t.ConfigureAwait(false);
            }
            catch (NullReferenceException ex) // TODO #708 - ServiceClient AMQP will continuously fail with NullRefException after fault.
            {
                CreateServiceClient();
                _m.ErrorMessage = $"{ex.GetType().Name} - {ex.Message}";
                throw;
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
            _m.Clear(TelemetryMetrics.ServiceOperationSend);
            _sw.Restart();

            try
            {
                var message = new Message(_messageBytes);
                message.ExpiryTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(C2DExpiryTimeSeconds);
                Task t = s_sc.SendAsync(Configuration.Stress.GetDeviceNameById(_id, _authType), message);
                _m.ScheduleTime = _sw.ElapsedMilliseconds;

                _sw.Restart();
                await t.ConfigureAwait(false);
            }
            catch (NullReferenceException ex) // TODO #708 - ServiceClient AMQP will continuously fail with NullRefException after fault.
            {
                CreateServiceClient();
                _m.ErrorMessage = $"{ex.GetType().Name} - {ex.Message}";
                throw;
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

        protected async Task CallMethodAsync(CancellationToken ct)
        {
            _mMethod.Clear(TelemetryMetrics.ServiceOperationMethodCall);
            _swMethod.Restart();

            try
            {
                string deviceId = Configuration.Stress.GetDeviceNameById(_id, _authType);

                var methodCall = new CloudToDeviceMethod(
                    methodName: TestMethodName, 
                    responseTimeout: TimeSpan.FromSeconds(MethodResponseTimeoutSeconds), 
                    connectionTimeout: TimeSpan.FromSeconds(MethodConnectionTimeoutSeconds));
                methodCall.SetPayloadJson(_methodPayload);
                Task<CloudToDeviceMethodResult> t = s_sc.InvokeDeviceMethodAsync(Configuration.Stress.GetDeviceNameById(_id, _authType), methodCall);
                _mMethod.ScheduleTime = _swMethod.ElapsedMilliseconds;

                _swMethod.Restart();
                CloudToDeviceMethodResult result = await t.ConfigureAwait(false);

                // Check method result.
                if (result.Status != MethodPassStatus)
                {
                    throw new InvalidOperationException($"IoTPerfClient: Status: {result.Status} Payload:{result.GetPayloadAsJson()}");
                }
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
            if (s_sc == null) return;

            _m.Clear(TelemetryMetrics.ServiceOperationClose);
            _sw.Restart();

            try
            {
                await s_sc.CloseAsync().ConfigureAwait(false);
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
    }
}
