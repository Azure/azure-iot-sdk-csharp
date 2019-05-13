// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class DeviceTelemetryTest
    {
        private int _messageSize;
        private int _messageCount;
        private int _parallelCount;
        private TransportType _protocol;
        private ResultWriter _writer;
        private DeviceClient _client;
        private Stopwatch _wall = new Stopwatch();

        public DeviceTelemetryTest(
            TransportType protocol,
            int messageCount,
            int messageSize,
            int parallelCount,
            ResultWriter resultWriter)
        {
            _messageCount = messageCount;
            _messageSize = messageSize;
            _protocol = protocol;
            _parallelCount = parallelCount;
            _writer = resultWriter;

            if (_messageCount < _parallelCount) throw new ArgumentException($"{nameof(messageCount)} needs to be greater than {nameof(parallelCount)}");
        }

        public void CreateDevice()
        {
            string connectionString =
                Environment.ExpandEnvironmentVariables(Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING"));

            _client = DeviceClient.CreateFromConnectionString(connectionString, _protocol);

            _wall.Start();
        }

        public async Task SendMessageAsync()
        {
            var metrics = new DeviceTelemetryMetrics();
            metrics.MessageSize = _messageSize;
            metrics.Protocol = _protocol.ToString();
            metrics.WallTime = _wall.Elapsed.TotalMilliseconds;

            try
            {
                Stopwatch watch = new Stopwatch();
                watch.Restart();
                Message message = new Message(new byte[_messageSize]);
                Task sendTask = _client.SendEventAsync(message);
                watch.Stop();
                metrics.SendMs = watch.Elapsed.TotalMilliseconds;

                watch.Restart();
                await sendTask.ConfigureAwait(false);
                watch.Stop();
                metrics.AckMs = watch.Elapsed.TotalMilliseconds;
            }
            catch (Exception ex)
            {
                metrics.ErrorMessage = ex.Message;
            }

            await _writer.WriteAsync(metrics).ConfigureAwait(false);
        }

        public async Task RunTestAsync()
        {
            CreateDevice();

            var tasks = new List<Task>();
            int remainingMessages = _messageCount;


            var sw = new Stopwatch();
            sw.Start();
            double startTimeSeconds = _wall.Elapsed.TotalSeconds;
            int interimStatCompleted = 0;

            for (int i = 0; i < _parallelCount; i++)
            {
                tasks.Add(SendMessageAsync());
            }

            while (true)
            {
                Task finished = await Task.WhenAny(tasks).ConfigureAwait(false);
                tasks.Remove(finished);
                remainingMessages--;

                // Interim stats
                interimStatCompleted++;
                if (sw.Elapsed.TotalSeconds >= 1)
                {
                    sw.Stop();
                    Console.Write(
                        $"{_protocol} parallel: {_parallelCount}, totalMessages: {_messageCount} @ {_messageSize}B:" + 
                        $" {interimStatCompleted / sw.Elapsed.TotalSeconds:    0.00}RPS {100 * (_messageCount - remainingMessages)/_messageCount}%          \r");

                    sw.Restart();
                    interimStatCompleted = 0;
                }

                if (remainingMessages > 0)
                {
                    tasks.Add(SendMessageAsync());
                }
                else
                {
                    break;
                }
            }

            double stopTimeSeconds = _wall.Elapsed.TotalSeconds;
            double rps = (double)_messageCount / (stopTimeSeconds - startTimeSeconds);

            Console.WriteLine($"{_protocol} parallel: {_parallelCount}, totalMessages: {_messageCount} @ {_messageSize}B - Average: {rps:    0.00}RPS                ");
        }
    }
}
