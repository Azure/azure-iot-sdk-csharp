using System;

namespace Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling
{
    /// <summary>
    /// Contains information that is required for the <see cref="E:Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling.RetryPolicy.Retrying" /> event.
    /// </summary>
    public class RetryingEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current retry count.
        /// </summary>
        public int CurrentRetryCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the delay that indicates how long the current thread will be suspended before the next iteration is invoked.
        /// </summary>
        public TimeSpan Delay
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the exception that caused the retry conditions to occur.
        /// </summary>
        public Exception LastException
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling.RetryingEventArgs" /> class.
        /// </summary>
        /// <param name="currentRetryCount">The current retry attempt count.</param>
        /// <param name="delay">The delay that indicates how long the current thread will be suspended before the next iteration is invoked.</param>
        /// <param name="lastException">The exception that caused the retry conditions to occur.</param>
        public RetryingEventArgs(int currentRetryCount, TimeSpan delay, Exception lastException)
        {
            Guard.ArgumentNotNull(lastException, "lastException");
            this.CurrentRetryCount = currentRetryCount;
            this.Delay = delay;
            this.LastException = lastException;
        }
    }
}
