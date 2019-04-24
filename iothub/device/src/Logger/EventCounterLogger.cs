using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Logger
{
    /// <summary>
    /// Logger for device client events.
    /// </summary>
    public interface IEventCounterLogger
    {
        /// <summary>
        /// When device client was created.
        /// </summary>
        void OnDeviceClientCreated();
        /// <summary>
        /// When device client was disposed.
        /// </summary>
        void OnDeviceClientDisposed();
        /// <summary>
        /// When AMQP unit was created.
        /// </summary>
        void OnAmqpUnitCreated();
        /// <summary>
        /// When AMQP unit was disposed.
        /// </summary>
        void OnAmqpUnitDisposed();
        /// <summary>
        /// When AMQP connection was established.
        /// </summary>
        void OnAmqpConnectionEstablished();
        /// <summary>
        /// When AMQP connection was disconnected.
        /// </summary>
        void OnAmqpConnectionDisconnected();
        /// <summary>
        /// When AMQP session was established.
        /// </summary>
        void OnAmqpSessionEstablished();
        /// <summary>
        /// When AMQP session was disconnected.
        /// </summary>
        void OnAmqpSessionDisconnected();
        /// <summary>
        /// When AMQP token refresher was created.
        /// </summary>
        void OnAmqpTokenRefresherCreated();
        /// <summary>
        /// When AMQP token refresher was disposed.
        /// </summary>
        void OnAmqpTokenRefresherDisposed();
        /// <summary>
        /// When AMQP token was refreshed.
        /// </summary>
        void OnAmqpTokenRefreshed();
        /// <summary>
        /// Enable logger and start logging
        /// </summary>
        /// <param name="interval">The interval of each logging period.</param>
        /// <param name="cancellationToken">Cancel the loop.</param>
        /// <param name="redirectToConsole">Dump log to file.</param>
        Task StartLoggerAsync(TimeSpan interval, CancellationToken cancellationToken, bool redirectToConsole);
    }

#if NETSTANDARD2_0
    /// <summary>
    /// Logger for device client events.
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Devices-Client-Logger-Event-Counter")]
    public class EventCounterLogger : EventSource, IEventCounterLogger
    {
        private static EventCounterLogger s_instance = new EventCounterLogger();

        

        private bool _started;
        private int _deviceClientCreationCounts;
        private int _deviceClientDisposeCounts;
        private int _amqpUnitCreationCounts;
        private int _amqpUnitDisposeCounts;
        private int _amqpConnectionEstablishCounts;
        private int _amqpConnectionDisconnectionCounts;
        private int _amqpSessionEstablishCounts;
        private int _amqpSessionDisconnectionCounts;
        private int _amqpTokenRefresherCreationCounts;
        private int _amqpTokenRefresherDisposeCounts;
        private int _amqpTokenRefreshCounts;

        /// <summary>
        /// Return the instance.
        /// </summary>
        public static IEventCounterLogger GetInstance()
        {
            return s_instance;
        }

        private EventCounterLogger() : base()
        {
        }

        /// <summary>
        /// When device client was created.
        /// </summary>
        public void OnDeviceClientCreated()
        {
            if (_started)
            {
                Interlocked.Increment(ref _deviceClientCreationCounts);
            }
        }

        /// <summary>
        /// When device client was disposed.
        /// </summary>
        public void OnDeviceClientDisposed()
        {
            if (_started)
            {
                Interlocked.Increment(ref _deviceClientDisposeCounts);
            }
        }

        /// <summary>
        /// When Amqp unit was created.
        /// </summary>
        public void OnAmqpUnitCreated()
        {
            if (_started)
            {
                Interlocked.Increment(ref _amqpUnitCreationCounts);
            }
        }

        /// <summary>
        /// When Amqp unit was disposed.
        /// </summary>
        public void OnAmqpUnitDisposed()
        {
            if (_started)
            {
                Interlocked.Increment(ref _amqpUnitDisposeCounts);
            }
        }

        /// <summary>
        /// When AMQP connection was established.
        /// </summary>
        public void OnAmqpConnectionEstablished()
        {
            if (_started)
            {
                Interlocked.Increment(ref _amqpConnectionEstablishCounts);
            }
        }

        /// <summary>
        /// When AMQP connection was disconnected.
        /// </summary>
        public void OnAmqpConnectionDisconnected()
        {
            if (_started)
            {
                Interlocked.Increment(ref _amqpConnectionDisconnectionCounts);
            }
        }

        /// <summary>
        /// When AMQP session was established.
        /// </summary>

        public void OnAmqpSessionEstablished()
        {
            if (_started)
            {
                Interlocked.Increment(ref _amqpSessionEstablishCounts);
            }
        }

        /// <summary>
        /// When AMQP session was disconnected.
        /// </summary>
        public void OnAmqpSessionDisconnected()
        {
            if (_started)
            {
                Interlocked.Increment(ref _amqpSessionDisconnectionCounts);
            }
        }
        
        /// <summary>
        /// When AMQP token refresher was created.
        /// </summary>
        public void OnAmqpTokenRefresherCreated()
        {
            if (_started)
            {
                Interlocked.Increment(ref _amqpTokenRefresherCreationCounts);
            }
        }

        /// <summary>
        /// When AMQP token refresher was disposed.
        /// </summary>
        public void OnAmqpTokenRefresherDisposed()
        {
            if (_started)
            {
                Interlocked.Increment(ref _amqpTokenRefresherDisposeCounts);
            }
        }

        /// <summary>
        /// When AMQP token was refreshed.
        /// </summary>
        public void OnAmqpTokenRefreshed()
        {
            if (_started)
            {
                Interlocked.Increment(ref _amqpTokenRefreshCounts);
            }
        }

        /// <summary>
        /// Enable logger and start logging
        /// </summary>
        /// <param name="interval">The interval of each logging period.</param>
        /// <param name="cancellationToken">Cancel the loop.</param>
        /// <param name="redirectToConsole">Redirect log to console.</param>
        public async Task StartLoggerAsync(TimeSpan interval, CancellationToken cancellationToken, bool redirectToConsole)
        {
            if (_started) return;

            _started = true;

            if (IsEnabled())
            {
                EventCounter deviceClientCreationEventCounter = new EventCounter("Device-Client-Creation", this);
                EventCounter deviceClientDisposeEventCounter = new EventCounter("Device-Client-Dispose", this);
                EventCounter amqpUnitCreationEventCounter = new EventCounter("AMQP-Unit-Creation", this);
                EventCounter amqpUnitDisposeEventCounter = new EventCounter("AMQP-Unit-Dispose", this);
                EventCounter amqpConnectionEstablishEventCounter = new EventCounter("AMQP-Connection-Establish", this);
                EventCounter amqpConnectionDisconnectionEventCounter = new EventCounter("AMQP-Connection-Disconnection", this);
                EventCounter amqpSessionEstablishEventCounter = new EventCounter("AMQP-Session-Establish", this);
                EventCounter amqpSessionDisconnectionEventCounter = new EventCounter("AMQP-Session-Disconnection", this);
                EventCounter amqpTokenRefreshEventCounter = new EventCounter("AMQP-Token-Refresh", this);

                while (!cancellationToken.IsCancellationRequested)
                {
                    deviceClientCreationEventCounter.WriteMetric(_deviceClientCreationCounts);
                    deviceClientDisposeEventCounter.WriteMetric(_deviceClientDisposeCounts);
                    amqpUnitCreationEventCounter.WriteMetric(_amqpUnitCreationCounts);
                    amqpUnitDisposeEventCounter.WriteMetric(_amqpUnitDisposeCounts);
                    amqpConnectionEstablishEventCounter.WriteMetric(_amqpConnectionEstablishCounts);
                    amqpConnectionDisconnectionEventCounter.WriteMetric(_amqpConnectionDisconnectionCounts);
                    amqpSessionEstablishEventCounter.WriteMetric(_amqpSessionEstablishCounts);
                    amqpSessionDisconnectionEventCounter.WriteMetric(_amqpSessionDisconnectionCounts);
                    amqpTokenRefreshEventCounter.WriteMetric(_amqpTokenRefreshCounts);
                    await Task.Delay(interval);
                }
            }
            if (redirectToConsole)
            {
                Console.WriteLine("Time,Device-Client-Creation,Device-Client-Dispose,AMQP-Unit-Creation,AMQP-Unit-Dispose,AMQP-Connection-Establish,AMQP-Connection-Disconnection,AMQP-Session-Establish,AMQP-Session-Disconnection,AMQP-Token-Refresher-Creation,AMQP-Token-Refresher-Dispose,AMQP-Token-Refresh");
                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"{DateTime.Now},{_deviceClientCreationCounts},{_deviceClientDisposeCounts},{_amqpUnitCreationCounts},{_amqpUnitDisposeCounts},{_amqpConnectionEstablishCounts},{_amqpConnectionDisconnectionCounts},{_amqpSessionEstablishCounts},{_amqpSessionDisconnectionCounts},{_amqpTokenRefresherCreationCounts},{_amqpTokenRefresherDisposeCounts},{_amqpTokenRefreshCounts}");
                    await Task.Delay(interval);
               }
            }

            _started = false;
        }
    }
# else
    internal class EventCounterLogger : IEventCounterLogger
    {
        private static EventCounterLogger s_instance = new EventCounterLogger();

        public static IEventCounterLogger GetInstance()
        {
            return s_instance;
        }

        public void OnDeviceClientCreated()
        {
        }

        public void OnDeviceClientDisposed()
        {
        }
    
        public void OnAmqpUnitCreated()
        {
        }

        public void OnAmqpUnitDisposed()
        {
        }

        public void OnAmqpConnectionEstablished()
        {
        }

        public void OnAmqpConnectionDisconnected()
        {
        }

        public void OnAmqpSessionEstablished()
        {
        }

        public void OnAmqpSessionDisconnected()
        {
        }

        public void OnAmqpTokenRefresherCreated()
        {
        }

        public void OnAmqpTokenRefresherDisposed()
        {
        }

        public void OnAmqpTokenRefreshed()
        {
        }

        public async Task StartLoggerAsync(TimeSpan interval, CancellationToken cancellationToken, bool redirectToConsole)
        {
        }

    }

#endif
}
