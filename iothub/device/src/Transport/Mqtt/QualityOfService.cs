using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// The policy for which a particular message will be sent.
    /// </summary>
    public enum QualityOfService
    {
        /// <summary>
        /// The message will be sent once. It will not be resent under any circumstances.
        /// </summary>
        AtMostOnce = 0,

        /// <summary>
        /// The message will be sent once, but will be resent if the service fails to acknowledge the message.
        /// </summary>
        AtLeastOnce = 1,

        /// <summary>
        /// TODO does hub even support this?
        /// </summary>
        ExactlyOnce = 2
    }
}
