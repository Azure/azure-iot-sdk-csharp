using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Retry Strategy types supported by DeviceClient
    /// </summary>
    [Obsolete("This enum has been deprecated. Please use Microsoft.Azure.Devices.Client.SetRetryPolicy(IRetryPolicy retryPolicy) instead.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1707:Identifiers should not contain underscores",
        Justification = "Public facing types cannot be renamed. This is considered a breaking change")]
    public enum RetryPolicyType
    {
        /// <summary>
        /// No retry.  A single attempt of operation.
        /// </summary>
        No_Retry = 0,

        /// <summary>
        /// A retry strategy which exponentially augments the retry delay and adds a random value to the delay at each retry.
        /// This is the DEFAULT policy to use
        /// </summary>
        Exponential_Backoff_With_Jitter = 1,
    }
}
