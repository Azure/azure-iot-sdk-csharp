//Copyright(c) Microsoft.All rights reserved.
//Microsoft would like to thank its contributors, a list
//of whom are at http://aka.ms/entlib-contributors

using System;

//Licensed under the Apache License, Version 2.0 (the "License"); you
//may not use this file except in compliance with the License. You may
//obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
//implied. See the License for the specific language governing permissions
//and limitations under the License.

// THIS FILE HAS BEEN MODIFIED FROM ITS ORIGINAL FORM.
// Change Log:
// 9/1/2017 jasminel Renamed namespace to Microsoft.Azure.Devices.Client.TransientFaultHandling and modified access modifier to internal.

namespace Microsoft.Azure.Devices.Client.TransientFaultHandling
{
    /// <summary>
    /// Contains information that is required for the <see cref="RetryPolicy.Retrying" /> event.
    /// </summary>
    internal class RetryingEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current retry count.
        /// </summary>
        public int CurrentRetryCount { get; private set; }

        /// <summary>
        /// Gets the delay that indicates how long the current thread will be suspended before the next iteration is invoked.
        /// </summary>
        public TimeSpan Delay { get; private set; }

        /// <summary>
        /// Gets the exception that caused the retry conditions to occur.
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryingEventArgs" /> class.
        /// </summary>
        /// <param name="currentRetryCount">The current retry attempt count.</param>
        /// <param name="delay">The delay that indicates how long the current thread will be suspended before the next iteration is invoked.</param>
        /// <param name="lastException">The exception that caused the retry conditions to occur.</param>
        public RetryingEventArgs(int currentRetryCount, TimeSpan delay, Exception lastException)
        {
            Guard.ArgumentNotNull(lastException, "lastException");
            CurrentRetryCount = currentRetryCount;
            Delay = delay;
            LastException = lastException;
        }
    }
}
