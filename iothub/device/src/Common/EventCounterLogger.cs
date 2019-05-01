using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// Device event monitor
    /// </summary>
    public interface IDeviceEventMonitor
    {
        /// <summary>
        /// When device event occurs 
        /// </summary>
        /// <param name="deviceEventName">Name of device event</param>
        void OnEvent(string deviceEventName);
    }

    /// <summary>
    /// Start or stop customized event counter monitor
    /// </summary>
    public static class DeviceEventMonitor
    {
        /// <summary>
        /// Attach device event monitor
        /// </summary>
        /// <param name="deviceEventMonitor">Customized device event monitor</param>
        public static List<string> Attach(IDeviceEventMonitor deviceEventMonitor)
        {
            DeviceEventCounter.s_deviceEventMonitor = deviceEventMonitor;
            return DeviceEventCounter.s_deviceEventNames;
        }

        /// <summary>
        /// Detach device event monitor
        /// </summary>
        public static void Detach()
        {
            DeviceEventCounter.s_deviceEventMonitor = null;
        }
    }

    internal interface IDeviceEventListener
    {
        void OnDeviceClientCreated();
        void OnDeviceClientDisposed();
        void OnAmqpUnitCreated();
        void OnAmqpUnitDisposed();
        void OnAmqpConnectionEstablished();
        void OnAmqpConnectionDisconnected();
        void OnAmqpSessionEstablished();
        void OnAmqpSessionDisconnected();
        void OnAmqpTokenRefresherStarted();
        void OnAmqpTokenRefresherStopped();
        void OnAmqpTokenRefreshed();
        bool IsEnabled();
    }

#if NETSTANDARD2_0
    internal sealed class DeviceEventCounterListener : EventSource, IDeviceEventListener
    {
        private const string s_eventSourceName = "Microsoft-Azure-Devices-Shared-Device-Event-Counter";

        private readonly static IDeviceEventListener s_instance = new DeviceEventCounterListener();

        private readonly List<EventCounter> _deviceEventCounters;

        internal static IDeviceEventListener GetInstance()
        {
            return s_instance;
        }

        private DeviceEventCounterListener() : base(s_eventSourceName)
        {
            _deviceEventCounters = new List<EventCounter>();
            foreach (string deviceEventName in DeviceEventCounter.s_deviceEventNames)
            {
                _deviceEventCounters.Add(new EventCounter(deviceEventName, this));
            }
        }

        public void OnDeviceClientCreated()
        {
            _deviceEventCounters[0].WriteMetric(1);
        }

        public void OnDeviceClientDisposed()
        {
            _deviceEventCounters[1].WriteMetric(1);
        }

        public void OnAmqpUnitCreated()
        {
            _deviceEventCounters[2].WriteMetric(1);
        }

        public void OnAmqpUnitDisposed()
        {
            _deviceEventCounters[3].WriteMetric(1);
        }

        public void OnAmqpConnectionEstablished()
        {
            _deviceEventCounters[4].WriteMetric(1);
        }

        public void OnAmqpConnectionDisconnected()
        {
            _deviceEventCounters[5].WriteMetric(1);
        }

        public void OnAmqpSessionEstablished()
        {
            _deviceEventCounters[6].WriteMetric(1);
        }

        public void OnAmqpSessionDisconnected()
        {
            _deviceEventCounters[7].WriteMetric(1);
        }

        public void OnAmqpTokenRefresherStarted()
        {
            _deviceEventCounters[8].WriteMetric(1);
        }

        public void OnAmqpTokenRefresherStopped()
        {
            _deviceEventCounters[9].WriteMetric(1);
        }

        public void OnAmqpTokenRefreshed()
        {
            _deviceEventCounters[10].WriteMetric(1);
        }

    }
