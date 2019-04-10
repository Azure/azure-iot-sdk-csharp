using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal abstract class AbstractStatusWatcher : IStatusReportor, IDisposable
    {
        private readonly List<IStatusMonitor> _statusMonitors;
        protected readonly object _stateLock;

        protected bool _disposed;
        protected bool _closed;

        protected abstract void CleanupResource();
        protected abstract void DisposeResource();

        protected AbstractStatusWatcher()
        {
            _statusMonitors = new List<IStatusMonitor>();
            _stateLock = new object();
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            lock (_stateLock)
            {
                if (!_disposed)
                {
                    if (Logging.IsEnabled) Logging.Enter(this, disposing, $"{nameof(Dispose)}");
                    if (disposing)
                    {
                        ChangeStatus(Status.Disposed);
                    }
                    if (Logging.IsEnabled) Logging.Exit(this, disposing, $"{nameof(Dispose)}");
                }
            }

        }

        #endregion

        #region Status watcher
        protected void ChangeStatus(Status status)
        {
            if (Logging.IsEnabled) Logging.Enter(this, status, $"{nameof(ChangeStatus)}");
            List<IStatusMonitor> statusMonitors;
            lock (_stateLock)
            {
                if (_disposed || (_closed && status != Status.Disposed))
                {
                    return;
                }

                if (status == Status.Disposed)
                {
                    statusMonitors = new List<IStatusMonitor>(_statusMonitors);
                    _closed = true;
                    _disposed = true;
                    DisposeResource();
                    _statusMonitors.Clear();
                }
                else
                {
                    statusMonitors = _statusMonitors;
                    if (status == Status.Closed)
                    {
                        _closed = true;
                        CleanupResource();
                    }
                }
            }

            foreach (IStatusMonitor statusMonitor in statusMonitors)
            {
                statusMonitor.OnStatusChange(this, status);
            }

            if (Logging.IsEnabled) Logging.Exit(this, status, $"{nameof(ChangeStatus)}");
        }

        public void AddStatusMonitor(IStatusMonitor statusMonitor)
        {
            if (Logging.IsEnabled) Logging.Enter(this, statusMonitor, $"{nameof(AddStatusMonitor)}");

            lock (_stateLock)
            {
                ThrowExceptionIfClosedOrDisposed();
                _statusMonitors.Add(statusMonitor);
                if (Logging.IsEnabled) Logging.Associate(this, statusMonitor, $"{nameof(AddStatusMonitor)}");
            }

            if (Logging.IsEnabled) Logging.Exit(this, statusMonitor, $"{nameof(AddStatusMonitor)}");
        }

        public void DetachStatusMonitor(IStatusMonitor statusMonitor)
        {
            if (Logging.IsEnabled) Logging.Enter(this, statusMonitor, $"{nameof(DetachStatusMonitor)}");

            lock (_stateLock)
            {
                ThrowExceptionIfClosedOrDisposed();
                _statusMonitors.Remove(statusMonitor);
                if (Logging.IsEnabled) Logging.Associate(this, statusMonitor, $"{nameof(DetachStatusMonitor)}");
            }

            if (Logging.IsEnabled) Logging.Exit(this, statusMonitor, $"{nameof(DetachStatusMonitor)}");

        }

        protected void ThrowExceptionIfDisposed()
        {
            lock (_stateLock)
            {
                if (_disposed)
                {
                    if (Logging.IsEnabled) Logging.Info(this, $"{this} is disposed.", $"{nameof(ThrowExceptionIfDisposed)}");
                    throw new ObjectDisposedException($"{this} is disposed.");
                }
            }
        }

        protected void ThrowExceptionIfClosedOrDisposed()
        {
            lock (_stateLock)
            {
                if (_disposed || _closed)
                {
                    if (_disposed)
                    {
                        DisposeResource();
                        if (Logging.IsEnabled) Logging.Info(this, $"{this} is disposed.", $"{nameof(ThrowExceptionIfClosedOrDisposed)}");
                        throw new ObjectDisposedException($"{this} is disposed.");
                    }
                    else
                    {
                        CleanupResource();
                        if (Logging.IsEnabled) Logging.Info(this, $"{this} is closed.", $"{nameof(ThrowExceptionIfClosedOrDisposed)}");
                        throw new InvalidOperationException($"{this} is closed.");
                    }
                }
            }
        }
        #endregion
    }
}
