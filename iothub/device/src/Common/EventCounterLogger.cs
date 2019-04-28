using System;
using System.Diagnostics.Tracing;

#if NETSTANDARD2_0   
    internal sealed class DeviceEventCounter : EventSource
    {
        private readonly static DeviceEventCounter s_instance = new DeviceEventCounter();

        private readonly EventCounter _deviceClientCreation;
        private readonly EventCounter _deviceClientDisposal;
        private readonly EventCounter _amqpUnitCreation;
        private readonly EventCounter _amqpUnitDisposal;
        private readonly EventCounter _amqpConnectionEstablishment;
        private readonly EventCounter _amqpConnectionDisconnection;
        private readonly EventCounter _amqpSessionEstablishment;
        private readonly EventCounter _amqpSessionDisconnection;
        private readonly EventCounter _amqpTokenRefresherInitiation;
        private readonly EventCounter _amqpTokenRefresherTermination;
        private readonly EventCounter _amqpTokenRefresh;

        private DeviceEventCounter() : base("Microsoft-Azure-Devices-Shared-Device-Event-Counter")
        {
            _deviceClientCreation = new EventCounter("Device-Client-Creation", this);
            _deviceClientDisposal = new EventCounter("Device-Client-Disposal", this);
            _amqpUnitCreation = new EventCounter("AMQP-Unit-Creation", this);
            _amqpUnitDisposal = new EventCounter("AMQP-Unit-Disposal", this);
            _amqpConnectionEstablishment = new EventCounter("AMQP-Connection-Establishment", this);
            _amqpConnectionDisconnection = new EventCounter("AMQP-Connection-Disconnection", this);
            _amqpSessionEstablishment = new EventCounter("AMQP-Session-Establishment", this);
            _amqpSessionDisconnection = new EventCounter("AMQP-Session-Disconnection", this);
            _amqpTokenRefresherInitiation = new EventCounter("AMQP-Token-Refresher-Initiation", this);
            _amqpTokenRefresherTermination = new EventCounter("AMQP-Token-Refresher-Termination", this);
            _amqpTokenRefresh = new EventCounter("AMQP-Token-Refreshes", this);
        }

        internal static void OnDeviceClientCreated()
        {
            s_instance._deviceClientCreation.WriteMetric(1);
        }

        internal static void OnDeviceClientDisposed()
        {
            s_instance._deviceClientDisposal.WriteMetric(1);
        }

        internal static void OnAmqpUnitCreated()
        {
            s_instance._amqpUnitCreation.WriteMetric(1);
        }

        internal static void OnAmqpUnitDisposed()
        {
            s_instance._amqpUnitDisposal.WriteMetric(1);
        }

        internal static void OnAmqpConnectionEstablished()
        {
            s_instance._amqpConnectionEstablishment.WriteMetric(1);
        }

        internal static void OnAmqpConnectionDisconnected()
        {
            s_instance._amqpConnectionDisconnection.WriteMetric(1);
        }

        internal static void OnAmqpSessionEstablished()
        {
            s_instance._amqpSessionEstablishment.WriteMetric(1);
        }

        internal static void OnAmqpSessionDisconnected()
        {
            s_instance._amqpSessionDisconnection.WriteMetric(1);
        }

        internal static void OnAmqpTokenRefresherStarted()
        {
            s_instance._amqpTokenRefresherInitiation.WriteMetric(1);
        }

        internal static void OnAmqpTokenRefresherStopped()
        {
            s_instance._amqpTokenRefresherTermination.WriteMetric(1);
        }

        internal static void OnAmqpTokenRefreshed()
        {
            s_instance._amqpTokenRefresh.WriteMetric(1);
        }

        internal static bool IsEnabled => s_instance.IsEnabled();

    }
#else
    internal sealed class DeviceEventCounter
    {
        internal static void OnDeviceClientCreated() {}

        internal static void OnDeviceClientDisposed() {}

        internal static void OnAmqpUnitCreated() {}

        internal static void OnAmqpUnitDisposed() { }

        internal static void OnAmqpConnectionEstablished() { }

        internal static void OnAmqpConnectionDisconnected() { }

        internal static void OnAmqpSessionEstablished() { }

        internal static void OnAmqpSessionDisconnected() { }

        internal static void OnAmqpTokenRefresherStarted() { }

        internal static void OnAmqpTokenRefresherStopped() { }

        internal static void OnAmqpTokenRefreshed() { }

        internal static bool IsEnabled => false;
    }
#endif
