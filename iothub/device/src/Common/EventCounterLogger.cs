using System;
using System.Diagnostics.Tracing;
using System.Threading;

namespace Microsoft.Azure.Devices.Shared
{
    internal interface IEventCountLogger
    {
        void LogDeviceClientCreationCount(int count);
         void LogDeviceClientDisposalCount(int count);
        void LogActiveDeviceClientCount(int count);
        void LogAmqpUnitCreationCount(int count);
        void LogAmqpUnitDisposalCount(int count);
        void LogActiveAmqpUnitCount(int count);
        void LogAmqpConnectionEstablishmentCount(int count);
        void LogAmqpConnectionDisconnectionCount(int count);
        void LogActiveAmqpConnectionCount(int count);
        void LogAmqpSessionEstablishmentCount(int count);
        void LogAmqpSessionDisconnectionCount(int count);
        void LogActiveAmqpSessionCount(int count);
        void LogAmqpTokenRefresherInitiationCount(int count);
        void LogAmqpTokenRefresherTerminationCount(int count);
        void LogActiveAmqpTokenRefresherCount(int count);
        void LogAmqpTokenRefreshCount(int count);
        bool IsEnabled();
    }

    internal sealed class DeviceEventCounter
    {
        internal readonly static string[] s_eventNames = {
            "Device-Client-Creation",
            "Device-Client-Disposal",
            "Device-Client-Active",
            "AMQP-Unit-Creation",
            "AMQP-Unit-Disposal",
            "AMQP-Unit-Active",
            "AMQP-Connection-Establishment",
            "AMQP-Connection-Disconnection",
            "AMQP-Connection-Active",
            "AMQP-Session-Establishment",
            "AMQP-Session-Disconnection",
            "AMQP-Session-Active",
            "AMQP-Token-Refresher-Initiation",
            "AMQP-Token-Refresher-Termination",
            "AMQP-Token-Refresher-Active",
            "AMQP-Token-Refreshes"
        };

        private static int s_deviceClientCreationCount;
        private static int s_deviceClientDisposalCount;
        private static int s_amqpUnitCreationCount;
        private static int s_amqpUnitDisposalCount;
        private static int s_amqpConnectionEstablishmentCount;
        private static int s_amqpConnectionDisconnectionCount;
        private static int s_amqpSessionEstablishmentCount;
        private static int s_amqpSessionDisconnectionCount;
        private static int s_amqpTokenRefresherInitiation;
        private static int s_amqpTokenRefreshTerminationCount;
        private static int s_amqpTokenRefreshCount;
        private static IEventCountLogger s_eventCountLogger = CreateEventCountLogger();

        private DeviceEventCounter()
        {
        }

        private static void LogActiveDeviceClientCount()
        {
            s_eventCountLogger.LogActiveDeviceClientCount(s_deviceClientCreationCount - s_deviceClientDisposalCount);
        }

        internal static void OnDeviceClientCreated()
        {
            s_eventCountLogger.LogDeviceClientCreationCount(Interlocked.Increment(ref s_deviceClientCreationCount));
            LogActiveDeviceClientCount();
        }

        internal static void OnDeviceClientDisposed()
        {
            s_eventCountLogger.LogDeviceClientDisposalCount(Interlocked.Increment(ref s_deviceClientDisposalCount));
            LogActiveDeviceClientCount();
        }

        private static void LogActiveAmqpUnitCount()
        {
            s_eventCountLogger.LogActiveAmqpUnitCount(s_amqpUnitCreationCount - s_amqpUnitDisposalCount);
        }

        internal static void OnAmqpUnitCreated()
        {
            s_eventCountLogger.LogAmqpUnitCreationCount(Interlocked.Increment(ref s_amqpUnitCreationCount));
            LogActiveAmqpUnitCount();
        }

        internal static void OnAmqpUnitDisposed()
        {
            s_eventCountLogger.LogAmqpUnitDisposalCount(Interlocked.Increment(ref s_amqpUnitDisposalCount));
            LogActiveAmqpUnitCount();
        }

        private static void LogActiveAmqpConnectionCount()
        {
            s_eventCountLogger.LogActiveAmqpConnectionCount(s_amqpConnectionEstablishmentCount - s_amqpConnectionDisconnectionCount);
        }

        internal static void OnAmqpConnectionEstablished()
        {
            s_eventCountLogger.LogAmqpConnectionEstablishmentCount(Interlocked.Increment(ref s_amqpConnectionEstablishmentCount));
            LogActiveAmqpConnectionCount();
        }

        internal static void OnAmqpConnectionDisconnected()
        {
            s_eventCountLogger.LogAmqpConnectionDisconnectionCount(Interlocked.Increment(ref s_amqpConnectionDisconnectionCount));
            LogActiveAmqpConnectionCount();
        }

        private static void LogActiveAmqpSessionCount()
        {
            s_eventCountLogger.LogActiveAmqpSessionCount(s_amqpSessionEstablishmentCount - s_amqpSessionDisconnectionCount);
        }

        internal static void OnAmqpSessionEstablished()
        {
            s_eventCountLogger.LogAmqpSessionEstablishmentCount(Interlocked.Increment(ref s_amqpSessionEstablishmentCount));
            LogActiveAmqpSessionCount();
        }

