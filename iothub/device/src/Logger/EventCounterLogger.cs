using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Logger
{
    /// <summary>
    /// Event logger interface.
    /// </summary>
    public interface IEventCountLogger
    {
        /// <summary>
        /// Write event names
        /// </summary>
        /// <param name="eventNames">Event names.</param>
        void WriteEventNames(params string[] eventNames);

        /// <summary>
        /// Write event counts
        /// </summary>
        /// <param name="eventCounts">Event counts.</param>
        void WriteEventCounts(params int[] eventCounts);
    }

    /// <summary>
    /// Device client event counter interface.
    /// </summary>
    public interface IDeviceEventCounter
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
        /// When AMQP token refresher was started.
        /// </summary>
        void OnAmqpTokenRefresherStarted();
        /// <summary>
        /// When AMQP token refresher was stopped.
        /// </summary>
        void OnAmqpTokenRefresherStopped();
        /// <summary>
        /// When AMQP token was refreshed.
        /// </summary>
        void OnAmqpTokenRefreshed();
        /// <summary>
        /// Enable logger and start logging
        /// </summary>
        /// <param name="interval">The interval of each logging period.</param>
        /// <param name="logger">The logger to accept records.</param>
        /// <param name="cancellationToken">Cancel the loop.</param>
        Task StartLoggerAsync(TimeSpan interval, IEventCountLogger logger, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Device client event counter implemetation.
    /// </summary>
#if NETSTANDARD2_0
    [EventSource(Name = "Microsoft-Azure-Devices-Client-Logger-Event-Counter")]
    public class DeviceEventCounter : EventSource, IDeviceEventCounter
#else
    public class DeviceEventCounter : IDeviceEventCounter
#endif
    {
        private readonly static DeviceEventCounter s_instance = new DeviceEventCounter();
        internal readonly static string[] s_event_names = {
            "Device-Client-Creation",
            "Device-Client-Dispose",
            "AMQP-Unit-Creation",
            "AMQP-Unit-Dispose",
            "AMQP-Connection-Establish",
            "AMQP-Connection-Disconnection",
            "AMQP-Session-Establish",
            "AMQP-Session-Disconnection",
            "AMQP-Token-Refresher-Started",
            "AMQP-Token-Refresher-Stopped",
            "AMQP-Token-Refreshes"
        };

        private int _deviceClientCreationCounts;
        private int _deviceClientDisposeCounts;
        private int _amqpUnitCreationCounts;
        private int _amqpUnitDisposeCounts;
        private int _amqpConnectionEstablishCounts;
        private int _amqpConnectionDisconnectionCounts;
        private int _amqpSessionEstablishCounts;
        private int _amqpSessionDisconnectionCounts;
        private int _amqpTokenRefreshStartCounts;
        private int _amqpTokenRefreshStopCounts;
        private int _amqpTokenRefreshCounts;

        private bool _started;

        /// <summary>
        /// Return the instance.
        /// </summary>
        public static IDeviceEventCounter GetInstance()
        {
            return s_instance;
        }

        private DeviceEventCounter() : base()
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
        public void OnAmqpTokenRefresherStarted()
        {
            if (_started)
            {
                Interlocked.Increment(ref _amqpTokenRefreshStartCounts);
            }
        }

        /// <summary>
        /// When AMQP token refresher was disposed.
        /// </summary>
        public void OnAmqpTokenRefresherStopped()
        {
            if (_started)
            {
                Interlocked.Increment(ref _amqpTokenRefreshStopCounts);
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
        /// <param name="logger">The logger to accept records.</param>
        /// <param name="cancellationToken">Cancel the loop.</param>
        public async Task StartLoggerAsync(TimeSpan interval, IEventCountLogger logger, CancellationToken cancellationToken)
        {
            if (_started) throw new InvalidOperationException("EventCounterLogger is already running."); ;

            _started = true;

            IEventCountLogger eventCountLogger = CreateLogger(logger);

            eventCountLogger.WriteEventNames(s_event_names);
            while (!cancellationToken.IsCancellationRequested)
            {
                eventCountLogger.WriteEventCounts(
                    _deviceClientCreationCounts,
                    _deviceClientDisposeCounts,
                    _amqpUnitCreationCounts,
                    _amqpUnitDisposeCounts,
                    _amqpConnectionEstablishCounts,
                    _amqpConnectionDisconnectionCounts,
                    _amqpSessionEstablishCounts,
                    _amqpSessionDisconnectionCounts,
                    _amqpTokenRefreshStartCounts,
                    _amqpTokenRefreshStopCounts,
                    _amqpTokenRefreshCounts
                );
                await Task.Delay(interval).ConfigureAwait(false);
            }

            _started = false;
        }

        private IEventCountLogger CreateLogger(IEventCountLogger logger)
        {
#if NETSTANDARD2_0
            return new EventCounterLogger(this, logger);
#else
            return logger?? ConsoleCounterLogger.GetInstance();
# endif
       }

    }

    /// <summary>
    /// Event logger implematation with console output in CSV format.
    /// </summary>
    public class ConsoleCounterLogger : IEventCountLogger
    {
        private readonly static ConsoleCounterLogger s_instance = new ConsoleCounterLogger();

        private ConsoleCounterLogger()
        {
        }

        /// <summary>
        /// Return instance
        /// </summary>
        /// <returns></returns>
        public static IEventCountLogger GetInstance()
        {
            return s_instance;
        }

        /// <summary>
        /// Write event names
        /// </summary>
        /// <param name="eventNames">Event names.</param>
        
        public void WriteEventNames(params string[] eventNames)
        {
            Console.WriteLine($"Time,{string.Join(",", eventNames)}");
        }

        /// <summary>
        /// Write event counts
        /// </summary>
        /// <param name="eventCounts">Event counts.</param>
        public void WriteEventCounts(params int[] eventCounts)
        {
            Console.WriteLine($"{DateTime.Now},{string.Join(",", eventCounts)}");
        }
    }

#if NETSTANDARD2_0
    internal class EventCounterLogger : IEventCountLogger
    {
        EventCounter[] _eventCounters;
        IEventCountLogger _logger;

        internal EventCounterLogger(EventSource eventSource, IEventCountLogger logger)
        {
            _logger = logger;
            if (eventSource.IsEnabled())
            {
                int length = DeviceEventCounter.s_event_names.Length;
                _eventCounters = new EventCounter[length];
                for (int i = 0; i < length; i++)
                {
                    _eventCounters[i] = new EventCounter(DeviceEventCounter.s_event_names[i], eventSource);
                }
            }
        }

        public void WriteEventNames(string[] eventNames)
        {
            _logger?.WriteEventNames(eventNames);
        }

        public void WriteEventCounts(params int[] eventCounts)
        {
            if (_eventCounters != null)
            {
                for (int i = 0; i < _eventCounters.Length && i < eventCounts.Length; i++)
                {
                    _eventCounters[i].WriteMetric(eventCounts[i]);
                }
            }
            _logger?.WriteEventCounts(eventCounts);
        }
    }
#endif
}
