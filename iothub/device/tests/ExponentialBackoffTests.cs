using Microsoft.Azure.Devices.Client.TransientFaultHandling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    public class ExponentialBackoffTests
    {
        private const int MAX_RETRY_ATTEMPTS = 5000;

        [TestMethod]
        public async Task ExponentialBackoffDoesNotUnderflow()
        {
            TransientFaultHandling.ExponentialBackoff exponentialBackoff = new TransientFaultHandling.ExponentialBackoff(MAX_RETRY_ATTEMPTS, RetryStrategy.DefaultMinBackoff, RetryStrategy.DefaultMaxBackoff, RetryStrategy.DefaultClientBackoff);

            TimeSpan delay = TimeSpan.Zero;
            ShouldRetry shouldRetry = exponentialBackoff.GetShouldRetry();
            for (int i = 1; i < MAX_RETRY_ATTEMPTS; i++)
            {    
                shouldRetry(i, new Exception(), out delay);
                if (delay.TotalSeconds <= 0)
                {
                    Assert.Fail("Exponential backoff should never recommend a negative delay");
                }
            }
        }
    }
}