        internal static void OnAmqpSessionDisconnected()
        {
            s_eventCountLogger.LogAmqpSessionDisconnectionCount(Interlocked.Increment(ref s_amqpSessionDisconnectionCount));
            LogActiveAmqpSessionCount();
        }

        private static void LogActiveAmqpTokenRefresherCount()
        {
            s_eventCountLogger.LogActiveAmqpTokenRefresherCount(s_amqpTokenRefresherInitiation - s_amqpTokenRefreshTerminationCount);
        }

        internal static void OnAmqpTokenRefresherStarted()
        {
            s_eventCountLogger.LogAmqpTokenRefresherInitiationCount(Interlocked.Increment(ref s_amqpTokenRefresherInitiation));
            LogActiveAmqpTokenRefresherCount();
        }

        internal static void OnAmqpTokenRefresherStopped()
        {
            s_eventCountLogger.LogAmqpTokenRefresherTerminationCount(Interlocked.Increment(ref s_amqpTokenRefreshTerminationCount));
            LogActiveAmqpTokenRefresherCount();
        }

        internal static void OnAmqpTokenRefreshed()
        {
            s_eventCountLogger.LogAmqpTokenRefreshCount(Interlocked.Increment(ref s_amqpTokenRefreshCount));
        }

        public static bool IsEnabled => s_eventCountLogger.IsEnabled();

        private static IEventCountLogger CreateEventCountLogger()
        {
#if NETSTANDARD2_0
            return new EtwCounterLogger();
#else
            return new NopCounterLogger();
#endif
        }

    }

#if NETSTANDARD2_0
    [EventSource(Name = "Microsoft-Azure-Devices-Shared-Device-Event-Counter")]
    internal class EtwCounterLogger : EventSource, IEventCountLogger
    {
        private readonly EventCounter[] s_eventCounters;

        internal EtwCounterLogger()
        {
            int length = DeviceEventCounter.s_eventNames.Length;
            s_eventCounters = new EventCounter[length];
            for (int i = 0; i < length; i++)
            {
                s_eventCounters[i] = new EventCounter(DeviceEventCounter.s_eventNames[i], this);
            }
        }

        private void WriteMetric(int index, int count)
        {
            Console.WriteLine($"{DateTime.Now}: {DeviceEventCounter.s_eventNames[index]}: {count}");
            s_eventCounters[index].WriteMetric(count);
        }

        public void LogDeviceClientCreationCount(int count)
        {
            WriteMetric(0, count);
        }

        public void LogDeviceClientDisposalCount(int count)
        {
            WriteMetric(1, count);
        }

        public void LogActiveDeviceClientCount(int count)
        {
            WriteMetric(2, count);
        }

        public void LogAmqpUnitCreationCount(int count)
        {
            WriteMetric(3, count);
        }

        public void LogAmqpUnitDisposalCount(int count)
        {
            WriteMetric(4, count);
        }

        public void LogActiveAmqpUnitCount(int count)
        {
            WriteMetric(5, count);
        }
        public void LogAmqpConnectionEstablishmentCount(int count)
        {
            WriteMetric(6, count);
        }

        public void LogAmqpConnectionDisconnectionCount(int count)
        {
            WriteMetric(7, count);
        }

        public void LogActiveAmqpConnectionCount(int count)
        {
            WriteMetric(8, count);
        }

        public void LogAmqpSessionEstablishmentCount(int count)
        {
            WriteMetric(9, count);
        }

        public void LogAmqpSessionDisconnectionCount(int count)
        {
            WriteMetric(10, count);
        }

        public void LogActiveAmqpSessionCount(int count)
        {
            WriteMetric(11, count);
        }

        public void LogAmqpTokenRefresherInitiationCount(int count)
        {
            WriteMetric(12, count);
        }

        public void LogAmqpTokenRefresherTerminationCount(int count)
        {
            WriteMetric(13, count);
        }

        public void LogActiveAmqpTokenRefresherCount(int count)
        {
            WriteMetric(14, count);
        }

        public void LogAmqpTokenRefreshCount(int count)
        {
            WriteMetric(15, count);
        }

    }
#else
    internal class NopCounterLogger : IEventCountLogger
    {
        public void LogDeviceClientCreationCount(int count) {}
        public void LogDeviceClientDisposalCount(int count) {}
        public void LogActiveDeviceClientCount(int count) {}
        public void LogAmqpUnitCreationCount(int count) {}
        public void LogAmqpUnitDisposalCount(int count) {}
        public void LogActiveAmqpUnitCount(int count) {}
        public void LogAmqpConnectionEstablishmentCount(int count) {}
        public void LogAmqpConnectionDisconnectionCount(int count) {}
        public void LogActiveAmqpConnectionCount(int count) {}
        public void LogAmqpSessionEstablishmentCount(int count) {}
        public void LogAmqpSessionDisconnectionCount(int count) {}
        public void LogActiveAmqpSessionCount(int count) {}
        public void LogAmqpTokenRefresherInitiationCount(int count) {}
        public void LogAmqpTokenRefresherTerminationCount(int count) {}
        public void LogActiveAmqpTokenRefresherCount(int count) {}
        public void LogAmqpTokenRefreshCount(int count) {}
        public bool IsEnabled() { return false; }
    }
#endif
}