#endif
    internal static class DeviceEventCounter
    {
        internal readonly static List<string> s_deviceEventNames = new List<string>
        {
            "Device-Client-Creation",
            "Device-Client-Disposal",
            "AMQP-Unit-Creation",
            "AMQP-Unit-Disposal",
            "AMQP-Connection-Establishment",
            "AMQP-Connection-Disconnection",
            "AMQP-Session-Establishment",
            "AMQP-Session-Disconnection",
            "AMQP-Token-Refresher-Initiation",
            "AMQP-Token-Refresher-Termination",
            "AMQP-Token-Refreshes",
        };

#if NETSTANDARD2_0
        private static IDeviceEventListener s_deviceEventCounterListener = DeviceEventCounterListener.GetInstance();
#else
        private static IDeviceEventListener s_deviceEventCounterListener = null;
#endif
        internal static IDeviceEventMonitor s_deviceEventMonitor;

        internal static void OnDeviceClientCreated()
        {
            if (IsDeviceEventListenerEnabled(s_deviceEventCounterListener)) s_deviceEventCounterListener.OnDeviceClientCreated();
            s_deviceEventMonitor?.OnEvent(s_deviceEventNames[0]);
        }

        internal static void OnDeviceClientDisposed()
        {
            if (IsDeviceEventListenerEnabled(s_deviceEventCounterListener)) s_deviceEventCounterListener.OnDeviceClientDisposed();
            s_deviceEventMonitor?.OnEvent(s_deviceEventNames[1]);
        }

        internal static void OnAmqpUnitCreated()
        {
            if (IsDeviceEventListenerEnabled(s_deviceEventCounterListener)) s_deviceEventCounterListener.OnAmqpUnitCreated();
            s_deviceEventMonitor?.OnEvent(s_deviceEventNames[2]);
        }

        internal static void OnAmqpUnitDisposed()
        {
            if (IsDeviceEventListenerEnabled(s_deviceEventCounterListener)) s_deviceEventCounterListener.OnAmqpUnitDisposed();
            s_deviceEventMonitor?.OnEvent(s_deviceEventNames[3]);
        }

        internal static void OnAmqpConnectionEstablished()
        {
            if (IsDeviceEventListenerEnabled(s_deviceEventCounterListener)) s_deviceEventCounterListener.OnAmqpConnectionEstablished();
            s_deviceEventMonitor?.OnEvent(s_deviceEventNames[4]);
        }

        internal static void OnAmqpConnectionDisconnected()
        {
            if (IsDeviceEventListenerEnabled(s_deviceEventCounterListener)) s_deviceEventCounterListener.OnAmqpConnectionDisconnected();
            s_deviceEventMonitor?.OnEvent(s_deviceEventNames[5]);
        }

        internal static void OnAmqpSessionEstablished()
        {
            if (IsDeviceEventListenerEnabled(s_deviceEventCounterListener)) s_deviceEventCounterListener.OnAmqpSessionEstablished();
            s_deviceEventMonitor?.OnEvent(s_deviceEventNames[6]);
        }

        internal static void OnAmqpSessionDisconnected()
        {
            if (IsDeviceEventListenerEnabled(s_deviceEventCounterListener)) s_deviceEventCounterListener.OnAmqpSessionDisconnected();
            s_deviceEventMonitor?.OnEvent(s_deviceEventNames[7]);
        }

        internal static void OnAmqpTokenRefresherStarted()
        {
            if (IsDeviceEventListenerEnabled(s_deviceEventCounterListener)) s_deviceEventCounterListener.OnAmqpTokenRefresherStarted();
            s_deviceEventMonitor?.OnEvent(s_deviceEventNames[8]);
        }

        internal static void OnAmqpTokenRefresherStopped()
        {
            if (IsDeviceEventListenerEnabled(s_deviceEventCounterListener)) s_deviceEventCounterListener.OnAmqpTokenRefresherStopped();
            s_deviceEventMonitor?.OnEvent(s_deviceEventNames[9]);
        }

        internal static void OnAmqpTokenRefreshed()
        {
            if (IsDeviceEventListenerEnabled(s_deviceEventCounterListener)) s_deviceEventCounterListener.OnAmqpTokenRefreshed();
            s_deviceEventMonitor?.OnEvent(s_deviceEventNames[10]);
        }

        internal static bool IsEnabled => IsDeviceEventListenerEnabled(s_deviceEventCounterListener) || s_deviceEventMonitor != null;

        private static bool IsDeviceEventListenerEnabled(IDeviceEventListener deviceEventListener)
        {
            return deviceEventListener?.IsEnabled() ?? false;
        }
    }
}

